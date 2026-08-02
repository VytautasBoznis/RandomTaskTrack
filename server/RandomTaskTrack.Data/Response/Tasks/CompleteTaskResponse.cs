using RandomTaskTrack.Data.Dtos.Tasks;
using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Tasks;

public class CompleteTaskResponse : BaseResponse
{
    public TaskListItemDto Task { get; set; } = new();
    public Guid CompletionId { get; set; }

    /// <summary>Set when completing spawned the next occurrence of a
    /// from-completion recurrence.</summary>
    public Guid? NextTaskId { get; set; }
    public DateOnly? NextDueOn { get; set; }
}
