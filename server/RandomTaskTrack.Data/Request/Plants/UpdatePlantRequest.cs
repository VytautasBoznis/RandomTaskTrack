using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Plants;

/// <summary>Every field is optional; null means "leave alone".</summary>
public class UpdatePlantRequest : AuthenticatedRequest
{
    public Guid Id { get; set; }

    /// <summary>Send Plant to promote a packet that has come up. Its photos,
    /// tasks and profile all stay where they are.</summary>
    public PlantKind? Kind { get; set; }

    public string? Name { get; set; }
    public string? Location { get; set; }

    /// <summary>Correcting the species by hand outranks the lookup — a
    /// re-research keeps whatever is typed here.</summary>
    public string? Species { get; set; }

    public string? LatinName { get; set; }
    public DateOnly? AcquiredOn { get; set; }
    public string? Notes { get; set; }
}
