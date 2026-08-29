using RandomTaskTrack.Data.Models.Enums;

namespace RandomTaskTrack.Data.Models.Plants;

public class Plant
{
    public Guid Id { get; set; }

    /// <summary>A plant you have, or a seed packet you have not sown yet.</summary>
    public PlantKind Kind { get; set; } = PlantKind.Plant;

    /// <summary>What the user calls it, not the species. "The big one in the hall".</summary>
    public string Name { get; set; } = "";

    public string? Location { get; set; }
    public string? Species { get; set; }
    public string? LatinName { get; set; }
    public DateOnly? AcquiredOn { get; set; }

    /// <summary>The user's own notes. A lookup never touches these.</summary>
    public string Notes { get; set; } = "";

    /// <summary>The free text the identification was made from.</summary>
    public string Description { get; set; } = "";

    /// <summary>Raw jsonb. Shaped like PlantProfile; '{}' until researched.</summary>
    public string Profile { get; set; } = "{}";

    public DateTime? ResearchedAt { get; set; }

    /// <summary>Which model answered, so an odd profile can be attributed.</summary>
    public string? ResearchModel { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
