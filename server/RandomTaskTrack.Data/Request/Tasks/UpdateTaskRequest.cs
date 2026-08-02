using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Tasks;

public class UpdateTaskRequest : AuthenticatedRequest
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public string? Notes { get; set; }
    public string? Data { get; set; }
    public DateOnly? DueOn { get; set; }
    public TimeOnly? DueTime { get; set; }
    public int? DomainId { get; set; }
}
