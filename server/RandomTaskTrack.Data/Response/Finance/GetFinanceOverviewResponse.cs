using RandomTaskTrack.Data.Dtos.Finance;
using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Finance;

public class GetFinanceOverviewResponse : BaseResponse
{
    public FinanceOverviewDto Overview { get; set; } = new();
}
