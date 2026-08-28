using RandomTaskTrack.Data.Models.Recipes;
using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Recipes;

public class SearchRecipesResponse : BaseResponse
{
    /// <summary>Not saved yet. SourceRecipe is already the right shape, so the
    /// candidates go over the wire as-is rather than through a copy of it.</summary>
    public List<SourceRecipe> Candidates { get; set; } = new();
}
