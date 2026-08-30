namespace RandomTaskTrack.Data.Models.Enums;

/// <summary>
/// Whether a held credential has a clock on it. Matches
/// ck_learn_credentials_renewal, which also enforces that
/// <see cref="Expires"/> carries a date and <see cref="Permanent"/> does not.
///
/// Three states rather than a nullable expiry date, because "never expires" and
/// "nobody has looked it up yet" are different facts that need different
/// treatment: one is finished, the other is a job. An older MCSD is permanent
/// and should never appear in a renewal list again.
/// </summary>
public enum CredentialRenewalKind
{
    Permanent = 1,
    Expires = 2,
    Unknown = 3
}
