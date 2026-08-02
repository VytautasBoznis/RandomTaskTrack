using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Tasks;

public class GetDashboardRequest : AuthenticatedRequest
{
    /// <summary>How many days ahead the "Upcoming" bucket reaches.</summary>
    public int UpcomingDays { get; set; } = 7;
}
