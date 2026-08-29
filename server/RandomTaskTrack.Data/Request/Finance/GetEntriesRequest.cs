using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Finance;

public class GetEntriesRequest : AuthenticatedRequest
{
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public FinanceFlowKind? Kind { get; set; }
    public string? Search { get; set; }
    public int Limit { get; set; } = 200;
}
