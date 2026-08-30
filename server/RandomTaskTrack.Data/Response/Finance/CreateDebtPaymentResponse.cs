using RandomTaskTrack.Data.Models.Finance;
using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Finance;

public class CreateDebtPaymentResponse : BaseResponse
{
    public DebtPayment Payment { get; set; } = new();
}
