using RandomTaskTrack.Data.Models.Enums;

namespace RandomTaskTrack.Data.Models.Learning;

public class CredentialResearchResult
{
    /// <summary>
    /// Permanent, Expires or Unknown. Already reconciled with
    /// <see cref="CredentialRenewal.ValidityMonths"/> by the researcher, so a
    /// contradictory answer arrives here as Unknown rather than as a wrong date.
    /// </summary>
    public CredentialRenewalKind RenewalKind { get; set; } = CredentialRenewalKind.Unknown;

    public CredentialRenewal Renewal { get; set; } = new();

    public string? Model { get; set; }
}
