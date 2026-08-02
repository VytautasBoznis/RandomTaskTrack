using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Auth;

public class ChangePasswordResponse : BaseResponse
{
    public bool Success { get; set; }
}
