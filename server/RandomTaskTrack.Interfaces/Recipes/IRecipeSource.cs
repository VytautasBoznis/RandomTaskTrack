using RandomTaskTrack.Data.Models.Recipes;

namespace RandomTaskTrack.Interfaces.Recipes;

/// <summary>
/// Scoped to "give me dishes" — the level at which recipe APIs actually agree.
/// Paging, scoring and nutrition stay behind the implementation.
///
/// Both methods return every usable candidate rather than one. The caller banks
/// the lot and chooses from its own library, so a single metered call to the
/// source is worth ten dishes instead of one.
/// </summary>
public interface IRecipeSource
{
    string Name { get; }

    /// <param name="cuisine">The family's code, in the source's own vocabulary.</param>
    /// <returns>Empty when the source had nothing for that cuisine.</returns>
    Task<List<SourceRecipe>> PullAsync(string cuisine, CancellationToken cancellationToken);

    /// <summary>Free-text search, for overriding the cuisine rotation outright.</summary>
    /// <param name="offset">How many matches to skip. The catalog answers
    /// "ramen" with a thousand dishes, and one page of ten is not a choice.</param>
    Task<List<SourceRecipe>> SearchAsync(string query, int number, int offset, CancellationToken cancellationToken);
}
