using RandomTaskTrack.Data.Models.Learning;
using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Learning;

/// <summary>
/// Commits chosen lines of the drafted plan — or hand-typed ones — to the path.
/// Bulk, because picking four resources off a plan is one gesture, and deduped
/// by title the way CreatePlantScheduleOperation dedupes care lines.
/// </summary>
public class CreateLearningStepsRequest : AuthenticatedRequest
{
    public Guid Id { get; set; }

    public List<LearningStepInput> Steps { get; set; } = new();
}
