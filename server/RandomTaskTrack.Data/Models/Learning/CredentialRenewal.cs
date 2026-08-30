namespace RandomTaskTrack.Data.Models.Learning;

/// <summary>
/// What the renewal lookup found. Stored whole as learn_credentials.renewal.
///
/// The one field that is not prose is <see cref="ValidityMonths"/>, which is
/// what the expiry date is computed from — and it is nullable because
/// "permanent" and "could not find out" are both real answers that must not be
/// turned into a date.
/// </summary>
public class CredentialRenewal
{
    /// <summary>
    /// How long it lasts from the day it was earned. Null for a permanent
    /// credential, and null when the lookup could not establish it.
    /// </summary>
    public int? ValidityMonths { get; set; }

    /// <summary>How renewing actually works: an assessment, CPE credits, resitting.</summary>
    public string Renewal { get; set; } = "";

    /// <summary>How many days before expiry the renewal window opens. 0 when there is none.</summary>
    public int WindowOpensDays { get; set; }

    /// <summary>"Free", "€165". Text, like every other cost in this scope.</summary>
    public string Cost { get; set; } = "";

    /// <summary>What happens if it lapses — resit the whole exam, or a grace period.</summary>
    public string IfLapsed { get; set; } = "";

    /// <summary>Where the issuer states this, so it can be checked rather than trusted.</summary>
    public string OfficialUrl { get; set; } = "";

    /// <summary>
    /// The lookup's own summary, including the case that matters most: that a
    /// programme is retired and the credential stays on the transcript for good.
    /// </summary>
    public string Notes { get; set; } = "";
}
