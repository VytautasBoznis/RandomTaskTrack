using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Tasks;

public class GetCompletionLogRequest : AuthenticatedRequest
{
    public int? DomainId { get; set; }
    public string? TitleContains { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public int Limit { get; set; } = 200;
}
