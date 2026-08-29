using RandomTaskTrack.Data.Dtos.Plants;
using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Plants;

public class AddPlantPhotoResponse : BaseResponse
{
    public PlantDto Plant { get; set; } = new();

    /// <summary>
    /// Set when the photo was stored but the AI read of it did not happen. The
    /// photo is a stage either way — same bargain the create path makes.
    /// </summary>
    public string? ReadError { get; set; }
}
