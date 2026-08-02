namespace RandomTaskTrack.Data.Dtos.Chat;

public class ChatMessageDto
{
    public Guid Id { get; set; }
    public int Seq { get; set; }
    public string Role { get; set; } = "";
    public string? Content { get; set; }
    public string? ToolCalls { get; set; }
    public DateTime CreatedAt { get; set; }
}
