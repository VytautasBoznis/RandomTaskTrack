using RandomTaskTrack.Data.Models.Finance;
using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Finance;

public class CreateFlowResponse : BaseResponse
{
    public FinanceFlow Flow { get; set; } = new();
}
