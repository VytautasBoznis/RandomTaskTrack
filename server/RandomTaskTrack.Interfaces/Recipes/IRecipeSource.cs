using RandomTaskTrack.Data.Models.Recipes;

namespace RandomTaskTrack.Interfaces.Recipes;

/// <summary>
/// Scoped to "give me one dish from this cuisine that I have not cooked" —
/// the level at which recipe APIs actually agree. Paging, scoring and
/// nutrition stay behind the implementation.
/// </summary>
public interface IRecipeSource
{
    string Name { get; }

    /// <param name="cuisine">The family's code, in the source's own vocabulary.</param>
    /// <param name="excludeExternalIds">Dishes already cooked or offered.</param>
    /// <returns>Null when the source had nothing new for that cuisine.</returns>
    Task<SourceRecipe?> PullAsync(string cuisine, IReadOnlyCollection<string> excludeExternalIds, CancellationToken cancellationToken);
}
