using RandomTaskTrack.Data.Models.Enums;

namespace RandomTaskTrack.Data.Models.Learning;

/// <summary>
/// A line being committed to a path — off the drafted plan, or typed by hand.
/// The UI fills this straight from a <see cref="LearningResource"/>, a
/// <see cref="LearningProject"/> or a <see cref="LearningCertificationSuggestion"/>,
/// which is why those carry the same fields.
/// </summary>
public class LearningStepInput
{
    public string Title { get; set; } = "";
    public LearningStepKind Kind { get; set; } = LearningStepKind.Study;
    public DateOnly? TargetOn { get; set; }
    public string? Notes { get; set; }
    public string? Provider { get; set; }
    public string? Url { get; set; }
    public string? Cost { get; set; }
    public int? Hours { get; set; }
}
