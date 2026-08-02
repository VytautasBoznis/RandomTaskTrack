using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Tasks;

public class CompleteTaskRequest : AuthenticatedRequest
{
    public Guid Id { get; set; }

    /// <summary>Done or Skipped. Skipped still writes a log row — a skip is
    /// data, not an absence of data.</summary>
    public TaskItemStatus Status { get; set; } = TaskItemStatus.Done;

    /// <summary>What actually happened. Falls back to the task's planned data
    /// when the user completes without adjusting anything.</summary>
    public string? ActualData { get; set; }

    public string? Note { get; set; }
}
