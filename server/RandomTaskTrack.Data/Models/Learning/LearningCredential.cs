using RandomTaskTrack.Data.Models.Enums;

namespace RandomTaskTrack.Data.Models.Learning;

/// <summary>
/// A credential already held. The scope's other half: a step is work to do, a
/// credential is an asset to keep — and keeping it is a renewal date, not a
/// checklist.
/// </summary>
public class LearningCredential
{
    public Guid Id { get; set; }

    /// <summary>The path it was earned for, if it was earned for one.</summary>
    public Guid? GoalId { get; set; }

    public string Name { get; set; } = "";
    public string Issuer { get; set; } = "";

    /// <summary>"AZ-305". The thing you actually search for.</summary>
    public string? Code { get; set; }

    public DateOnly EarnedOn { get; set; }

    /// <summary>
    /// Whether there is a clock on it at all. Unknown until someone says
    /// otherwise — by hand, or by the lookup.
    /// </summary>
    public CredentialRenewalKind RenewalKind { get; set; } = CredentialRenewalKind.Unknown;

    /// <summary>Set only when <see cref="RenewalKind"/> is Expires. The database enforces it.</summary>
    public DateOnly? ExpiresOn { get; set; }

    /// <summary>The number on the certificate, for proving it.</summary>
    public string? CredentialId { get; set; }

    public string? Url { get; set; }

    /// <summary>Raw jsonb. Shaped like CredentialRenewal; '{}' until looked up.</summary>
    public string Renewal { get; set; } = "{}";

    public DateTime? ResearchedAt { get; set; }
    public string? ResearchModel { get; set; }

    public string Notes { get; set; } = "";

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
