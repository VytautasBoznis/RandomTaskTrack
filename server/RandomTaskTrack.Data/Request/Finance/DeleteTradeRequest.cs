using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Finance;

public class DeleteTradeRequest : AuthenticatedRequest
{
    public Guid Id { get; set; }
}
