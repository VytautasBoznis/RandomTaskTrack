using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Recipes;

/// <summary>
/// Overrides the cuisine rotation: "never mind what is next, find me ramen".
/// Reads only — nothing is stored until the results come back to
/// <see cref="SaveRecipesRequest"/>.
/// </summary>
public class SearchRecipesRequest : AuthenticatedRequest
{
    public string Query { get; set; } = "";

    /// <summary>How many to show. Null takes the source's default page.</summary>
    public int? Number { get; set; }
}
