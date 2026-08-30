using RandomTaskTrack.Data.Models.Enums;

namespace RandomTaskTrack.Data.Models.Learning;

public class LearningGoal
{
    public Guid Id { get; set; }

    /// <summary>"Architect promotion", "Purple Team SecOps", "Spanish".</summary>
    public string Title { get; set; } = "";

    public LearningTier Tier { get; set; } = LearningTier.NiceToHave;
    public LearningGoalStatus Status { get; set; } = LearningGoalStatus.Active;

    /// <summary>Why this is worth doing. The user's words; a draft never touches them.</summary>
    public string Why { get; set; } = "";

    /// <summary>What is expected out of it — the promotion, the pay, the door it opens.</summary>
    public string Benefits { get; set; } = "";

    /// <summary>"Prepared by". A direction, not a deadline anything enforces.</summary>
    public DateOnly? TargetOn { get; set; }

    /// <summary>The free text the plan was drafted from: current level, hours a week.</summary>
    public string Context { get; set; } = "";

    /// <summary>Raw jsonb. Shaped like LearningPlan; '{}' until drafted.</summary>
    public string Plan { get; set; } = "{}";

    public DateTime? ResearchedAt { get; set; }

    /// <summary>Which model drafted it, so an odd plan can be attributed.</summary>
    public string? ResearchModel { get; set; }

    public string Notes { get; set; } = "";

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
