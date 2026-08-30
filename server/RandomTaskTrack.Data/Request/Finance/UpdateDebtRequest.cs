using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Finance;

public class UpdateDebtRequest : AuthenticatedRequest
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public decimal? Principal { get; set; }
    public string? Currency { get; set; }
    public decimal? AnnualRate { get; set; }
    public decimal? Payment { get; set; }
    public DateOnly? StartsOn { get; set; }
    public DateOnly? EndsOn { get; set; }
    public decimal? AssetValue { get; set; }
    public decimal? DownPayment { get; set; }
    public Guid? DownPaymentAccountId { get; set; }
    public Guid? DisbursesToAccountId { get; set; }
    public string? Note { get; set; }
}
