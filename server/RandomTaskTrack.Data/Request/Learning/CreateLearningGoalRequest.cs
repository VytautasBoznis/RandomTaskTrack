using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Learning;

/// <summary>
/// Adds a path. Deliberately does not draft the plan: the draft is only as good
/// as <see cref="Context"/>, and that is usually typed on the second pass once
/// the goal is on screen. Drafting is its own button, and its own endpoint,
/// which is also the re-draft.
/// </summary>
public class CreateLearningGoalRequest : AuthenticatedRequest
{
    public string Title { get; set; } = "";
    public LearningTier Tier { get; set; } = LearningTier.NiceToHave;

    public string? Why { get; set; }
    public string? Benefits { get; set; }
    public DateOnly? TargetOn { get; set; }

    /// <summary>Where they are now, hours a week, constraints. What the draft is given.</summary>
    public string? Context { get; set; }

    public string? Notes { get; set; }
}
