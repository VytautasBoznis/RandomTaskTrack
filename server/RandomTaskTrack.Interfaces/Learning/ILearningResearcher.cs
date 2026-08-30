using RandomTaskTrack.Data.Models.Learning;

namespace RandomTaskTrack.Interfaces.Learning;

/// <summary>
/// The two questions this scope cannot answer by itself: "how do I get from
/// here to there?" and "does this thing I already hold expire?"
///
/// One shot each, no tools of ours, no conversation to persist — the answers
/// are a plan and a renewal rule, not a chat. Iterating on a path afterwards is
/// what the chat agent's learning tools are for.
/// </summary>
public interface ILearningResearcher
{
    /// <summary>
    /// Drafts a route to a goal: phases, the certifications worth sitting, the
    /// courses and labs to use, and projects pitched at a level. Web-search
    /// backed, because syllabi, prices and course catalogues all move.
    /// </summary>
    Task<LearningPlanResult> DraftPlanAsync(LearningPlanQuestion question, CancellationToken cancellationToken);

    /// <summary>
    /// Finds out how a held credential renews — including that it does not.
    /// The result is already reconciled: an answer that claims to be permanent
    /// and also carries a validity period comes back as Unknown rather than as
    /// a date nobody checked.
    /// </summary>
    Task<CredentialResearchResult> ResearchCredentialAsync(CredentialQuestion question, CancellationToken cancellationToken);
}
