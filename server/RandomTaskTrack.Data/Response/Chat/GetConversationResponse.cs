using RandomTaskTrack.Data.Dtos.Chat;
using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Chat;

public class GetConversationResponse : BaseResponse
{
    public ConversationDetailDto Conversation { get; set; } = new();
}
