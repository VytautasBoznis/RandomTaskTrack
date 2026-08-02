using RandomTaskTrack.Data.Dtos.Tasks;
using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Tasks;

public class GetTasksResponse : BaseResponse
{
    public List<TaskListItemDto> Tasks { get; set; } = new();
    public int TotalCount { get; set; }
}
