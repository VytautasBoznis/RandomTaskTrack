using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Data.Models.ConfigurationOptions;
using RandomTaskTrack.Data.Models.Finance;
using RandomTaskTrack.Data.Request.Finance;
using RandomTaskTrack.Data.Response.Finance;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Finance;
using RandomTaskTrack.Interfaces.Repositories.Finance;
using RandomTaskTrack.Interfaces.Services;

namespace RandomTaskTrack.Business.Operations.Finance;

/// <summary>
/// The button. Prices and FX rates in one pass, because the source quotes
/// currency pairs as ordinary symbols and the portfolio is worthless without
/// the rates that convert it.
///
/// A symbol the source cannot price is reported, not thrown: one dead ticker
/// should not cost you the other nine prices, and the stale price stays on the
/// holding rather than being wiped.
/// </summary>
public class RefreshPricesOperation : BaseOperation<RefreshPricesRequest, RefreshPricesResponse>
{
    private readonly IFinanceRepository _financeRepository;
    private readonly IStockPriceSource _priceSource;
    private readonly IClock _clock;
    private readonly FinanceOptions _options;

    public RefreshPricesOperation(
        ILogger<RefreshPricesOperation> logger,
        IValidator<RefreshPricesRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IFinanceRepository financeRepository,
        IStockPriceSource priceSource,
        IClock clock,
        IOptions<FinanceOptions> options) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _financeRepository = financeRepository;
        _priceSource = priceSource;
        _clock = clock;
        _options = options.Value;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<RefreshPricesResponse> Execute(RefreshPricesRequest request, IUnitOfWork unitOfWork)
    {
        List<Holding> holdings = await _financeRepository.GetHoldingsAsync(unitOfWork);
        List<Currency> currencies = await _financeRepository.GetCurrenciesAsync(unitOfWork);

        // A rate is spelled as a pair symbol: EURUSD=X is "how many USD to one
        // EUR", which is exactly what rate_to_base means. The base currency is
        // 1 by definition and is never asked for.
        var pairs = currencies
            .Where(c => !string.Equals(c.Code, _options.BaseCurrency, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(
                c => $"{_options.BaseCurrency.ToUpperInvariant()}{c.Code.ToUpperInvariant()}=X",
                c => c.Code,
                StringComparer.OrdinalIgnoreCase);

        // Distinct: the same symbol held in two accounts is two holdings but
        // one quote, and asking twice would only be slower and ruder.
        List<string> symbols = holdings
            .Select(h => h.Symbol)
            .Concat(pairs.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (symbols.Count == 0)
        {
            return new RefreshPricesResponse();
        }

        List<StockQuote> quotes = await _priceSource.GetQuotesAsync(symbols, CancellationToken.None);

        var bySymbol = quotes
            .GroupBy(q => q.Symbol, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        DateTime now = _clock.UtcNow;
        var response = new RefreshPricesResponse();

        foreach (string symbol in holdings.Select(h => h.Symbol).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!bySymbol.TryGetValue(symbol, out StockQuote? quote))
            {
                response.Failed.Add(symbol);
                continue;
            }

            // One write per symbol, counted per holding it landed on: a symbol
            // held in two accounts really did update two positions.
            response.UpdatedHoldings += await _financeRepository.UpdatePricesBySymbolAsync(symbol, quote.Price, now, unitOfWork);
        }

        foreach ((string pairSymbol, string code) in pairs)
        {
            // A rate of zero would turn every conversion into a divide-by-zero
            // guard downstream, so it is treated as no answer at all.
            if (!bySymbol.TryGetValue(pairSymbol, out StockQuote? quote) || quote.Price <= 0)
            {
                response.Failed.Add(pairSymbol);
                continue;
            }

            await _financeRepository.UpsertCurrencyRateAsync(code, quote.Price, unitOfWork);
            response.UpdatedCurrencies++;
        }

        _logger.LogInformation(
            "Price refresh updated {Holdings} holdings and {Currencies} rates; {Failed} symbols had no price",
            response.UpdatedHoldings, response.UpdatedCurrencies, response.Failed.Count);

        return response;
    }
}
