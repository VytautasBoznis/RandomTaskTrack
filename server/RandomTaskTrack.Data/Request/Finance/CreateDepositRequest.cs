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

    public string? Note { get; set; }
}
