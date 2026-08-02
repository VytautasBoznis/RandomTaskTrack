using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Auth;

public class LoginRequest : BaseRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}
