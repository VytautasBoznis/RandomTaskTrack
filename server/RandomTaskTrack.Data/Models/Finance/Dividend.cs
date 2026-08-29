using RandomTaskTrack.Data.Models.Enums;

namespace RandomTaskTrack.Data.Models.Finance;

/// <summary>
/// A dividend you <i>expect</i> to be paid. A dividend that actually landed is
/// a <see cref="LedgerEntry"/> like any other income.
/// </summary>
public class Dividend
{
    public Guid Id { get; set; }

    /// <summary>
    /// Optional. Attached it documents which position pays; detached it still
    /// projects, so a payer you do not track as a holding still shows up.
    /// </summary>
    public Guid? HoldingId { get; set; }

    public string Name { get; set; } = "";

    /// <summary>Per payment, not per year. A quarterly dividend is four of these.</summary>
    public decimal Amount { get; set; }

    public string Currency { get; set; } = "";
    public FinanceCadence Cadence { get; set; }
    public int? DayOfMonth { get; set; }
    public int? MonthOfYear { get; set; }
    public DateOnly StartsOn { get; set; }
    public DateOnly? EndsOn { get; set; }
    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
