using RandomTaskTrack.Data.Models.Recipes;

namespace RandomTaskTrack.Interfaces.Recipes;

/// <summary>
/// The bulk catalog load. A singleton because the run outlives the request that
/// started it — the tab fires and forgets, then polls.
/// </summary>
public interface IRecipeCatalogImporter
{
    /// <summary>Counters plus the live row count from the database.</summary>
    Task<CatalogImportStatus> GetStatusAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Starts a run in the background. False when one is already going, so a
    /// second click is a no-op rather than a second 2GB download.
    /// </summary>
    bool TryStart();
}
