using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Recipes;

/// <summary>
/// Kicks off the bulk load. Safe to send twice — a run already in flight is
/// reported rather than duplicated.
/// </summary>
public class StartCatalogImportRequest : AuthenticatedRequest
{
}
