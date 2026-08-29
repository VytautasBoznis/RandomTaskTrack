using RandomTaskTrack.Data.Dtos.Plants;
using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Plants;

public class CreateSowingPlanResponse : BaseResponse
{
    public PlantDto Plant { get; set; } = new();

    /// <summary>How many dated tasks the plan put on the board.</summary>
    public int CreatedTaskCount { get; set; }
}
