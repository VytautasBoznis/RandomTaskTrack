using RandomTaskTrack.Data.Dtos.Plants;
using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Plants;

public class CreatePlantResponse : BaseResponse
{
    public PlantDto Plant { get; set; } = new();

    /// <summary>
    /// Set when the plant was saved but the lookup did not answer — no API key,
    /// provider down, a reply that was not JSON. The plant is real either way;
    /// the card offers to look it up again.
    /// </summary>
    public string? ResearchError { get; set; }
}
