namespace RandomTaskTrack.Data.Dtos.Recipes;

/// <summary>
/// One library row for the history list. Covers both halves of the library —
/// dishes that were the weekly dish (WeekOf set) and dishes saved but not yet
/// cooked (WeekOf null) — because the list shows them together under one filter.
/// </summary>
public class RecipeHistoryItemDto
{
    public Guid RecipeId { get; set; }
    public string Title { get; set; } = "";
    public string? FamilyName { get; set; }
    public string? ImageUrl { get; set; }
    public string? SourceUrl { get; set; }
    public int? ReadyMinutes { get; set; }
    public int? Servings { get; set; }

    /// <summary>The week it was the dish, or null if it has never been picked.</summary>
    public DateOnly? WeekOf { get; set; }

    public int? Rating { get; set; }
    public string Notes { get; set; } = "";
    public string[] Tags { get; set; } = Array.Empty<string>();
}
