using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Finance;

public class DeleteAccountRequest : AuthenticatedRequest
{
    public Guid Id { get; set; }
}
