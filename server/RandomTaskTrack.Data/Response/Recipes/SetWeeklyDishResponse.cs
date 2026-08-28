using RandomTaskTrack.Data.Dtos.Recipes;
using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Recipes;

public class SetWeeklyDishResponse : BaseResponse
{
    public WeeklyDishDto Dish { get; set; } = new();
}
