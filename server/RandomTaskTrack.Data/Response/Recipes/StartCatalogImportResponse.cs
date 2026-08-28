using RandomTaskTrack.Data.Models.Recipes;
using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Recipes;

public class StartCatalogImportResponse : BaseResponse
{
    /// <summary>False when one was already running, so the tab can say so
    /// instead of implying it started a second.</summary>
    public bool Started { get; set; }

    public CatalogImportStatus Status { get; set; } = new();
}
