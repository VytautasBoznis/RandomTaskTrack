using RandomTaskTrack.Data.Dtos.Chat;
using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Chat;

public class SendChatMessageResponse : BaseResponse
{
    public Guid ConversationId { get; set; }
    public string Reply { get; set; } = "";

    /// <summary>Every write the AI performed this turn, so the UI can show them
    /// instead of asking the user to take it on faith.</summary>
    public List<AppliedToolCallDto> AppliedToolCalls { get; set; } = new();

    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
}
