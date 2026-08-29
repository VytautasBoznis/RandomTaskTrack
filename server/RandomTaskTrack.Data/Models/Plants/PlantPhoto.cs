namespace RandomTaskTrack.Data.Models.Plants;

/// <summary>
/// One photo, which is also one stage — taking a picture is how a change gets
/// recorded, so the two are the same act.
/// </summary>
public class PlantPhoto
{
    public Guid Id { get; set; }
    public Guid PlantId { get; set; }

    /// <summary>The bytes. Loaded only when one photo is being served — the
    /// list queries deliberately leave this column alone.</summary>
    public byte[] Image { get; set; } = Array.Empty<byte>();

    public string MediaType { get; set; } = "";

    /// <summary>"sown", "first true leaves", "flowering". Suggested by the
    /// lookup from the photo itself, editable like anything else.</summary>
    public string Stage { get; set; } = "";

    public string Note { get; set; } = "";
    public DateOnly TakenOn { get; set; }
    public DateTime CreatedAt { get; set; }
}
