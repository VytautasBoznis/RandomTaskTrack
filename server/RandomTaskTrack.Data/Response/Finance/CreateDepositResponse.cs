using RandomTaskTrack.Data.Models.Finance;
using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Finance;

public class CreateDepositResponse : BaseResponse
{
    public Deposit Deposit { get; set; } = new();
}
