using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Finance;

public class DeleteDepositRequest : AuthenticatedRequest
{
    public Guid Id { get; set; }
}
