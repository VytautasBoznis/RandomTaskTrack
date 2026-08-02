using RandomTaskTrack.Data.Dtos.Tasks;
using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Tasks;

public class GetCompletionLogResponse : BaseResponse
{
    public List<CompletionLogItemDto> Entries { get; set; } = new();
}
