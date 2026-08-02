using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Domains;

public class GetDomainsRequest : AuthenticatedRequest
{
    public bool IncludeInactive { get; set; }
}
