using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Finance;

public class CreateTradeRequest : AuthenticatedRequest
{
    public Guid HoldingId { get; set; }
    public TradeSide Side { get; set; }

    /// <summary>Positive. Side carries the sign.</summary>
    public decimal Quantity { get; set; }

    public decimal Price { get; set; }
    public decimal? Fee { get; set; }
    public DateOnly TradedOn { get; set; }
    public string? Note { get; set; }
}
