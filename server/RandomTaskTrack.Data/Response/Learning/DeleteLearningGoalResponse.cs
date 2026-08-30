using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Learning;

public class DeleteLearningGoalResponse : BaseResponse
{
    public bool Success { get; set; }

    /// <summary>Pending tasks the steps had on the board, swept up on the way
    /// out — see DeleteLearningGoalOperation for why that is manual.</summary>
    public int DeletedTaskCount { get; set; }
}
