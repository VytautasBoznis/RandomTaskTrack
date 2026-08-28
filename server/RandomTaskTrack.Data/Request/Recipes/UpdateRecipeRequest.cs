using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Recipes;

/// <summary>
/// The verdict on a dish. Every field is null-means-leave-alone, matching
/// UpdateNoteRequest — except that clearing a rating is a real thing to want, so
/// <see cref="ClearRating"/> says so explicitly.
/// </summary>
public class UpdateRecipeRequest : AuthenticatedRequest
{
    public Guid RecipeId { get; set; }

    /// <summary>1-5.</summary>
    public int? Rating { get; set; }

    /// <summary>Sets the rating back to unrated, whatever Rating says.</summary>
    public bool ClearRating { get; set; }

    public string? Notes { get; set; }

    /// <summary>The whole tag set, not an addition. Normalised server-side.</summary>
    public string[]? Tags { get; set; }
}
