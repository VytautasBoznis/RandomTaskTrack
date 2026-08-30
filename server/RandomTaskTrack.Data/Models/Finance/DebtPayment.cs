namespace RandomTaskTrack.Data.Models.Finance;

/// <summary>
/// A lump sum off the principal, over and above the monthly payment. What
/// <see cref="Trade"/> is to <see cref="Holding"/>: the events under the terms,
/// summed rather than stored, so fixing a mistyped one fixes the balance, the
/// payoff date and the interest saved at once.
/// </summary>
public class DebtPayment
{
    public Guid Id { get; set; }
    public Guid DebtId { get; set; }

    /// <summary>Always positive. It only ever comes off the principal.</summary>
    public decimal Amount { get; set; }

    public DateOnly PaidOn { get; set; }

    /// <summary>
    /// Named, the cash leaves this account on <see cref="PaidOn"/> and no entry
    /// should be logged for it. Null means you logged it yourself.
    /// </summary>
    public Guid? AccountId { get; set; }

    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}
