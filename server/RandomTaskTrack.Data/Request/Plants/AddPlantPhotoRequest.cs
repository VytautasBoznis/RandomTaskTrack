using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Plants;

/// <summary>
/// Adds a photo, which is the same act as recording a stage. The lookup reads
/// the picture and fills in the stage and the note unless they are given here.
/// </summary>
public class AddPlantPhotoRequest : AuthenticatedRequest
{
    public Guid PlantId { get; set; }

    /// <summary>Raw base64, no data: prefix.</summary>
    public string ImageBase64 { get; set; } = "";

    public string MediaType { get; set; } = "";

    /// <summary>Defaults to today.</summary>
    public DateOnly? TakenOn { get; set; }

    /// <summary>Skip the AI read and label it by hand.</summary>
    public string? Stage { get; set; }

    public string? Note { get; set; }
}
