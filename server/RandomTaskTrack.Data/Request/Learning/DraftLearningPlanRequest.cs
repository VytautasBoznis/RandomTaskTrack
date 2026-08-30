using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Learning;

/// <summary>
/// Drafts the path, or drafts it again with a better brief. The steps already
/// committed to are untouched — a re-draft replaces the suggestion, never the
/// commitment.
/// </summary>
public class DraftLearningPlanRequest : AuthenticatedRequest
{
    public Guid Id { get; set; }

    /// <summary>
    /// A better brief. Replaces the stored one when given, for the reason
    /// ResearchPlantRequest.Description does: it is what the next re-draft
    /// should ask, and a plan should be traceable to the question behind it.
    /// </summary>
    public string? Context { get; set; }
}
