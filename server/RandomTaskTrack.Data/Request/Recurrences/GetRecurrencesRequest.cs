using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Recurrences;

public class GetRecurrencesRequest : AuthenticatedRequest
{
    public int? DomainId { get; set; }
    public bool IncludeInactive { get; set; }
}
