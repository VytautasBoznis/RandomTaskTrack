using RandomTaskTrack.Data.Dtos.Recipes;
using RandomTaskTrack.Data.Models.Recipes;
using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Recipes;

public class GetWeeklyDishResponse : BaseResponse
{
    /// <summary>Null when the week has no dish — either it was cleared, or the
    /// rotation could not find one. Not an error: the tab offers ways to fill
    /// it.</summary>
    public WeeklyDishDto? Dish { get; set; }

    /// <summary>Bundled so the reroll picker does not need a second round trip.</summary>
    public List<RecipeFamily> Families { get; set; } = new();
}
