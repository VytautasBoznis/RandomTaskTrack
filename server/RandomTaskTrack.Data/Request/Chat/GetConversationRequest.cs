using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Chat;

public class GetConversationRequest : AuthenticatedRequest
{
    public Guid Id { get; set; }
}
