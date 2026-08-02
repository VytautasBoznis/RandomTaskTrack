using RandomTaskTrack.Data.Dtos.Tasks;
using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Tasks;

public class GetDashboardResponse : BaseResponse
{
    public DashboardDto Dashboard { get; set; } = new();
}
