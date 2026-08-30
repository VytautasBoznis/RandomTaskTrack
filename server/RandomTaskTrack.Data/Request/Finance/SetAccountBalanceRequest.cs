using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Finance;

/// <summary>
/// "The bank says 4,180." The balance itself is not stored, so this is turned
/// into one adjustment entry for the difference — which keeps the total
/// explained by the ledger instead of being a number that can quietly rot.
/// </summary>
public class SetAccountBalanceRequest : AuthenticatedRequest
{
    public Guid Id { get; set; }

    /// <summary>
    /// What the balance should read, in the account's own currency. May be
    /// negative — an overdrawn account is a real thing.
    /// </summary>
    public decimal Balance { get; set; }

    /// <summary>When the adjustment happened. Defaults to today.</summary>
    public DateOnly? OccurredOn { get; set; }

    public string? Note { get; set; }
}
