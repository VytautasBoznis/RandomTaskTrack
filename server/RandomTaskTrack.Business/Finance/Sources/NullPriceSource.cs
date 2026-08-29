using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Exceptions;
using RandomTaskTrack.Data.Models.Finance;
using RandomTaskTrack.Interfaces.Finance;

namespace RandomTaskTrack.Business.Finance.Sources;

/// <summary>
/// What you get when Finance:Provider is set to "null". Every other part of the
/// scope works without a price source — holdings, trades, flows and the
/// projection are all hand-entered — so only the refresh button reports why.
/// </summary>
public class NullPriceSource : IStockPriceSource
{
    public string Name => PriceSourceNames.Null;

    public Task<List<StockQuote>> GetQuotesAsync(IReadOnlyList<string> symbols, CancellationToken cancellationToken) =>
        throw new PriceSourceException(
            "No price source is configured.",
            ExceptionCodes.FINANCE_PRICE_SOURCE_FAILED,
            "Set Finance__Provider to 'yahoo' to enable price refreshes.");
}
