using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Tasks;

public class CreateTaskRequest : AuthenticatedRequest
{
    public int DomainId { get; set; }
    public string Title { get; set; } = "";
    public string? Notes { get; set; }

    /// <summary>Raw JSON object for the domain-specific payload.</summary>
    public string? Data { get; set; }

    public DateOnly DueOn { get; set; }
    public TimeOnly? DueTime { get; set; }
}
