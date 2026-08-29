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
/// The fallback is only for an *empty* catalog — someone who has never pressed
/// Load. A loaded catalog that simply has no match returns nothing, on purpose:
/// falling back on every miss would spend a metered API call each time, and
/// would turn "no such dish" into a quota error the moment the daily allowance
/// ran out.
/// </summary>
public class HybridRecipeSource : IRecipeSource
{
    private readonly IRecipeSource _rotation;
    private readonly CatalogRecipeSource _catalog;
    private readonly ILogger<HybridRecipeSource> _logger;

    public string Name => RecipeSourceNames.Hybrid;

    public HybridRecipeSource(IRecipeSource rotation, CatalogRecipeSource catalog, ILogger<HybridRecipeSource> logger)
    {
        _rotation = rotation;
        _catalog = catalog;
        _logger = logger;
    }

    public Task<List<SourceRecipe>> PullAsync(string cuisine, CancellationToken cancellationToken) =>
        _rotation.PullAsync(cuisine, cancellationToken);

    public async Task<List<SourceRecipe>> SearchAsync(string query, int number, int offset, CancellationToken cancellationToken)
    {
        List<SourceRecipe> local = await _catalog.SearchAsync(query, number, offset, cancellationToken);

        if (local.Count > 0 || await _catalog.HasAnyAsync(cancellationToken))
        {
            return local;
        }

        _logger.LogInformation("Catalog is empty; searching {Source} for {Query}", _rotation.Name, query);

        return await _rotation.SearchAsync(query, number, offset, cancellationToken);
    }
}
