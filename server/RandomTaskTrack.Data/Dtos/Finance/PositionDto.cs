using RandomTaskTrack.Data.Models.Finance;

namespace RandomTaskTrack.Data.Dtos.Finance;

/// <summary>
/// A holding with its trades folded in. Quantity and cost basis are summed from
/// <see cref="Trades"/> rather than stored, so correcting a trade corrects the
/// position for free.
/// </summary>
public class PositionDto
{
    public Guid Id { get; set; }
    public string Symbol { get; set; } = "";
    public string? Name { get; set; }
    public string Currency { get; set; } = "";
    public decimal? LastPrice { get; set; }
    public DateTime? LastPriceAt { get; set; }

    /// <summary>Buys minus sells. Can be zero for a fully closed position.</summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// What the shares still held cost, fees included, in the holding's own
    /// currency. Average-cost, not FIFO: this is a personal tracker, not a tax
    /// return, and average cost is the number that answers "am I up on this".
    /// </summary>
    public decimal CostBasis { get; set; }

    /// <summary>Quantity × LastPrice, in the holding's currency. Null with no price.</summary>
    public decimal? MarketValue { get; set; }

    /// <summary>MarketValue converted to base. Null with no price.</summary>
    public decimal? MarketValueBase { get; set; }

    public List<Trade> Trades { get; set; } = new();
}
