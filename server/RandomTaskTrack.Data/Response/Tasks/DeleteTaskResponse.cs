using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Tasks;

public class DeleteTaskResponse : BaseResponse
{
    public bool Success { get; set; }
}
