namespace RandomTaskTrack.Data.Models.Chat;

public class ChatMessage
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public int Seq { get; set; }

    /// <summary>"user" | "assistant" | "tool" — provider-neutral, so switching
    /// AI provider does not invalidate stored history.</summary>
    public string Role { get; set; } = "";

    public string? Content { get; set; }
    public string? ToolCalls { get; set; }
    public string? ToolResults { get; set; }
    public string? Model { get; set; }
    public int? InputTokens { get; set; }
    public int? OutputTokens { get; set; }
    public DateTime CreatedAt { get; set; }
}
