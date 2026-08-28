using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Notes;

public class DeleteNoteResponse : BaseResponse
{
    public bool Success { get; set; }
}