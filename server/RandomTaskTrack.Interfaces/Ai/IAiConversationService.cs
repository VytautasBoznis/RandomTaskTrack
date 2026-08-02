using RandomTaskTrack.Data.Dtos.Chat;
using RandomTaskTrack.Interfaces.Base;

namespace RandomTaskTrack.Interfaces.Ai;

public class AiTurnResult
{
    public string Reply { get; set; } = "";
    public List<AppliedToolCallDto> AppliedToolCalls { get; set; } = new();
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public string Model { get; set; } = "";
}

/// <summary>Runs the agent loop: complete → execute tools → feed back → repeat.</summary>
public interface IAiConversationService
{
    Task<AiTurnResult> RunTurnAsync(Guid conversationId, int? domainId, IUnitOfWork unitOfWork, CancellationToken cancellationToken);

    /// <summary>Cheap one-shot used to name a new conversation from its first
    /// message. Failure here is non-fatal — the caller falls back to a slice of
    /// the message text.</summary>
    Task<string> GenerateTitleAsync(string firstMessage, CancellationToken cancellationToken);
}
