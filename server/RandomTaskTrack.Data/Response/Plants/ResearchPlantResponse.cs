using RandomTaskTrack.Data.Dtos.Plants;
using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Plants;

public class ResearchPlantResponse : BaseResponse
{
    public PlantDto Plant { get; set; } = new();
}
