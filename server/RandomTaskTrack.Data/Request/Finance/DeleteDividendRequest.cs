using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Finance;

public class DeleteDividendRequest : AuthenticatedRequest
{
    public Guid Id { get; set; }
}
