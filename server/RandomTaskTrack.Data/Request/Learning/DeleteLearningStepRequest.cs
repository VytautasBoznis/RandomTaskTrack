using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Learning;

public class DeleteLearningStepRequest : AuthenticatedRequest
{
    public Guid Id { get; set; }
}
