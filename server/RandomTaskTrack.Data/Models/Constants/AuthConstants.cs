namespace RandomTaskTrack.Data.Models.Constants;

public static class AuthConstants
{
    public const string BearerPrefix = "Bearer";
    public const string BearerAuthorizationHeaderName = "Authorization";

    public const string UserRequiredPolicyName = "UserRequired";
    public const string AdminRequiredPolicyName = "AdminRequired";

    public const int PasswordWorkFactor = 12;

    /// <summary>30 days. The tablet is a permanently signed-in kiosk — a short
    /// token would mean re-authenticating on a device with no keyboard.</summary>
    public const int DefaultExpiryInMinutes = 43200;
}
