namespace RandomTaskTrack.Data.Models.Constants;

public static class RecipeSourceNames
{
    public const string Spoonacular = "spoonacular";

    /// <summary>The bulk corpus in tracker.recipe_catalog.</summary>
    public const string Catalog = "catalog";

    /// <summary>Spoonacular for the weekly rotation, the catalog for search.</summary>
    public const string Hybrid = "hybrid";

    public const string Null = "null";
}
