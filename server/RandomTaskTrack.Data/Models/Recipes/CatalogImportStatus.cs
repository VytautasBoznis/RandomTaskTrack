namespace RandomTaskTrack.Data.Models.Recipes;

/// <summary>
/// A snapshot of the bulk import, taken under lock so the tab never sees a
/// half-updated set of counters. Everything except <see cref="Loaded"/> is
/// in-memory and resets with the pod — the row count in the database is the
/// durable answer to "is it there", which is the one that matters.
/// </summary>
public class CatalogImportStatus
{
    /// <summary>Recipes currently in tracker.recipe_catalog.</summary>
    public long Loaded { get; set; }

    /// <summary>Rows in the source file, for progress and for saying what a
    /// first import is about to pull down.</summary>
    public long SourceRows { get; set; }

    public bool IsRunning { get; set; }

    /// <summary>Source rows consumed so far this run.</summary>
    public long RowsRead { get; set; }

    /// <summary>New recipes this run added. Re-running only adds what is
    /// missing, so on a second run this is usually 0.</summary>
    public long RowsAdded { get; set; }

    /// <summary>Set when a run has finished in this process, so the tab can say
    /// "added 1,204" rather than only showing a total.</summary>
    public DateTime? FinishedAt { get; set; }

    public string? Error { get; set; }
}
