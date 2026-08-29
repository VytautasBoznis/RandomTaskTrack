using RandomTaskTrack.Data.Models.Recipes;
using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Recipes;

public class SearchRecipesResponse : BaseResponse
{
    /// <summary>Not saved yet. SourceRecipe is already the right shape, so the
    /// candidates go over the wire as-is rather than through a copy of it.</summary>
    public List<SourceRecipe> Candidates { get; set; } = new();

    /// <summary>Whether a next page exists. Asking the source for one extra and
    /// dropping it answers this without a second COUNT over two million rows.</summary>
    public bool HasMore { get; set; }

    /// <summary>The page size actually used, so the caller can step by it
    /// instead of hard-coding a number the server owns.</summary>
    public int PageSize { get; set; }
}
