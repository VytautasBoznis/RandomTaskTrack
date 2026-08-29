using RandomTaskTrack.Data.Models.Ai;
using RandomTaskTrack.Data.Models.Enums;

namespace RandomTaskTrack.Data.Models.Plants;

/// <summary>
/// Everything the lookup gets to work with. A parameter object rather than six
/// arguments, because the photo and the kind changed the shape of the question
/// and the next thing that does should not be a seventh.
/// </summary>
public class PlantResearchQuestion
{
    public string Name { get; set; } = "";
    public string? Location { get; set; }
    public string Description { get; set; } = "";

    /// <summary>A seed packet gets asked a different question than a pot plant.</summary>
    public PlantKind Kind { get; set; } = PlantKind.Plant;

    /// <summary>The plant, or the packet. Null when there is nothing to look at.</summary>
    public AiImage? Image { get; set; }
}
