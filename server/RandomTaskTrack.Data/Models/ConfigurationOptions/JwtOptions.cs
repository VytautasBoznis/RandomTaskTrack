namespace RandomTaskTrack.Data.Models.ConfigurationOptions;

public class JwtOptions
{
    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";
    public string SecretKey { get; set; } = "";

    /// <summary>Defaults to 30 days (43200) when unset — see AuthConstants.</summary>
    public int ExpiryInMinutes { get; set; }
}
