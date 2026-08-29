using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Plants;

public class GetPlantPhotoRequest : AuthenticatedRequest
{
    public Guid PhotoId { get; set; }
}
