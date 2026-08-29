namespace RandomTaskTrack.Data.Dtos.Plants;

/// <summary>
/// A photo without its bytes. The tab lists a plant's whole history, and
/// shipping a few hundred KB per entry inside the JSON would make the one call
/// the tab is built around the slowest thing in the app — the UI fetches each
/// image separately from /api/plants/photos/{id}.
/// </summary>
public class PlantPhotoDto
{
    public Guid Id { get; set; }

    /// <summary>Carried so one query can serve every plant on the tab.</summary>
    public Guid PlantId { get; set; }
    public string MediaType { get; set; } = "";
    public string Stage { get; set; } = "";
    public string Note { get; set; } = "";
    public DateOnly TakenOn { get; set; }
    public DateTime CreatedAt { get; set; }
}
