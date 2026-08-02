using RandomTaskTrack.Data.Dtos.Tasks;
using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Tasks;

public class UpdateTaskResponse : BaseResponse
{
    public TaskListItemDto Task { get; set; } = new();
}
