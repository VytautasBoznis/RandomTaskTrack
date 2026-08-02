namespace RandomTaskTrack.Data.Dtos.Chat;

public class ConversationDetailDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public int? DomainId { get; set; }
    public List<ChatMessageDto> Messages { get; set; } = new();
}
