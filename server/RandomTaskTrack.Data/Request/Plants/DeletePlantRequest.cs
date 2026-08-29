using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Plants;

public class DeletePlantRequest : AuthenticatedRequest
{
    public Guid Id { get; set; }
}
