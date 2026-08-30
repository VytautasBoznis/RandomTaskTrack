namespace RandomTaskTrack.Data.Dtos.Tasks;

/// <summary>
/// One learning path with something on the board, as a single row rather than
/// one row per task. A path with four dated steps is four titles and four sets
/// of notes in the buckets, which is what stopped the dashboard being readable
/// standing up; here it is one line that says where to go.
/// </summary>
public class DashboardLearningDto
{
    /// <summary>The path, or null when the row is a credential's renewal — those
    /// sit on no path and are named by the credential instead.</summary>
    public Guid? GoalId { get; set; }

    public string Title { get; set; } = "";

    /// <summary>How many of this row's tasks are on the board. The reason the
    /// row can collapse without hiding that there is more than one.</summary>
    public int Count { get; set; }

    /// <summary>The soonest of them. Overdue is left for the reader to see by
    /// comparing against today, the same way every other date on this screen is
    /// read.</summary>
    public DateOnly NextDueOn { get; set; }
}
