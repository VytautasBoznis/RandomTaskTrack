using RandomTaskTrack.Data.Models.Plants;
using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Plants;

/// <summary>
/// Turns chosen lines of the suggested care schedule into recurrences. The UI
/// sends the lines back rather than an index into the profile, so an edited
/// interval is what gets scheduled.
/// </summary>
public class CreatePlantScheduleRequest : AuthenticatedRequest
{
    public Guid PlantId { get; set; }
    public List<PlantCareTask> Tasks { get; set; } = new();
}
