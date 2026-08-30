using RandomTaskTrack.Data.Models.Finance;
using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Finance;

public class CreateDebtResponse : BaseResponse
{
    public Debt Debt { get; set; } = new();
}
