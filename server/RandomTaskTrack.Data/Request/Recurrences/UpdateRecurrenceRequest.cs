using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Recurrences;

public class UpdateRecurrenceRequest : AuthenticatedRequest
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public string? Notes { get; set; }
    public string? Data { get; set; }
    public RecurrenceRuleType? RuleType { get; set; }
    public int? IntervalDays { get; set; }
    public int[]? DaysOfWeek { get; set; }
    public int? DayOfMonth { get; set; }
    public RecurrenceAnchorMode? AnchorMode { get; set; }
    public TimeOnly? TimeOfDay { get; set; }
    public DateOnly? EndsOn { get; set; }
    public bool? IsActive { get; set; }
}
