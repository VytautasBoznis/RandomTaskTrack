using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Finance;

public class DeleteDebtRequest : AuthenticatedRequest
{
    public Guid Id { get; set; }
}
