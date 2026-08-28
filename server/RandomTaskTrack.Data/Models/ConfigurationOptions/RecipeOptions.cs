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

    /// <summary>
    /// The bulk catalog, streamed on demand from the Recipes tab. RecipeNLG:
    /// 2.2M recipes, one unauthenticated URL, which is the only kind that can be
    /// pulled without a human pasting a Kaggle token or accepting a licence form.
    /// </summary>
    public string CatalogUrl { get; set; } =
        "https://huggingface.co/datasets/SandhyaKilari/RecipeNLG_dataset/resolve/main/RecipeNLG_dataset.csv";

    /// <summary>Rows in the source file. Only used to show progress and "how
    /// much am I about to pull" before the first import.</summary>
    public long CatalogSourceRows { get; set; } = 2_231_142;

    /// <summary>Stop after this many usable recipes. 0 takes the lot; a smaller
    /// number is handy on a home server that does not want the full 2GB.</summary>
    public int CatalogMaxRows { get; set; }
}
