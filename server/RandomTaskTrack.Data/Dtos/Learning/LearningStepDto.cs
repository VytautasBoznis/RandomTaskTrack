using RandomTaskTrack.Data.Dtos.Tasks;
using RandomTaskTrack.Data.Models.Enums;

namespace RandomTaskTrack.Data.Dtos.Learning;

public class LearningStepDto
{
    public Guid Id { get; set; }
    public Guid GoalId { get; set; }

    public string Title { get; set; } = "";
    public LearningStepKind Kind { get; set; }
    public LearningStepStatus Status { get; set; }

    public DateOnly? TargetOn { get; set; }
    public string Notes { get; set; } = "";

    /// <summary>The grade, the retake, the comment. The row badges when this is set.</summary>
    public string Outcome { get; set; } = "";

    public string? Provider { get; set; }
    public string? Url { get; set; }
    public string? Cost { get; set; }
    public int? Hours { get; set; }
    public int SortOrder { get; set; }

    /// <summary>
    /// The pending task this step has on the board, if it has one. Null is what
    /// the "Put on board" button reads to know it is still worth offering.
    /// </summary>
    public TaskListItemDto? Task { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
