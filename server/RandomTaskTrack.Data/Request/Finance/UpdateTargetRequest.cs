using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Finance;

public class UpdateTargetRequest : AuthenticatedRequest
{
    public Guid Id { get; set; }
    public string? Label { get; set; }
    public DateOnly? TargetOn { get; set; }
    public decimal? Amount { get; set; }
    public string? Note { get; set; }
}
