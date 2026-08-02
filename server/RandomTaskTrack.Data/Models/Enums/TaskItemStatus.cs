namespace RandomTaskTrack.Data.Models.Enums;

/// <summary>Named TaskItemStatus, not TaskStatus, to avoid colliding with System.Threading.Tasks.TaskStatus.</summary>
public enum TaskItemStatus : int
{
    Pending = 1,
    Done = 2,
    Skipped = 3
}
