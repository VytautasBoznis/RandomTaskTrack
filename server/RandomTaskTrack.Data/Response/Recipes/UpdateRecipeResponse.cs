using RandomTaskTrack.Data.Dtos.Recipes;
using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Recipes;

public class UpdateRecipeResponse : BaseResponse
{
    public RecipeHistoryItemDto Recipe { get; set; } = new();
}
