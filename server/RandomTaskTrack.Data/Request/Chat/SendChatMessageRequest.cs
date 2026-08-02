using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Chat;

public class SendChatMessageRequest : AuthenticatedRequest
{
    /// <summary>Null starts a new conversation.</summary>
    public Guid? ConversationId { get; set; }

    public string Message { get; set; } = "";

    /// <summary>Scopes the system prompt to one tracker when set.</summary>
    public int? DomainId { get; set; }
}
