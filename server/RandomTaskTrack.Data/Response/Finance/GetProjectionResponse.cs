using RandomTaskTrack.Data.Dtos.Finance;
using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Finance;

public class GetProjectionResponse : BaseResponse
{
    public List<ProjectionPointDto> Points { get; set; } = new();
}
