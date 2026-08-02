using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Recurrences;

public class CreateRecurrenceRequest : AuthenticatedRequest
{
    public int DomainId { get; set; }
    public string Title { get; set; } = "";
    public string? Notes { get; set; }
    public string? Data { get; set; }

    public RecurrenceRuleType RuleType { get; set; }
    public int? IntervalDays { get; set; }

    /// <summary>0 = Sunday … 6 = Saturday.</summary>
    public int[]? DaysOfWeek { get; set; }

    public int? DayOfMonth { get; set; }
    public RecurrenceAnchorMode AnchorMode { get; set; } = RecurrenceAnchorMode.FromSchedule;
    public TimeOnly? TimeOfDay { get; set; }
    public DateOnly? StartsOn { get; set; }
    public DateOnly? EndsOn { get; set; }
}
