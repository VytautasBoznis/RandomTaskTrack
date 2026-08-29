using RandomTaskTrack.Data.Dtos.Tasks;
using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Models.Plants;

namespace RandomTaskTrack.Data.Dtos.Plants;

/// <summary>
/// A plant with everything the card renders: the researched profile, the care
/// schedule it is on, and the tasks that schedule has already put on the board.
/// The whole tab is one round trip, the same bargain /tasks/dashboard makes.
/// </summary>
public class PlantDto
{
    public Guid Id { get; set; }
    public PlantKind Kind { get; set; }
    public string Name { get; set; } = "";
    public string? Location { get; set; }
    public string? Species { get; set; }
    public string? LatinName { get; set; }
    public DateOnly? AcquiredOn { get; set; }
    public string Notes { get; set; } = "";
    public string Description { get; set; } = "";

    /// <summary>Null until the first successful lookup.</summary>
    public PlantProfile? Profile { get; set; }

    public DateTime? ResearchedAt { get; set; }
    public string? ResearchModel { get; set; }

    /// <summary>Pending tasks carrying this plant's id, soonest first.</summary>
    public List<TaskListItemDto> Tasks { get; set; } = new();

    /// <summary>The recurrences the care schedule is made of, paused ones too.</summary>
    public List<RecurrenceListItemDto> Recurrences { get; set; } = new();

    /// <summary>Every photo, newest first — which is also the stage history.</summary>
    public List<PlantPhotoDto> Photos { get; set; } = new();

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
