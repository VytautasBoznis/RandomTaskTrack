using RandomTaskTrack.Data.Dtos.Learning;
using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Learning;

public class CreateLearningStepsResponse : BaseResponse
{
    public LearningGoalDto Goal { get; set; } = new();

    /// <summary>How many were new. The rest were already on the path — the
    /// response carries the goal back so nothing is hidden by the dedupe.</summary>
    public int CreatedStepCount { get; set; }
}
