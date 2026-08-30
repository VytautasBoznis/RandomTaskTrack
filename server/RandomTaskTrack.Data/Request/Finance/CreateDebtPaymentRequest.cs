using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Finance;

/// <summary>
/// A lump sum off the principal, over and above the monthly payment. In the
/// debt's own currency — it has no second currency of its own.
/// </summary>
public class CreateDebtPaymentRequest : AuthenticatedRequest
{
    public Guid DebtId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly PaidOn { get; set; }

    /// <summary>
    /// The account it comes out of. Named, the cash leaves on
    /// <see cref="PaidOn"/> and no entry should be logged for it; leave it unset
    /// for a chunk you have already logged yourself.
    /// </summary>
    public Guid? AccountId { get; set; }

    public string? Note { get; set; }
}
