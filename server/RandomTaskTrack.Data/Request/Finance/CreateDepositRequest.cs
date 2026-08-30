using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Finance;

public class CreateDepositRequest : AuthenticatedRequest
{
    public string Name { get; set; } = "";
    public decimal Principal { get; set; }
    public string Currency { get; set; } = "";

    /// <summary>A percentage as the bank writes it: 4.25 means 4.25%.</summary>
    public decimal AnnualRate { get; set; }

    public DepositCompounding? Compounding { get; set; }
    public DateOnly OpenedOn { get; set; }

    /// <summary>Null is open-ended: it accrues and never returns to cash on its own.</summary>
    public DateOnly? MaturesOn { get; set; }

    /// <summary>
    /// The account the principal comes out of. Optional: leave it unset to
    /// record a deposit whose transfer you logged as an entry yourself.
    /// </summary>
    public Guid? SourceAccountId { get; set; }

    /// <summary>Where it lands at maturity. Defaults to the source.</summary>
    public Guid? TargetAccountId { get; set; }

    public string? Note { get; set; }
}
