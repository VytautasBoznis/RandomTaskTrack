using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Finance;

/// <summary>Cascades to the holding trades and dividends — the schema says so.</summary>
public class DeleteHoldingRequest : AuthenticatedRequest
{
    public Guid Id { get; set; }
}
