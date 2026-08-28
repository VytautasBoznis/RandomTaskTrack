namespace RandomTaskTrack.Data.Models.Recipes;

/// <summary>
/// One dish as it comes back from an IRecipeSource, before it is stored. Kept
/// separate from Recipe so the source never has to know about ids, families or
/// the jsonb encoding.
/// </summary>
public class SourceRecipe
{
    /// <summary>
    /// Which backend produced it. Set by the source itself rather than stamped
    /// on by the caller, because with the rotation and search on different
    /// backends the caller no longer knows.
    /// </summary>
    public string Source { get; set; } = "";

    public string ExternalId { get; set; } = "";
    public string Title { get; set; } = "";
    public string? ImageUrl { get; set; }
    public string? SourceUrl { get; set; }
    public int? ReadyMinutes { get; set; }
    public int? Servings { get; set; }
    public List<RecipeIngredient> Ingredients { get; set; } = new();
    public List<string> Steps { get; set; } = new();
}
