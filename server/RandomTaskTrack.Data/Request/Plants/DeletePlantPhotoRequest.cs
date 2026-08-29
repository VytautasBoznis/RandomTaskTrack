using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Plants;

public class DeletePlantPhotoRequest : AuthenticatedRequest
{
    public Guid PhotoId { get; set; }
}
