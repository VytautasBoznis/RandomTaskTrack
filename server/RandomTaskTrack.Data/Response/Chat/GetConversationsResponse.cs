using RandomTaskTrack.Data.Dtos.Chat;
using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Chat;

public class GetConversationsResponse : BaseResponse
{
    public List<ConversationListItemDto> Conversations { get; set; } = new();
}
