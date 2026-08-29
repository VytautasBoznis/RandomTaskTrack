using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Finance;

/// <summary>At least one of TargetOn and Amount must be set, or there is nothing to draw.</summary>
public class CreateTargetRequest : AuthenticatedRequest
{
    public string Label { get; set; } = "";
    public DateOnly? TargetOn { get; set; }
    public decimal? Amount { get; set; }
    public string? Note { get; set; }
}
