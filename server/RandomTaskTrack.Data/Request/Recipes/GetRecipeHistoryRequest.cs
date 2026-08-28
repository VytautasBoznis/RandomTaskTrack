using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Recipes;

/// <summary>
/// The library, newest first. Every filter is optional and they combine with
/// AND, so no filters at all is "show me everything".
/// </summary>
public class GetRecipeHistoryRequest : AuthenticatedRequest
{
    /// <summary>Matched against the title and the notes, case-insensitively.</summary>
    public string? Search { get; set; }

    /// <summary>Any overlap counts — a dish tagged "quick" matches ["quick","cheap"].</summary>
    public string[]? Tags { get; set; }

    /// <summary>True cooked only, false saved-but-not-cooked only, null both.</summary>
    public bool? Cooked { get; set; }
}
