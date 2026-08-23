namespace RandomTaskTrack.Data.Models.Recipes;

/// <summary>One line of the shopping checklist.</summary>
public class RecipeIngredient
{
    public string Item { get; set; } = "";
    public string? Amount { get; set; }
}
