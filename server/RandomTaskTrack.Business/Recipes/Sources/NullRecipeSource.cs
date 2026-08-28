using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Exceptions;
using RandomTaskTrack.Data.Models.Recipes;
using RandomTaskTrack.Interfaces.Recipes;

namespace RandomTaskTrack.Business.Recipes.Sources;

/// <summary>
/// Registered when no recipe API key is configured. Same bargain as
/// NullAiProvider: the app boots and everything else works, only the recipes
/// tab reports a clear reason.
/// </summary>
public class NullRecipeSource : IRecipeSource
{
    public string Name => RecipeSourceNames.Null;

    public Task<List<SourceRecipe>> PullAsync(string cuisine, CancellationToken cancellationToken) => throw NotConfigured();

    public Task<List<SourceRecipe>> SearchAsync(string query, int number, CancellationToken cancellationToken) => throw NotConfigured();

    private static RecipeSourceException NotConfigured() => new(
        "No recipe source is configured.",
        ExceptionCodes.RECIPE_SOURCE_NOT_CONFIGURED,
        "Set Recipes:ApiKey (env: Recipes__ApiKey) to a Spoonacular API key.");
}
