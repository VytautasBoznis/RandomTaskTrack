using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Finance;

public class UpdateDepositRequest : AuthenticatedRequest
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public decimal? Principal { get; set; }
    public string? Currency { get; set; }
    public decimal? AnnualRate { get; set; }
    public DepositCompounding? Compounding { get; set; }
    public DateOnly? OpenedOn { get; set; }
    public DateOnly? MaturesOn { get; set; }
    public Guid? SourceAccountId { get; set; }
    public Guid? TargetAccountId { get; set; }
    public string? Note { get; set; }
}
