using RandomTaskTrack.Data.Models.Enums;

namespace RandomTaskTrack.Data.Validator.Learning;

/// <summary>
/// The one rule the create and update paths must not disagree about: the
/// renewal kind and the expiry date have to say the same thing.
///
/// ck_learn_credentials_renewal enforces this in the database as well, on
/// purpose — but a constraint violation surfaces as a 500 with a Postgres
/// message in it, and this is a mistake a form can make. Rejecting it here is
/// what turns it into a sentence the user can act on.
/// </summary>
public static class CredentialRules
{
    public static bool BeAConsistentExpiry(CredentialRenewalKind kind, DateOnly? expiresOn) => kind switch
    {
        // A date on something that never expires is a contradiction, not a
        // harmless extra: it is what would put a permanent credential back into
        // the renewal list.
        CredentialRenewalKind.Permanent => expiresOn is null,

        CredentialRenewalKind.Expires => expiresOn is not null,

        // Unknown tolerates either. A date typed before anyone checked how the
        // renewal works is still a date worth keeping.
        _ => true
    };

    public const string ExpiryMessage =
        "A credential that expires needs an expiry date, and a permanent one must not have one.";
}
