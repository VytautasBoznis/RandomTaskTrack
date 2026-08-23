namespace RandomTaskTrack.Data.Models.Recipes;

public class Recipe
{
    public Guid Id { get; set; }
    public string Source { get; set; } = "";
    public string ExternalId { get; set; } = "";
    public int? FamilyId { get; set; }
    public string Title { get; set; } = "";
    public string? ImageUrl { get; set; }
    public string? SourceUrl { get; set; }
    public int? ReadyMinutes { get; set; }
    public int? Servings { get; set; }

    /// <summary>Raw jsonb. Shaped like RecipeIngredient[].</summary>
    public string Ingredients { get; set; } = "[]";

    /// <summary>Raw jsonb. Shaped like string[], in order.</summary>
    public string Steps { get; set; } = "[]";

    public DateTime PulledAt { get; set; }
}
