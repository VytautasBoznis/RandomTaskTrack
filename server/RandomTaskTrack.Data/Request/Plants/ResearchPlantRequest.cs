using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Plants;

public class ResearchPlantRequest : AuthenticatedRequest
{
    public Guid Id { get; set; }

    /// <summary>
    /// A better description to ask with — "it flowered, they're white and
    /// waxy". Null re-asks with the one already stored, which is the retry
    /// case after a failed lookup.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Show it the newest photo. Default true: if there is one, the lookup
    /// should be looking at it, and the caller has to opt out rather than
    /// remember to opt in.
    /// </summary>
    public bool UsePhoto { get; set; } = true;
}
