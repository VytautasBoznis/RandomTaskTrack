using RandomTaskTrack.Data.Models.Enums;

namespace RandomTaskTrack.Data.Dtos.Tasks;

public class TaskListItemDto
{
    public Guid Id { get; set; }
    public int DomainId { get; set; }
    public string DomainCode { get; set; } = "";
    public string DomainName { get; set; } = "";
    public Guid? RecurrenceId { get; set; }
    public string Title { get; set; } = "";
    public string? Notes { get; set; }
    public string Data { get; set; } = "{}";
    public DateOnly DueOn { get; set; }
    public TimeOnly? DueTime { get; set; }
    public TaskItemStatus Status { get; set; }
    public DateTime? CompletedAt { get; set; }
}
