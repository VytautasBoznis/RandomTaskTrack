using RandomTaskTrack.Data.Dtos.Plants;
using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Plants;

public class CreatePlantScheduleResponse : BaseResponse
{
    /// <summary>The plant as it now stands — schedule and first tasks included.</summary>
    public PlantDto Plant { get; set; } = new();

    public int MaterializedTaskCount { get; set; }
}
