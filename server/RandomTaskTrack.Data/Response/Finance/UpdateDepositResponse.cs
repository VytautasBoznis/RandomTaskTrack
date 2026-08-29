using RandomTaskTrack.Data.Models.Finance;
using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Finance;

public class UpdateDepositResponse : BaseResponse
{
    public Deposit Deposit { get; set; } = new();
}
