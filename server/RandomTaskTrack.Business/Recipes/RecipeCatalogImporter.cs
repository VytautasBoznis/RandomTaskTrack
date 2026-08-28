using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;
using RandomTaskTrack.Data.Models.ConfigurationOptions;
using RandomTaskTrack.Data.Models.Recipes;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Recipes;

namespace RandomTaskTrack.Business.Recipes;

/// <summary>
/// Streams the bulk recipe corpus into tracker.recipe_catalog.
///
/// On demand rather than on startup: it is two gigabytes, and whether a
/// household wants a two million recipe cookbook on its wall tablet is a
/// decision for the person, not for the deployment. The Recipes tab starts it
/// and polls.
///
/// The file is never written to disk or held in memory — rows go from the HTTP
/// response straight into a Postgres binary COPY.
///
/// Re-running is incremental and safe. Rows land in a temp table and cross into
/// the catalog in one INSERT ... ON CONFLICT DO NOTHING, so a second run adds
/// only what is new, and a pod that dies mid-import leaves nothing behind: the
/// temp table goes with the connection and the single promoting statement either
/// commits whole or not at all.
/// </summary>
public class RecipeCatalogImporter : IRecipeCatalogImporter
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly IUnitOfWorkFactory _unitOfWorkFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly RecipeOptions _options;
    private readonly ILogger<RecipeCatalogImporter> _logger;

    private readonly Lock _gate = new();
    private bool _isRunning;
    private long _rowsRead;
    private long _rowsAdded;
    private DateTime? _finishedAt;
    private string? _error;

    public RecipeCatalogImporter(
        IUnitOfWorkFactory unitOfWorkFactory,
        IHttpClientFactory httpClientFactory,
        IOptions<RecipeOptions> options,
        ILogger<RecipeCatalogImporter> logger)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<CatalogImportStatus> GetStatusAsync(CancellationToken cancellationToken)
    {
        await using IUnitOfWork unitOfWork = await _unitOfWorkFactory.CreateAsync();

        await using var command = new NpgsqlCommand("SELECT count(*) FROM tracker.recipe_catalog", unitOfWork.Connection);

        long loaded = (long)(await command.ExecuteScalarAsync(cancellationToken) ?? 0L);

        lock (_gate)
        {
            return new CatalogImportStatus
            {
                Loaded = loaded,
                SourceRows = _options.CatalogSourceRows,
                IsRunning = _isRunning,
                RowsRead = _rowsRead,
                RowsAdded = _rowsAdded,
                FinishedAt = _finishedAt,
                Error = _error
            };
        }
    }

    public bool TryStart()
    {
        lock (_gate)
        {
            if (_isRunning)
            {
                return false;
            }

            _isRunning = true;
            _rowsRead = 0;
            _rowsAdded = 0;
            _finishedAt = null;
            _error = null;
        }

        // Deliberately not awaited: the request returns immediately and the run
        // continues on a background thread. Nothing is scoped to the request, so
        // there is nothing to dispose out from under it.
        _ = Task.Run(RunAsync);

        return true;
    }

    private async Task RunAsync()
    {
        try
        {
            _logger.LogInformation("Catalog import starting from {Url}", _options.CatalogUrl);

            long added = await ImportAsync();

            lock (_gate)
            {
                _rowsAdded = added;
                _finishedAt = DateTime.UtcNow;
            }

            _logger.LogInformation("Catalog import finished; {Added} new recipes", added);
        }
        catch (Exception ex)
        {
            // The tab is the only audience, so the message has to survive to it
            // rather than only reaching the log.
            _logger.LogError(ex, "Catalog import failed");

            lock (_gate)
            {
                _error = ex.Message;
                _finishedAt = DateTime.UtcNow;
            }
        }
        finally
        {
            lock (_gate)
            {
                _isRunning = false;
            }
        }
    }

    private async Task<long> ImportAsync()
    {
        await using IUnitOfWork unitOfWork = await _unitOfWorkFactory.CreateAsync();
        NpgsqlConnection connection = unitOfWork.Connection;

        await using (var staging = new NpgsqlCommand(
            @"CREATE TEMP TABLE catalog_staging
                  (external_id text, title text, ingredients jsonb, steps jsonb, link text)
              ON COMMIT PRESERVE ROWS", connection))
        {
            await staging.ExecuteNonQueryAsync();
        }

        HttpClient client = _httpClientFactory.CreateClient(nameof(RecipeCatalogImporter));
        client.Timeout = Timeout.InfiniteTimeSpan;

        using HttpResponseMessage response = await client.GetAsync(_options.CatalogUrl, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        await using Stream stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            // Scraped text: a handful of rows have a stray column or a bad
            // quote. Losing those beats losing the import two million rows in.
            BadDataFound = null,
            MissingFieldFound = null,
            HasHeaderRecord = true
        });

        await csv.ReadAsync();
        csv.ReadHeader();

        long written = 0;

        await using (var writer = await connection.BeginBinaryImportAsync(
            "COPY catalog_staging (external_id, title, ingredients, steps, link) FROM STDIN (FORMAT BINARY)"))
        {
            while (await csv.ReadAsync())
            {
                lock (_gate)
                {
                    _rowsRead++;
                }

                CatalogRow? row = Parse(csv);

                if (row is null)
                {
                    continue;
                }

                await writer.StartRowAsync();
                await writer.WriteAsync(row.ExternalId);
                await writer.WriteAsync(row.Title);
                await writer.WriteAsync(row.Ingredients, NpgsqlDbType.Jsonb);
                await writer.WriteAsync(row.Steps, NpgsqlDbType.Jsonb);

                if (row.Link is null)
                {
                    await writer.WriteNullAsync();
                }
                else
                {
                    await writer.WriteAsync(row.Link);
                }

                written++;

                if (_options.CatalogMaxRows > 0 && written >= _options.CatalogMaxRows)
                {
                    break;
                }
            }

            await writer.CompleteAsync();
        }

        await using var promote = new NpgsqlCommand(
            @"INSERT INTO tracker.recipe_catalog (external_id, title, ingredients, steps, link)
              SELECT DISTINCT ON (external_id) external_id, title, ingredients, steps, link
              FROM catalog_staging
              ON CONFLICT (external_id) DO NOTHING",
            connection);

        // Two million rows through a gin index takes as long as it takes, and
        // timing out at the last step would waste the whole download.
        promote.CommandTimeout = 0;

        return await promote.ExecuteNonQueryAsync();
    }

    private static CatalogRow? Parse(CsvReader csv)
    {
        string title = (csv.TryGetField("title", out string? t) ? t : null)?.Trim() ?? "";

        if (title.Length == 0)
        {
            return null;
        }

        List<string> steps = ReadJsonArray(csv, "directions");

        // The same rule the API sources apply: a dish with no method is useless
        // here, because the point is to cook it. One-line "recipes" are scrape
        // noise rather than instructions.
        if (steps.Count < 2)
        {
            return null;
        }

        List<string> ingredients = ReadJsonArray(csv, "ingredients");

        if (ingredients.Count == 0)
        {
            return null;
        }

        string? link = (csv.TryGetField("link", out string? l) ? l : null)?.Trim();

        return new CatalogRow
        {
            ExternalId = Hash(title, link),
            Title = title.Length > 500 ? title[..500] : title,
            // RecipeIngredient's shape. The corpus keeps the amount inside the
            // line ("1 c. flour"), which is what a shopping list wants, so there
            // is nothing to split out.
            Ingredients = JsonSerializer.Serialize(ingredients.Select(item => new { item, amount = (string?)null }), Json),
            Steps = JsonSerializer.Serialize(steps, Json),
            Link = string.IsNullOrWhiteSpace(link) ? null : link
        };
    }

    private static List<string> ReadJsonArray(CsvReader csv, string field)
    {
        if (!csv.TryGetField(field, out string? raw) || string.IsNullOrWhiteSpace(raw))
        {
            return new List<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(raw!, Json)?
                       .Select(value => value.Trim())
                       .Where(value => value.Length > 0)
                       .ToList()
                   ?? new List<string>();
        }
        catch (JsonException)
        {
            // A malformed cell costs one recipe out of two million.
            return new List<string>();
        }
    }

    /// <summary>
    /// Stable across runs, which is what makes a re-import add only new dishes
    /// and never duplicate one already copied into the library.
    /// </summary>
    private static string Hash(string title, string? link) =>
        Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes($"{title}|{link}"))).ToLowerInvariant();

    private sealed class CatalogRow
    {
        public required string ExternalId { get; init; }
        public required string Title { get; init; }
        public required string Ingredients { get; init; }
        public required string Steps { get; init; }
        public required string? Link { get; init; }
    }
}
