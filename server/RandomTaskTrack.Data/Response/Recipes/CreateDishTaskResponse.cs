using RandomTaskTrack.Data.Dtos.Tasks;
using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Recipes;

public class CreateDishTaskResponse : BaseResponse
{
    public TaskListItemDto Task { get; set; } = new();
}
