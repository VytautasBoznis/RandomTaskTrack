using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Learning;

public class UpdateLearningGoalRequest : AuthenticatedRequest
{
    public Guid Id { get; set; }

    public string Title { get; set; } = "";
    public LearningTier Tier { get; set; }
    public LearningGoalStatus Status { get; set; }

    public string? Why { get; set; }
    public string? Benefits { get; set; }
    public DateOnly? TargetOn { get; set; }
    public string? Context { get; set; }
    public string? Notes { get; set; }
}
