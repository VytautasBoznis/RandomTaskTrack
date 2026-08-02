using RandomTaskTrack.Data.Models.Enums;

namespace RandomTaskTrack.Data.Models.Tasks;

public class TaskCompletion
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public int DomainId { get; set; }
    public TaskItemStatus Status { get; set; }

    /// <summary>What was asked for, snapshotted at completion time.</summary>
    public string PlannedData { get; set; } = "{}";

    /// <summary>What actually happened. The difference between these two is the
    /// whole point of the log.</summary>
    public string ActualData { get; set; } = "{}";

    public string? Note { get; set; }
    public DateOnly DueOn { get; set; }
    public DateTime CompletedAt { get; set; }
}
