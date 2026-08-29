using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Finance;

public class DeleteTargetRequest : AuthenticatedRequest
{
    public Guid Id { get; set; }
}
