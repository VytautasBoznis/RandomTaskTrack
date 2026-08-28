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

    /// <summary>1-5, or null until the dish has been cooked and judged.</summary>
    public int? Rating { get; set; }

    /// <summary>What went wrong or right last time. Plain text, not markdown.</summary>
    public string Notes { get; set; } = "";

    /// <summary>Postgres text[]; Npgsql maps it straight onto string[].</summary>
    public string[] Tags { get; set; } = Array.Empty<string>();

    public DateTime PulledAt { get; set; }
}
