using RandomTaskTrack.Data.Models.Recipes;
using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Recipes;

/// <summary>
/// The dishes ticked on the search results, sent back whole. The search response
/// already carried them, so keeping them client-side costs one round trip
/// instead of a server-side cache or a second metered call to the source.
/// </summary>
public class SaveRecipesRequest : AuthenticatedRequest
{
    public List<SourceRecipe> Recipes { get; set; } = new();
}
