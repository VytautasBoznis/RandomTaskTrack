namespace RandomTaskTrack.Data.Models.Plants;

public class PlantResearchResult
{
    public PlantProfile Profile { get; set; } = new();

    /// <summary>Which model answered. Stored on the plant for attribution.</summary>
    public string Model { get; set; } = "";
}
