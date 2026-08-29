using RandomTaskTrack.Data.Models.Enums;

namespace RandomTaskTrack.Data.Models.Finance;

/// <summary>
/// A buy or a sell. The position is the sum of these and is never stored:
/// storing both invites them to disagree, and correcting a mistake means
/// editing the trade that was wrong and letting the position follow.
/// </summary>
public class Trade
{
    public Guid Id { get; set; }
    public Guid HoldingId { get; set; }
    public TradeSide Side { get; set; }

    /// <summary>Always positive. <see cref="Side"/> carries the sign.</summary>
    public decimal Quantity { get; set; }

    public decimal Price { get; set; }

    /// <summary>Commission and the rest. Part of the cost basis, not the price.</summary>
    public decimal Fee { get; set; }

    public DateOnly TradedOn { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}
