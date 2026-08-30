using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Learning;

public class CreateCredentialRequest : AuthenticatedRequest
{
    public string Name { get; set; } = "";
    public string? Issuer { get; set; }
    public string? Code { get; set; }

    public DateOnly EarnedOn { get; set; }

    /// <summary>
    /// Defaults to Unknown, which is what puts the card in the "look this up"
    /// group. Set it by hand when you already know — you do not need a web
    /// search to tell you an old MCSD is yours for good.
    /// </summary>
    public CredentialRenewalKind RenewalKind { get; set; } = CredentialRenewalKind.Unknown;

    /// <summary>Required when <see cref="RenewalKind"/> is Expires, rejected otherwise.</summary>
    public DateOnly? ExpiresOn { get; set; }

    public Guid? GoalId { get; set; }
    public string? CredentialId { get; set; }
    public string? Url { get; set; }
    public string? Notes { get; set; }
}
