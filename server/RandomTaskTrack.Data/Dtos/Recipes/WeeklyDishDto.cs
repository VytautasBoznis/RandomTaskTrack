using RandomTaskTrack.Data.Models.Recipes;

namespace RandomTaskTrack.Data.Dtos.Recipes;

/// <summary>This week's dish, with everything the tab renders in one payload.</summary>
public class WeeklyDishDto
{
    public Guid PickId { get; set; }
    public DateOnly WeekOf { get; set; }
    public Guid RecipeId { get; set; }
    public string Title { get; set; } = "";
    public string? FamilyName { get; set; }
    public string? ImageUrl { get; set; }
    public string? SourceUrl { get; set; }
    public int? ReadyMinutes { get; set; }
    public int? Servings { get; set; }
    public List<RecipeIngredient> Ingredients { get; set; } = new();
    public List<string> Steps { get; set; } = new();

    /// <summary>The library's verdict on this dish, editable from the card.</summary>
    public int? Rating { get; set; }
    public string Notes { get; set; } = "";
    public string[] Tags { get; set; } = Array.Empty<string>();

    /// <summary>Set once the dish has been put on the board, so the tab can say
    /// so instead of offering to add it twice.</summary>
    public Guid? TaskId { get; set; }
}
