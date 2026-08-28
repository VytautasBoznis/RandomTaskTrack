using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Notes;

public class CreateNoteRequest : AuthenticatedRequest
{
    public string Title { get; set; } = "";

    /// <summary>Markdown. Empty is fine — a note can start as a title.</summary>
    public string? Content { get; set; }
}