using RandomTaskTrack.Data.Models.Finance;

namespace RandomTaskTrack.Data.Dtos.Finance;

/// <summary>
/// A debt with its schedule run. Everything below <see cref="Note"/> is
/// amortised from the terms and the overpayments rather than stored, so
/// correcting a mistyped chunk corrects the balance, the payoff date and the
/// interest saved together — the same bargain <see cref="PositionDto"/> makes
/// with its trades.
/// </summary>
public class DebtDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";

    /// <summary>What was borrowed at origination, not what is left.</summary>
    public decimal Principal { get; set; }

    public string Currency { get; set; } = "";
    public decimal AnnualRate { get; set; }
    public decimal Payment { get; set; }
    public DateOnly StartsOn { get; set; }

    /// <summary>The contract's last payment. Compare with <see cref="PaidOffOn"/>.</summary>
    public DateOnly? EndsOn { get; set; }

    public decimal? AssetValue { get; set; }
    public decimal? DownPayment { get; set; }
    public Guid? DownPaymentAccountId { get; set; }
    public Guid? DisbursesToAccountId { get; set; }
    public string? Note { get; set; }

    // ── Derived ──────────────────────────────────────────────────────────────

    /// <summary>
    /// What is still owed today, in the debt's own currency.
    ///
    /// Zero means one of two opposite things — paid off, or not taken out yet —
    /// so read it against <see cref="StartsOn"/> rather than on its own. A
    /// mortgage you sign next June is not money you owe in March.
    /// </summary>
    public decimal Outstanding { get; set; }

    /// <summary>The same figure in base, for the totals and the chart.</summary>
    public decimal OutstandingBase { get; set; }

    /// <summary>What it bought, in base. Null when the debt bought nothing counted.</summary>
    public decimal? AssetValueBase { get; set; }

    /// <summary>The monthly payment in base, for the "a typical month" figure.</summary>
    public decimal PaymentBase { get; set; }

    /// <summary>
    /// The month the balance actually reaches zero. Earlier than
    /// <see cref="EndsOn"/> once overpayments have bitten, which is the whole
    /// point of tracking them. Null if the schedule does not clear inside the
    /// 50-year cap — a payment that barely covers the interest.
    /// </summary>
    public DateOnly? PaidOffOn { get; set; }

    /// <summary>
    /// Still standing on <see cref="EndsOn"/> when the payments run out before
    /// the balance does: a lease residual, a balloon. Zero for a debt that
    /// clears itself. Reported rather than quietly paid off, because it is a
    /// cheque somebody has to write.
    /// </summary>
    public decimal BalloonBase { get; set; }

    /// <summary>
    /// Interest still to be paid from today to the payoff, in base. What an
    /// overpayment buys you, and the number that makes the case for making one.
    /// </summary>
    public decimal InterestRemainingBase { get; set; }

    /// <summary>Lump sums off the principal, newest first.</summary>
    public List<DebtPayment> Payments { get; set; } = new();
}
