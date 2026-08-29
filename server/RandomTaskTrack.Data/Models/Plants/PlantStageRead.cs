namespace RandomTaskTrack.Data.Models.Plants;

/// <summary>
/// What the lookup made of a progress photo: where the plant has got to, and
/// one line on how it is doing.
/// </summary>
public class PlantStageRead
{
    /// <summary>Two or three words — "first true leaves", "starting to flower".</summary>
    public string Stage { get; set; } = "";

    /// <summary>One sentence. Anything wrong gets said here.</summary>
    public string Note { get; set; } = "";
}
