using RandomTaskTrack.Data.Models.Recipes;
using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Recipes;

public class GetCatalogStatusResponse : BaseResponse
{
    public CatalogImportStatus Status { get; set; } = new();
}
