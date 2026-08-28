using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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
public partial class RecipeCatalogImporter : IRecipeCatalogImporter
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Rows per COPY before the batch is promoted. Big enough that the
    /// per-batch overhead is noise, small enough that a failure costs seconds.</summary>
    private const int BatchSize = 50_000;

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
                  (external_id text, title text, ingredients jsonb, steps jsonb, link text,
                   image_url text, ready_minutes int, servings int, feed text)
              ON COMMIT PRESERVE ROWS", connection))
        {
            await staging.ExecuteNonQueryAsync();
        }

        long added = 0;

        // Rich feed first: it is 64MB against 2GB, so the pictured dishes are in
        // place within seconds rather than after the long tail has finished.
        added += await ImportFeedAsync(connection, Feeds.AllRecipes, _options.CatalogRichUrl, ParseAllRecipes);
        added += await ImportFeedAsync(connection, Feeds.RecipeNlg, _options.CatalogUrl, ParseRecipeNlg);

        return added;
    }

    /// <summary>
    /// Skips a feed that already has rows. Without this, "check for new" would
    /// re-stream two gigabytes to discover it has them all — and adding the
    /// second feed to an existing install would cost that for nothing.
    /// </summary>
    private async Task<long> ImportFeedAsync(
        NpgsqlConnection connection, string feed, string url, Func<CsvReader, string, CatalogRow?> parse)
    {
        await using (var loaded = new NpgsqlCommand(
            "SELECT EXISTS (SELECT 1 FROM tracker.recipe_catalog WHERE feed = @feed)", connection))
        {
            loaded.Parameters.AddWithValue("feed", feed);

            if ((bool)(await loaded.ExecuteScalarAsync() ?? false))
            {
                _logger.LogInformation("Feed {Feed} is already loaded; skipping", feed);

                return 0;
            }
        }

        HttpClient client = _httpClientFactory.CreateClient(nameof(RecipeCatalogImporter));
        client.Timeout = Timeout.InfiniteTimeSpan;

        using HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
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
        long added = 0;

        // Flushed in batches rather than one enormous COPY. Ten minutes of
        // streaming is long enough for a network blip, and losing all of it to a
        // failure at ninety percent is miserable. Batching also makes a re-run
        // resume for free — the ids are stable, so ON CONFLICT skips whatever
        // already landed — and lets the tab's "loaded" count climb as it goes
        // instead of sitting at zero until the very end.
        NpgsqlBinaryImporter writer = await OpenAsync(connection);

        try
        {
            while (await csv.ReadAsync())
            {
                lock (_gate)
                {
                    _rowsRead++;
                }

                CatalogRow? row = parse(csv, feed);

                if (row is null)
                {
                    continue;
                }

                await writer.StartRowAsync();
                await writer.WriteAsync(row.ExternalId);
                await writer.WriteAsync(row.Title);
                await writer.WriteAsync(row.Ingredients, NpgsqlDbType.Jsonb);
                await writer.WriteAsync(row.Steps, NpgsqlDbType.Jsonb);
                await WriteNullableAsync(writer, row.Link);
                await WriteNullableAsync(writer, row.ImageUrl);
                await WriteNullableAsync(writer, row.ReadyMinutes);
                await WriteNullableAsync(writer, row.Servings);
                await writer.WriteAsync(feed);

                written++;

                if (written % BatchSize == 0)
                {
                    await writer.CompleteAsync();
                    await writer.DisposeAsync();

                    added += await PromoteAsync(connection);

                    lock (_gate)
                    {
                        _rowsAdded = added;
                    }

                    writer = await OpenAsync(connection);
                }

                if (_options.CatalogMaxRows > 0 && written >= _options.CatalogMaxRows)
                {
                    break;
                }
            }

            await writer.CompleteAsync();
        }
        finally
        {
            await writer.DisposeAsync();
        }

        return added + await PromoteAsync(connection);
    }

    private static Task<NpgsqlBinaryImporter> OpenAsync(NpgsqlConnection connection) =>
        connection.BeginBinaryImportAsync(
            @"COPY catalog_staging
                  (external_id, title, ingredients, steps, link, image_url, ready_minutes, servings, feed)
              FROM STDIN (FORMAT BINARY)");

    private static async Task WriteNullableAsync<T>(NpgsqlBinaryImporter writer, T? value) where T : class
    {
        if (value is null)
        {
            await writer.WriteNullAsync();
        }
        else
        {
            await writer.WriteAsync(value);
        }
    }

    private static async Task WriteNullableAsync(NpgsqlBinaryImporter writer, int? value)
    {
        if (value is null)
        {
            await writer.WriteNullAsync();
        }
        else
        {
            await writer.WriteAsync(value.Value);
        }
    }

    /// <summary>Staging into the catalog, one row per external_id, then empties
    /// staging so the next batch starts clean.</summary>
    private static async Task<long> PromoteAsync(NpgsqlConnection connection)
    {
        await using var promote = new NpgsqlCommand(
            @"INSERT INTO tracker.recipe_catalog
                  (external_id, title, ingredients, steps, link, image_url, ready_minutes, servings, feed)
              SELECT DISTINCT ON (external_id)
                     external_id, title, ingredients, steps, link, image_url, ready_minutes, servings, feed
              FROM catalog_staging
              ON CONFLICT (external_id) DO NOTHING",
            connection);

        // A batch through a gin index takes as long as it takes, and timing out
        // would throw away work that has already been downloaded and parsed.
        promote.CommandTimeout = 0;

        int added = await promote.ExecuteNonQueryAsync();

        await using var clear = new NpgsqlCommand("TRUNCATE catalog_staging", connection);
        await clear.ExecuteNonQueryAsync();

        return added;
    }

    /// <summary>
    /// AllRecipes: ingredients are one semicolon-separated line and the method
    /// is a paragraph, so both are split here rather than stored as prose. The
    /// payoff is image, time and servings, which the other feed has none of.
    /// </summary>
    private static CatalogRow? ParseAllRecipes(CsvReader csv, string feed)
    {
        string title = Clean(Field(csv, "title"));

        if (title.Length == 0)
        {
            return null;
        }

        List<string> ingredients = Clean(Field(csv, "ingredients"))
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        // Sentence ends followed by a capital. Splitting on every full stop
        // would cut "350 degrees F (175 degrees C)." and "10 to 15 minutes."
        // into fragments the way the other corpus already suffers from.
        List<string> steps = SentenceBreak()
            .Split(Clean(Field(csv, "directions")))
            .Select(step => step.Trim())
            .Where(step => step.Length > 1)
            .ToList();

        if (ingredients.Count == 0 || steps.Count == 0)
        {
            return null;
        }

        string link = Clean(Field(csv, "url"));
        string image = Clean(Field(csv, "image"));

        return new CatalogRow
        {
            ExternalId = Hash(title, link),
            Title = title.Length > 500 ? title[..500] : title,
            Ingredients = JsonSerializer.Serialize(ingredients.Select(item => new { item, amount = (string?)null }), Json),
            Steps = JsonSerializer.Serialize(steps, Json),
            Link = link.Length == 0 ? null : link,
            // Only http links: the field is occasionally a bare filename.
            ImageUrl = image.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? image : null,
            ReadyMinutes = ParseMinutes(Field(csv, "total_time")),
            Servings = int.TryParse(Field(csv, "servings"), out int s) && s > 0 ? s : null,
            Feed = feed
        };
    }

    /// <summary>"55 mins", "1 hr 20 mins", "1 hr" — null when it says none of those.</summary>
    private static int? ParseMinutes(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        Match hours = HoursPart().Match(value);
        Match minutes = MinutesPart().Match(value);

        int total = (hours.Success ? int.Parse(hours.Groups[1].Value) * 60 : 0)
                  + (minutes.Success ? int.Parse(minutes.Groups[1].Value) : 0);

        return total > 0 ? total : null;
    }

    private static string Field(CsvReader csv, string name) =>
        csv.TryGetField(name, out string? value) ? value ?? "" : "";

    private static CatalogRow? ParseRecipeNlg(CsvReader csv, string feed)
    {
        string title = Clean(csv.TryGetField("title", out string? t) ? t : null);

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

        string link = Clean(csv.TryGetField("link", out string? l) ? l : null);

        return new CatalogRow
        {
            ExternalId = Hash(title, link),
            Title = title.Length > 500 ? title[..500] : title,
            // RecipeIngredient's shape. The corpus keeps the amount inside the
            // line ("1 c. flour"), which is what a shopping list wants, so there
            // is nothing to split out.
            Ingredients = JsonSerializer.Serialize(ingredients.Select(item => new { item, amount = (string?)null }), Json),
            Steps = JsonSerializer.Serialize(steps, Json),
            Link = link.Length == 0 ? null : link,
            // This corpus has no media or timings at all — that gap is exactly
            // why the AllRecipes feed exists alongside it.
            ImageUrl = null,
            ReadyMinutes = null,
            Servings = null,
            Feed = feed
        };
    }

    /// <summary>
    /// Postgres will not take a NUL in a text column ("invalid byte sequence
    /// for encoding UTF8: 0x00") and will not take   inside jsonb
    /// ("unsupported Unicode escape sequence"), and a two million row web scrape
    /// reliably contains both, along with the occasional half of a surrogate
    /// pair. There is no skipping a row once a binary COPY is in flight — one
    /// bad character aborts the entire stream — so they come out here instead.
    ///
    /// Tabs and newlines are kept: they are ordinary punctuation in a method.
    /// </summary>
    private static string Clean(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        var clean = new StringBuilder(value.Length);

        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];

            if (char.IsHighSurrogate(c))
            {
                // Only whole pairs survive; half of one is not encodable UTF-8.
                if (i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]))
                {
                    clean.Append(c).Append(value[i + 1]);
                    i++;
                }

                continue;
            }

            if (char.IsLowSurrogate(c) || (char.IsControl(c) && c is not ('\t' or '\n' or '\r')))
            {
                continue;
            }

            clean.Append(c);
        }

        return clean.ToString().Trim();
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
                       .Select(Clean)
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

    [GeneratedRegex(@"(?<=[.!?])\s+(?=[A-Z])")]
    private static partial Regex SentenceBreak();

    [GeneratedRegex(@"(\d+)\s*h", RegexOptions.IgnoreCase)]
    private static partial Regex HoursPart();

    [GeneratedRegex(@"(\d+)\s*m", RegexOptions.IgnoreCase)]
    private static partial Regex MinutesPart();

    private static class Feeds
    {
        public const string RecipeNlg = "recipenlg";
        public const string AllRecipes = "allrecipes";
    }

    private sealed class CatalogRow
    {
        public required string ExternalId { get; init; }
        public required string Title { get; init; }
        public required string Ingredients { get; init; }
        public required string Steps { get; init; }
        public required string? Link { get; init; }
        public required string? ImageUrl { get; init; }
        public required int? ReadyMinutes { get; init; }
        public required int? Servings { get; init; }
        public required string Feed { get; init; }
    }
}
