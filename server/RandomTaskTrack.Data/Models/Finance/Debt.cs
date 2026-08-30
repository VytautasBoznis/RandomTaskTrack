namespace RandomTaskTrack.Data.Models.Finance;

/// <summary>
/// Money owed on a schedule — a mortgage, a car loan, a lease. The mirror of a
/// <see cref="Deposit"/>: growth is contractual rather than assumed, so the
/// projection can value it exactly, it just points the other way.
///
/// Nothing here is a balance. What is left to pay is amortised from these terms
/// plus the overpayments, in <c>FinanceProjector</c>, for the same reason an
/// account has no balance column: a stored total is the one number that can
/// disagree with everything else.
/// </summary>
public class Debt
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";

    /// <summary>What was borrowed at origination, not what is left.</summary>
    public decimal Principal { get; set; }

    public string Currency { get; set; } = "";

    /// <summary>
    /// A percentage as the lender writes it: 3.25 means 3.25%, not 0.0325.
    /// Zero degenerates the schedule to a flat drawdown, which is what an
    /// interest-free instalment plan is.
    /// </summary>
    public decimal AnnualRate { get; set; }

    /// <summary>Monthly, always. See the migration for why there is no cadence.</summary>
    public decimal Payment { get; set; }

    /// <summary>The first payment, and the day the one-off transfers move.</summary>
    public DateOnly StartsOn { get; set; }

    /// <summary>
    /// The contractual last payment. Null means "until it is paid off". Either
    /// way the payoff month is derived, never read from here — that is what
    /// makes an overpayment visibly pull the date in.
    /// </summary>
    public DateOnly? EndsOn { get; set; }

    /// <summary>
    /// What the borrowing bought, at today's value, held flat. Null for a debt
    /// that bought nothing you would count. Without it, signing for a flat
    /// reads as a loss the size of the mortgage.
    /// </summary>
    public decimal? AssetValue { get; set; }

    /// <summary>What you put down. Recorded whether or not it moves any money.</summary>
    public decimal? DownPayment { get; set; }

    /// <summary>
    /// Named, the downpayment leaves this account on <see cref="StartsOn"/> and
    /// no entry should be logged for it. Null for a debt taken out before it was
    /// tracked here — that cash moved long ago and is already in the balance.
    /// </summary>
    public Guid? DownPaymentAccountId { get; set; }

    /// <summary>
    /// Named, the borrowed principal lands here on <see cref="StartsOn"/>. Null
    /// for a mortgage, where the bank pays the seller and the money never
    /// touches an account of yours.
    /// </summary>
    public Guid? DisbursesToAccountId { get; set; }

    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
