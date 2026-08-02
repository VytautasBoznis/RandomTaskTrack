using RandomTaskTrack.Data.Models.Enums;

namespace RandomTaskTrack.Data.Dtos.Tasks;

public class CompletionLogItemDto
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public int DomainId { get; set; }
    public string DomainCode { get; set; } = "";
    public string Title { get; set; } = "";
    public TaskItemStatus Status { get; set; }
    public string PlannedData { get; set; } = "{}";
    public string ActualData { get; set; } = "{}";
    public string? Note { get; set; }
    public DateOnly DueOn { get; set; }
    public DateTime CompletedAt { get; set; }
}
