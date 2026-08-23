namespace RandomTaskTrack.Data.Models.ConfigurationOptions;

public class RecipeOptions
{
    /// <summary>Selects the IRecipeSource implementation. See RecipeSourceNames.</summary>
    public string Provider { get; set; } = "spoonacular";

    public string ApiKey { get; set; } = "";

    public string BaseUrl { get; set; } = "https://api.spoonacular.com";

    /// <summary>How many random dishes to ask for per pull. Only the first one
    /// that has not been cooked before is kept, so this is the tolerance for
    /// repeats, not a page size.</summary>
    public int CandidatesPerPull { get; set; } = 10;
}
