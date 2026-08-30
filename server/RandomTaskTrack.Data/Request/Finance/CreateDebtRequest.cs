using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Finance;

public class CreateDebtRequest : AuthenticatedRequest
{
    public string Name { get; set; } = "";

    /// <summary>What is borrowed at origination, not what is left.</summary>
    public decimal Principal { get; set; }

    public string Currency { get; set; } = "";

    /// <summary>A percentage as the lender writes it: 3.25 means 3.25%.</summary>
    public decimal AnnualRate { get; set; }

    /// <summary>Monthly. Has to cover the first month's interest — see the validator.</summary>
    public decimal Payment { get; set; }

    public DateOnly StartsOn { get; set; }

    /// <summary>The contractual last payment. Null means "until it is paid off".</summary>
    public DateOnly? EndsOn { get; set; }

    /// <summary>What the borrowing buys, at today's value. Held flat.</summary>
    public decimal? AssetValue { get; set; }

    public decimal? DownPayment { get; set; }

    /// <summary>
    /// The account the downpayment comes out of. Leave unset for a debt that
    /// already exists — its cash moved before it was tracked here.
    /// </summary>
    public Guid? DownPaymentAccountId { get; set; }

    /// <summary>
    /// Where the borrowed principal lands. Leave unset for a mortgage: the bank
    /// pays the seller and the money never passes through an account of yours.
    /// </summary>
    public Guid? DisbursesToAccountId { get; set; }

    public string? Note { get; set; }
}
