using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Learning;

public class DeleteLearningGoalRequest : AuthenticatedRequest
{
    public Guid Id { get; set; }
}
