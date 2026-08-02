using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Chat;

public class GetConversationsRequest : AuthenticatedRequest
{
    public int Limit { get; set; } = 50;
}
