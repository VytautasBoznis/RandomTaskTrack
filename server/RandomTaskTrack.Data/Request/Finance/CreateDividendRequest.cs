using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Finance;

public class CreateDividendRequest : AuthenticatedRequest
{
    /// <summary>Optional — a payer you do not track as a holding still projects.</summary>
    public Guid? HoldingId { get; set; }

    public string Name { get; set; } = "";

    /// <summary>Per payment, not per year.</summary>
    public decimal Amount { get; set; }

    public string Currency { get; set; } = "";
    public FinanceCadence Cadence { get; set; }
    public int? DayOfMonth { get; set; }
    public int? MonthOfYear { get; set; }
    public DateOnly StartsOn { get; set; }
    public DateOnly? EndsOn { get; set; }
}
