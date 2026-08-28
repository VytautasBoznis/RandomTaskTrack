using RandomTaskTrack.Data.Dtos.Recipes;
using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Recipes;

public class GetRecipeHistoryResponse : BaseResponse
{
    public List<RecipeHistoryItemDto> Entries { get; set; } = new();
}
