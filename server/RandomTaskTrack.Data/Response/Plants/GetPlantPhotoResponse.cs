using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Plants;

/// <summary>
/// The one response in the app that is not JSON — the controller turns it into
/// a file result, because an img tag cannot read a base64 field out of an
/// envelope.
/// </summary>
public class GetPlantPhotoResponse : BaseResponse
{
    public byte[] Image { get; set; } = Array.Empty<byte>();
    public string MediaType { get; set; } = "";
}
