using RandomTaskTrack.Data.Models.Finance;

namespace RandomTaskTrack.Interfaces.Finance;

/// <summary>
/// Scoped to "what is this worth right now" — the one thing every quote API
/// agrees on. No history, no fundamentals, no order book: the projection values
/// holdings at one price, so anything more would be an interface built for a
/// caller that does not exist.
///
/// Batched on purpose. Every source meters or rate-limits per request, and the
/// refresh button always wants the whole portfolio at once.
/// </summary>
public interface IStockPriceSource
{
    string Name { get; }

    /// <param name="symbols">In the source's own vocabulary, as stored on the holding.</param>
    /// <returns>
    /// Only the symbols that resolved. A symbol missing from the result is a
    /// symbol with no price — the caller reports it rather than failing the
    /// whole refresh, since one dead ticker should not cost the other nine.
    /// </returns>
    Task<List<StockQuote>> GetQuotesAsync(IReadOnlyList<string> symbols, CancellationToken cancellationToken);
}
