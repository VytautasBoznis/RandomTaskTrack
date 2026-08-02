using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Auth;

public class ChangePasswordRequest : AuthenticatedRequest
{
    public string CurrentPassword { get; set; } = "";
    public string NewPassword { get; set; } = "";
}
