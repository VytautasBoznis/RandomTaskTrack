using RandomTaskTrack.Data.Authentication;

namespace RandomTaskTrack.Data.Request.Base;

public class AuthenticatedRequest : BaseRequest
{
    public SessionUserData SessionUserData { get; set; } = new();
}
