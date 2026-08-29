using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RandomTaskTrack.Data.Models.ConfigurationOptions;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Finance;
using RandomTaskTrack.Interfaces.Finance;

namespace RandomTaskTrack.Business.Finance.Sources;

/// <summary>
/// Yahoo's chart endpoint: no key, no account, no quota to sign up for. That is
/// why it is the default — the tab works the moment the app is deployed.
///
/// It is an undocumented endpoint rather than a published API, which is the
/// honest trade-off here. Two things it needs that are easy to get wrong:
/// a browser User-Agent (without one every request is answered 429), and one
/// request per symbol (the batch `v7/finance/quote` route now demands a crumb
/// and cookie pair, which is not worth carrying for a portfolio of twenty).
///
/// FX comes from the same place. Yahoo quotes a pair as an ordinary symbol —
/// `EURUSD=X` is "how many USD to one EUR", which is exactly what rate_to_base
/// means — so refreshing prices and refreshing rates is one loop.
///
/// Requests are sequential on purpose. A personal portfolio is a handful of
/// symbols, a button press can afford two seconds, and firing twenty at once is
/// the reliable way to be rate-limited.
/// </summary>
public class YahooPriceSource : IStockPriceSource
{
    // Yahoo answers 429 to anything that does not look like a browser.
    private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly FinanceOptions _options;
    private readonly ILogger<YahooPriceSource> _logger;

    public string Name => PriceSourceNames.Yahoo;

    public YahooPriceSource(
        IHttpClientFactory httpClientFactory,
        IOptions<FinanceOptions> options,
        ILogger<YahooPriceSource> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<List<StockQuote>> GetQuotesAsync(IReadOnlyList<string> symbols, CancellationToken cancellationToken)
    {
        var quotes = new List<StockQuote>();

        if (symbols.Count == 0)
        {
            return quotes;
        }

        HttpClient client = _httpClientFactory.CreateClient(PriceSourceNames.Yahoo);

        foreach (string symbol in symbols)
        {
            StockQuote? quote = await TryGetQuoteAsync(client, symbol, cancellationToken);

            if (quote is not null)
            {
                quotes.Add(quote);
            }
        }

        _logger.LogInformation("Yahoo priced {Priced} of {Asked} symbols", quotes.Count, symbols.Count);

        return quotes;
    }

    /// <summary>
    /// Null for anything that did not resolve. A delisted or mistyped ticker is
    /// answered with a 404 and a JSON error rather than an exception, and one
    /// dead symbol must not cost the caller the other nine prices — so nothing
    /// here throws.
    /// </summary>
    private async Task<StockQuote?> TryGetQuoteAsync(HttpClient client, string symbol, CancellationToken cancellationToken)
    {
        string url = $"{_options.BaseUrl.TrimEnd('/')}/v8/finance/chart/{Uri.EscapeDataString(symbol)}" +
                     "?interval=1d&range=1d";

        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Get, url);
            message.Headers.Add("User-Agent", UserAgent);

            using HttpResponseMessage response = await client.SendAsync(message, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Yahoo returned {Status} for {Symbol}", (int)response.StatusCode, symbol);

                return null;
            }

            await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using JsonDocument document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (!document.RootElement.TryGetProperty("chart", out JsonElement chart) ||
                !chart.TryGetProperty("result", out JsonElement result) ||
                result.ValueKind != JsonValueKind.Array ||
                result.GetArrayLength() == 0)
            {
                return null;
            }

            if (!result[0].TryGetProperty("meta", out JsonElement meta) ||
                !meta.TryGetProperty("regularMarketPrice", out JsonElement price) ||
                price.ValueKind != JsonValueKind.Number)
            {
                return null;
            }

            return new StockQuote
            {
                Symbol = symbol,
                Price = price.GetDecimal(),
                AsOf = ReadAsOf(meta)
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // A network blip on one symbol is not a reason to fail the refresh.
            _logger.LogWarning(ex, "Yahoo lookup failed for {Symbol}", symbol);

            return null;
        }
    }

    private static DateOnly? ReadAsOf(JsonElement meta)
    {
        if (!meta.TryGetProperty("regularMarketTime", out JsonElement time) || time.ValueKind != JsonValueKind.Number)
        {
            return null;
        }

        return DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(time.GetInt64()).UtcDateTime);
    }
}
