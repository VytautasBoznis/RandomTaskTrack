using RandomTaskTrack.Data.Models.Enums;

namespace RandomTaskTrack.Data.Models.Tasks;

public class TaskRecurrence
{
    public Guid Id { get; set; }
    public int DomainId { get; set; }
    public string Title { get; set; } = "";
    public string? Notes { get; set; }
    public string Data { get; set; } = "{}";

    public RecurrenceRuleType RuleType { get; set; }
    public int? IntervalDays { get; set; }
    public int[]? DaysOfWeek { get; set; }
    public int? DayOfMonth { get; set; }
    public RecurrenceAnchorMode AnchorMode { get; set; }

    public TimeOnly? TimeOfDay { get; set; }
    public DateOnly StartsOn { get; set; }
    public DateOnly? EndsOn { get; set; }
    public bool IsActive { get; set; }

    /// <summary>Last date materialized. The materializer resumes from here so a
    /// restart never re-walks the whole history.</summary>
    public DateOnly? LastDueOn { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
