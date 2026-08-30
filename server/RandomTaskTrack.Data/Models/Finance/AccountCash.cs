namespace RandomTaskTrack.Data.Models.Finance;

/// <summary>
/// One account's ledger total, still in the currency it was logged in.
/// Conversion happens in one place — the projector — so the SQL never has to
/// join the rate table and the rounding never happens twice.
/// </summary>
public class AccountCash
{
    public Guid AccountId { get; set; }
    public string Currency { get; set; } = "";
    public decimal Amount { get; set; }
}
