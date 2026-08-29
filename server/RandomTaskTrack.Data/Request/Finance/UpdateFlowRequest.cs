using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Finance;

/// <summary>
/// Only the fields you pass are changed, as UpdateRecurrenceRequest does.
/// Kind is absent on purpose: turning an income into an expense is a delete and
/// a re-create, not an edit, and silently flipping the sign of history is not
/// something a typo should be able to do.
/// </summary>
public class UpdateFlowRequest : AuthenticatedRequest
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }
    public FinanceCadence? Cadence { get; set; }
    public int? DayOfMonth { get; set; }
    public int? MonthOfYear { get; set; }
    public DateOnly? StartsOn { get; set; }
    public DateOnly? EndsOn { get; set; }
    public string? Category { get; set; }
    public bool? IsActive { get; set; }
}
