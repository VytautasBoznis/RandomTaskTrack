using RandomTaskTrack.Data.Models.Notes;
using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Notes;

public class GetNotesResponse : BaseResponse
{
    public List<Note> Notes { get; set; } = new();
}