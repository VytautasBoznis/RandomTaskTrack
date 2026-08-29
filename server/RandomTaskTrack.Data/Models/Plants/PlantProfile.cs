namespace RandomTaskTrack.Data.Models.Plants;

/// <summary>
/// What the lookup came back with. Stored whole as plant_plants.profile and
/// handed to the UI as-is — every field is prose meant to be read, not a value
/// anything computes with. The one exception is <see cref="CareTasks"/>, which
/// is the bridge into the task engine.
/// </summary>
public class PlantProfile
{
    public string? SpeciesCommon { get; set; }
    public string? SpeciesLatin { get; set; }

    /// <summary>
    /// "high", "medium" or "low" — how sure the identification is. The input is
    /// a sentence typed by someone who does not know what the plant is, so an
    /// answer that hides its own uncertainty is worse than no answer. The card
    /// says so out loud below "high".
    /// </summary>
    public string Confidence { get; set; } = "";

    /// <summary>Why it thinks that, when it is not sure.</summary>
    public string Reasoning { get; set; } = "";

    /// <summary>Two or three sentences on what the plant is.</summary>
    public string Summary { get; set; } = "";

    public string Light { get; set; } = "";
    public string Water { get; set; } = "";
    public string Humidity { get; set; } = "";
    public string Temperature { get; set; } = "";
    public string Soil { get; set; } = "";
    public string Feeding { get; set; } = "";
    public string Repotting { get; set; } = "";

    /// <summary>Cats, dogs, children. Empty when it is harmless.</summary>
    public string Toxicity { get; set; } = "";

    /// <summary>"Yellow lower leaves usually means overwatering" — one line each.</summary>
    public List<string> CommonProblems { get; set; } = new();

    /// <summary>Filled in for a seed packet, null for a plant already growing.</summary>
    public PlantSowing? Sowing { get; set; }

    /// <summary>
    /// The suggested schedule. Suggested, not applied: how often a plant
    /// actually needs water depends on the room it is in, so these become
    /// recurrences only when the user picks them.
    /// </summary>
    public List<PlantCareTask> CareTasks { get; set; } = new();
}
