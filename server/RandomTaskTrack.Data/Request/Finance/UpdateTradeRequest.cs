using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Finance;

/// <summary>The manual correction path: fix the trade, the position follows.</summary>
public class UpdateTradeRequest : AuthenticatedRequest
{
    public Guid Id { get; set; }
    public TradeSide? Side { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? Price { get; set; }
    public decimal? Fee { get; set; }
    public DateOnly? TradedOn { get; set; }
    public string? Note { get; set; }
}
