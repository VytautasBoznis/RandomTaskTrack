using RandomTaskTrack.Data.Dtos.Learning;
using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Learning;

public class DraftLearningPlanResponse : BaseResponse
{
    public LearningGoalDto Goal { get; set; } = new();
}
