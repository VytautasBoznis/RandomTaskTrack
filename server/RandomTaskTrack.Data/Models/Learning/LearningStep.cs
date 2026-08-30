using RandomTaskTrack.Data.Models.Enums;

namespace RandomTaskTrack.Data.Models.Learning;

/// <summary>
/// One committed line of a path. Whatever it came from — a drafted plan, the
/// chat agent, the form — it is a step once it is here, and a re-draft leaves
/// it alone.
/// </summary>
public class LearningStep
{
    public Guid Id { get; set; }
    public Guid GoalId { get; set; }

    public string Title { get; set; } = "";
    public LearningStepKind Kind { get; set; } = LearningStepKind.Study;
    public LearningStepStatus Status { get; set; } = LearningStepStatus.Planned;

    public DateOnly? TargetOn { get; set; }

    /// <summary>What to do. Written up front.</summary>
    public string Notes { get; set; } = "";

    /// <summary>
    /// What happened. The grade, the mark, "failed the lab section, retake
    /// booked 12 Jan". Written afterwards, and the reason an assignment needs
    /// no table of its own.
    /// </summary>
    public string Outcome { get; set; } = "";

    /// <summary>"Udemy", "TryHackMe", "Microsoft Learn". Survives a dead url.</summary>
    public string? Provider { get; set; }

    /// <summary>Best-effort. See LearningPlan.Resources for why it is not the handle.</summary>
    public string? Url { get; set; }

    /// <summary>Text, not money: "€14.99 on sale", "free with the subscription".</summary>
    public string? Cost { get; set; }

    /// <summary>Rough effort in hours, for sequencing against the time available.</summary>
    public int? Hours { get; set; }

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
