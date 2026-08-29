using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Finance;

public class UpdateDividendRequest : AuthenticatedRequest
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
    public bool? IsActive { get; set; }
}
