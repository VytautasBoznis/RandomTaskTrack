using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Auth;

public class RegisterResponse : BaseResponse
{
    public Guid UserId { get; set; }
}
