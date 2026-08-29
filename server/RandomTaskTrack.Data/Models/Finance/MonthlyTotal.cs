namespace RandomTaskTrack.Data.Models.Finance;

/// <summary>One month of the ledger, still per currency — see <see cref="CurrencyAmount"/>.</summary>
public class MonthlyTotal
{
    /// <summary>First of the month.</summary>
    public DateOnly Month { get; set; }

    public string Currency { get; set; } = "";
    public decimal Income { get; set; }
    public decimal Expenses { get; set; }
}
