using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Learning;

public class DeleteCredentialRequest : AuthenticatedRequest
{
    public Guid Id { get; set; }
}
