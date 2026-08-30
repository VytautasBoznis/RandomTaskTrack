using RandomTaskTrack.Data.Dtos.Tasks;
using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Models.Learning;

namespace RandomTaskTrack.Data.Dtos.Learning;

public class LearningCredentialDto
{
    public Guid Id { get; set; }
    public Guid? GoalId { get; set; }

    public string Name { get; set; } = "";
    public string Issuer { get; set; } = "";
    public string? Code { get; set; }

    public DateOnly EarnedOn { get; set; }
    public CredentialRenewalKind RenewalKind { get; set; }
    public DateOnly? ExpiresOn { get; set; }

    public string? CredentialId { get; set; }
    public string? Url { get; set; }

    /// <summary>Null until the renewal rules have been looked up.</summary>
    public CredentialRenewal? Renewal { get; set; }

    public DateTime? ResearchedAt { get; set; }
    public string? ResearchModel { get; set; }
    public string Notes { get; set; } = "";

    /// <summary>
    /// Days until it lapses, negative once it has. Null for a permanent
    /// credential and for one nobody has checked — the two cases that must
    /// never be rendered as a countdown.
    /// </summary>
    public int? DaysUntilExpiry { get; set; }

    /// <summary>
    /// Whether renewing is possible now, from the window the lookup found. The
    /// card goes to its warning state on this rather than on a fixed number of
    /// days, because the windows genuinely differ by issuer.
    /// </summary>
    public bool IsRenewable { get; set; }

    /// <summary>The reminder already on the board, if one was asked for.</summary>
    public TaskListItemDto? Task { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
