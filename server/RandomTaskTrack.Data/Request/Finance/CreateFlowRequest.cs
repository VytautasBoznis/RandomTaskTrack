using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Finance;

public class CreateFlowRequest : AuthenticatedRequest
{
    public FinanceFlowKind Kind { get; set; }
    public string Name { get; set; } = "";

    /// <summary>Positive. Kind carries the direction.</summary>
    public decimal Amount { get; set; }

    public string Currency { get; set; } = "";
    public FinanceCadence Cadence { get; set; }

    /// <summary>Optional override. Null anchors on StartsOn.</summary>
    public int? DayOfMonth { get; set; }

    public int? MonthOfYear { get; set; }
    public DateOnly StartsOn { get; set; }
    public DateOnly? EndsOn { get; set; }
    public string? Category { get; set; }
}
