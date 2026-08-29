using RandomTaskTrack.Data.Models.Plants;
using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Plants;

/// <summary>
/// Puts a seed packet's plan on the board against a real sowing date. The steps
/// come back from the UI rather than being read out of the stored profile, so
/// an edited offset is what gets scheduled.
/// </summary>
public class CreateSowingPlanRequest : AuthenticatedRequest
{
    public Guid PlantId { get; set; }

    /// <summary>Day zero. Every step is an offset from it.</summary>
    public DateOnly SowOn { get; set; }

    public List<PlantSowingStep> Steps { get; set; } = new();
}
