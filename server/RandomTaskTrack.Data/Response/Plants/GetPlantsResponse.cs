using RandomTaskTrack.Data.Dtos.Plants;
using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Plants;

public class GetPlantsResponse : BaseResponse
{
    public List<PlantDto> Plants { get; set; } = new();
}
