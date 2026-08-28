using RandomTaskTrack.Data.Models.Notes;
using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Notes;

public class CreateNoteResponse : BaseResponse
{
    public Note Note { get; set; } = new();
}