using RandomTaskTrack.Data.Response.Auth.Dto;
using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Auth;

public class LoginResponse : BaseResponse
{
    public SessionDto Session { get; set; } = new();
}
