namespace RandomTaskTrack.Data.Dtos.Tasks;

/// <summary>The single payload the tablet dashboard renders. One round trip.</summary>
public class DashboardDto
{
    public DateOnly Today { get; set; }
    public List<TaskListItemDto> Overdue { get; set; } = new();
    public List<TaskListItemDto> DueToday { get; set; } = new();
    public List<TaskListItemDto> Upcoming { get; set; } = new();
    public List<TaskListItemDto> CompletedToday { get; set; } = new();

    /// <summary>
    /// What the learning tab has on the board, grouped by path and kept out of
    /// the buckets above. Studying still has to reach the dashboard to compete
    /// with the watering, but it competes as one line per path rather than as a
    /// task each.
    /// </summary>
    public List<DashboardLearningDto> Learning { get; set; } = new();

    public List<DomainStreakDto> Streaks { get; set; } = new();
}
