namespace RandomTaskTrack.Data.Models.Chat;

public class ChatConversation
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public int? DomainId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
