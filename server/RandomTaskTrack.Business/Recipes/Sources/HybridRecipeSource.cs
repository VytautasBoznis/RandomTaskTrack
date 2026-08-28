using Microsoft.Extensions.Logging;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Recipes;
using RandomTaskTrack.Interfaces.Recipes;

namespace RandomTaskTrack.Business.Recipes.Sources;

/// <summary>
/// Sends each half of the interface to whichever backend is actually good at it.
///
/// The weekly rotation goes to the API source: it is the only one with cuisine
/// labels, images, cook times and servings, which is what the rotation picks by
/// and what the dish card is made of.
///
/// Targeted search goes to the local catalog, where breadth is the entire point
/// and you are choosing by hand anyway — searching "chicken ramen" returns 1
/// dish from Spoonacular and 43 from the catalog.
///
/// If the catalog has not been imported it has nothing to say, so search falls
/// back to the API rather than returning an empty list to someone who has never
/// pressed the button.
/// </summary>
public class HybridRecipeSource : IRecipeSource
{
    private readonly IRecipeSource _rotation;
    private readonly IRecipeSource _catalog;
    private readonly ILogger<HybridRecipeSource> _logger;

    public string Name => RecipeSourceNames.Hybrid;

    public HybridRecipeSource(IRecipeSource rotation, IRecipeSource catalog, ILogger<HybridRecipeSource> logger)
    {
        _rotation = rotation;
        _catalog = catalog;
        _logger = logger;
    }

    public Task<List<SourceRecipe>> PullAsync(string cuisine, CancellationToken cancellationToken) =>
        _rotation.PullAsync(cuisine, cancellationToken);

    public async Task<List<SourceRecipe>> SearchAsync(string query, int number, CancellationToken cancellationToken)
    {
        List<SourceRecipe> local = await _catalog.SearchAsync(query, number, cancellationToken);

        if (local.Count > 0)
        {
            return local;
        }

        _logger.LogInformation("Catalog had nothing for {Query}; falling back to {Source}", query, _rotation.Name);

        return await _rotation.SearchAsync(query, number, cancellationToken);
    }
}
