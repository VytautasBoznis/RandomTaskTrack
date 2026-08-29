namespace RandomTaskTrack.Data.Models.Finance;

/// <summary>
/// A total still in its own currency. Conversion happens in one place — the
/// projector — so the SQL never has to join the rate table and the rounding
/// never happens twice.
/// </summary>
public class CurrencyAmount
{
    public string Currency { get; set; } = "";
    public decimal Amount { get; set; }
}
