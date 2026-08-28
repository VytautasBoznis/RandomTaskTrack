namespace RandomTaskTrack.Data.Models.ConfigurationOptions;

public class RecipeOptions
{
    /// <summary>Selects the IRecipeSource implementation. See RecipeSourceNames.</summary>
    public string Provider { get; set; } = "spoonacular";

    public string ApiKey { get; set; } = "";

    public string BaseUrl { get; set; } = "https://api.spoonacular.com";

    /// <summary>How many dishes to ask for per call. All of them are banked in
    /// the library, so this is how many dishes one unit of quota buys — raising
    /// it means fewer calls, not more repeats. Doubles as the default page size
    /// for targeted search.</summary>
    public int CandidatesPerPull { get; set; } = 10;
}
