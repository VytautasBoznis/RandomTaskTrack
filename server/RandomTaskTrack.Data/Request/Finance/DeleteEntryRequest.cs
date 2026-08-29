using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Finance;

public class DeleteEntryRequest : AuthenticatedRequest
{
    public Guid Id { get; set; }
}
