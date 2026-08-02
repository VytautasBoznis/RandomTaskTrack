namespace RandomTaskTrack.Data.Dtos.Tasks;

/// <summary>The single payload the tablet dashboard renders. One round trip.</summary>
public class DashboardDto
{
    public DateOnly Today { get; set; }
    public List<TaskListItemDto> Overdue { get; set; } = new();
    public List<TaskListItemDto> DueToday { get; set; } = new();
    public List<TaskListItemDto> Upcoming { get; set; } = new();
    public List<TaskListItemDto> CompletedToday { get; set; } = new();
    public List<DomainStreakDto> Streaks { get; set; } = new();
}
