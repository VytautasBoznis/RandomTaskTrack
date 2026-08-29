using RandomTaskTrack.Data.Models.Enums;

namespace RandomTaskTrack.Data.Models.Finance;

/// <summary>
/// A recurring income or expense — what is <i>supposed</i> to happen. What
/// actually happened is a <see cref="LedgerEntry"/>.
/// </summary>
public class FinanceFlow
{
    public Guid Id { get; set; }
    public FinanceFlowKind Kind { get; set; }
    public string Name { get; set; } = "";

    /// <summary>Always positive. <see cref="Kind"/> carries the direction.</summary>
    public decimal Amount { get; set; }

    public string Currency { get; set; } = "";
    public FinanceCadence Cadence { get; set; }

    /// <summary>
    /// Optional override for when the calendar day matters more than the
    /// anchor — a salary on the 25th. Null means "same day as StartsOn".
    /// </summary>
    public int? DayOfMonth { get; set; }

    /// <summary>Optional override for yearly flows. Null means "same month as StartsOn".</summary>
    public int? MonthOfYear { get; set; }

    public DateOnly StartsOn { get; set; }
    public DateOnly? EndsOn { get; set; }
    public string? Category { get; set; }
    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
