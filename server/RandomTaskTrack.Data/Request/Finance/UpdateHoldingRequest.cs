using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Finance;

public class UpdateHoldingRequest : AuthenticatedRequest
{
    public Guid Id { get; set; }
    public string? Symbol { get; set; }
    public string? Name { get; set; }
    public string? Currency { get; set; }
}
