using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Tasks;

public class DeleteTaskRequest : AuthenticatedRequest
{
    public Guid Id { get; set; }
}
