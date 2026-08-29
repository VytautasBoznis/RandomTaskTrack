namespace RandomTaskTrack.Data.Models.Ai;

/// <summary>
/// An image attached to a user turn. Base64 rather than a URL because the only
/// images this app sends are ones it holds itself, in a database the provider
/// cannot reach.
/// </summary>
public class AiImage
{
    /// <summary>Raw base64 — no data: prefix, no newlines.</summary>
    public string Base64 { get; set; } = "";

    /// <summary>image/jpeg, image/png, image/gif or image/webp.</summary>
    public string MediaType { get; set; } = "";
}
