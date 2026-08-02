using RandomTaskTrack.Data.Models.Enums;

namespace RandomTaskTrack.Data.Models.Tasks;

public class TaskItem
{
    public Guid Id { get; set; }
    public int DomainId { get; set; }
    public Guid? RecurrenceId { get; set; }
    public string Title { get; set; } = "";
    public string? Notes { get; set; }

    /// <summary>Raw jsonb. Domain-specific payload (sets/reps/weight, ml of
    /// water, recipe id). Deliberately untyped at this layer.</summary>
    public string Data { get; set; } = "{}";

    public DateOnly DueOn { get; set; }
    public TimeOnly? DueTime { get; set; }
    public TaskItemStatus Status { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
