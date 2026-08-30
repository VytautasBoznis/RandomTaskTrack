using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Learning;

/// <summary>
/// Also how a renewal is recorded: move <see cref="EarnedOn"/> forward and push
/// <see cref="ExpiresOn"/> out. A renewed credential is the same credential, not
/// a second row — a new row would mean the old expiry keeps its reminder.
/// </summary>
public class UpdateCredentialRequest : AuthenticatedRequest
{
    public Guid Id { get; set; }

    public string Name { get; set; } = "";
    public string? Issuer { get; set; }
    public string? Code { get; set; }

    public DateOnly EarnedOn { get; set; }
    public CredentialRenewalKind RenewalKind { get; set; }
    public DateOnly? ExpiresOn { get; set; }

    public Guid? GoalId { get; set; }
    public string? CredentialId { get; set; }
    public string? Url { get; set; }
    public string? Notes { get; set; }
}
