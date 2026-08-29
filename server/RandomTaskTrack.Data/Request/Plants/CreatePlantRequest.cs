using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Plants;

public class CreatePlantRequest : AuthenticatedRequest
{
    /// <summary>What the user calls it.</summary>
    public string Name { get; set; } = "";

    /// <summary>Defaults to a plant. Send SeedPacket for something not sown yet.</summary>
    public PlantKind Kind { get; set; } = PlantKind.Plant;

    public string? Location { get; set; }

    /// <summary>
    /// Free text the lookup identifies the plant from — species if they know
    /// it, what it looks like if they do not. Optional once there is a photo:
    /// with neither, the plant is saved unidentified and can be looked up later.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// A photo to identify it from, kept as the plant's first stage. Raw base64,
    /// no data: prefix.
    /// </summary>
    public string? ImageBase64 { get; set; }

    public string? MediaType { get; set; }

    public DateOnly? AcquiredOn { get; set; }
    public string? Notes { get; set; }
}
