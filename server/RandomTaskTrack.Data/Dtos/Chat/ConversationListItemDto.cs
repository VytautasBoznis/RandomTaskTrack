namespace RandomTaskTrack.Data.Dtos.Chat;

public class ConversationListItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public int? DomainId { get; set; }
    public int MessageCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
