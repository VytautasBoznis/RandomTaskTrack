namespace RandomTaskTrack.Data.Models.Constants;

/// <summary>
/// What the AI provider accepts as an image, which is the only thing photos in
/// this app are for. Anything outside this list is rejected on the way in
/// rather than at the point of asking about it.
/// </summary>
public static class ImageMediaTypes
{
    public const string Jpeg = "image/jpeg";
    public const string Png = "image/png";
    public const string Gif = "image/gif";
    public const string Webp = "image/webp";

    public static readonly string[] All = [Jpeg, Png, Gif, Webp];

    /// <summary>
    /// Base64 characters, not bytes: 5 MB of image is the provider's per-image
    /// ceiling and base64 inflates by a third. The UI downscales to a couple of
    /// hundred KB, so this only ever catches something that skipped the UI.
    /// </summary>
    public const int MaxBase64Length = 7_000_000;
}
