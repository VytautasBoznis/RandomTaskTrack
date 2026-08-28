using RandomTaskTrack.Data.Dtos.Recipes;
using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Recipes;

public class SaveRecipesResponse : BaseResponse
{
    /// <summary>The saved dishes as library rows, so the tab can offer "cook this
    /// one" straight away without a reload. Includes any that were already there.</summary>
    public List<RecipeHistoryItemDto> Recipes { get; set; } = new();
}
