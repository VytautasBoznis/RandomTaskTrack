namespace RandomTaskTrack.Data.Models.Notes;

public class Note
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";

    /// <summary>Markdown, stored and returned verbatim. Rendered by the UI.</summary>
    public string Content { get; set; } = "";

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}