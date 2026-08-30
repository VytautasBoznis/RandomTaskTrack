namespace RandomTaskTrack.Data.Models.Learning;

/// <summary>
/// What the renewal lookup is asked. The earned date matters as much as the
/// name: renewal rules change, and which rules apply is usually decided by when
/// the credential was awarded rather than by what the issuer does today.
/// </summary>
public class CredentialQuestion
{
    public string Name { get; set; } = "";
    public string Issuer { get; set; } = "";
    public string? Code { get; set; }
    public DateOnly EarnedOn { get; set; }
}
