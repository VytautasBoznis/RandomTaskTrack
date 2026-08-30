using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Models.Learning;

namespace RandomTaskTrack.Data.Dtos.Learning;

/// <summary>
/// A path with everything its card renders: the motivation, the drafted route
/// and the steps committed to so far. The whole tab is one round trip, the same
/// bargain /api/plants and /tasks/dashboard make.
/// </summary>
public class LearningGoalDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public LearningTier Tier { get; set; }
    public LearningGoalStatus Status { get; set; }

    public string Why { get; set; } = "";
    public string Benefits { get; set; } = "";
    public DateOnly? TargetOn { get; set; }
    public string Context { get; set; } = "";

    /// <summary>Null until the first successful draft.</summary>
    public LearningPlan? Plan { get; set; }

    public DateTime? ResearchedAt { get; set; }
    public string? ResearchModel { get; set; }
    public string Notes { get; set; } = "";

    public List<LearningStepDto> Steps { get; set; } = new();

    /// <summary>
    /// Days until <see cref="TargetOn"/>, negative once it has passed. Derived
    /// server-side so the countdown agrees with the dates the tasks were filed
    /// under rather than with the tablet's own clock.
    /// </summary>
    public int? DaysUntilTarget { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
