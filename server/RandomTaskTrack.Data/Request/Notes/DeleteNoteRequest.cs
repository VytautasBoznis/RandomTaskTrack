using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Notes;

public class DeleteNoteRequest : AuthenticatedRequest
{
    public Guid Id { get; set; }
}