using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Notes;

public class UpdateNoteRequest : AuthenticatedRequest
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
}