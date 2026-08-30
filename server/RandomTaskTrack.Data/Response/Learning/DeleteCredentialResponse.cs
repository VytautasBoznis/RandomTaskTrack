using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Learning;

public class DeleteCredentialResponse : BaseResponse
{
    public bool Success { get; set; }
    public int DeletedTaskCount { get; set; }
}
