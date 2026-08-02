namespace RandomTaskTrack.Data.Dtos.Tasks;

public class DomainStreakDto
{
    public int DomainId { get; set; }
    public string DomainCode { get; set; } = "";
    public string DomainName { get; set; } = "";
    public int CompletedLast7Days { get; set; }
    public int SkippedLast7Days { get; set; }
    public int PendingOverdue { get; set; }
    public DateTime? LastCompletedAt { get; set; }
}
