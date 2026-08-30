using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Learning;

public class UpdateLearningStepRequest : AuthenticatedRequest
{
    public Guid Id { get; set; }

    public string Title { get; set; } = "";
    public LearningStepKind Kind { get; set; }
    public LearningStepStatus Status { get; set; }

    public DateOnly? TargetOn { get; set; }
    public string? Notes { get; set; }

    /// <summary>The grade, the mark, the retake. This is the field an assignment
    /// exists for, so it is editable long after the step is done.</summary>
    public string? Outcome { get; set; }

    public string? Provider { get; set; }
    public string? Url { get; set; }
    public string? Cost { get; set; }
    public int? Hours { get; set; }
    public int SortOrder { get; set; }
}
