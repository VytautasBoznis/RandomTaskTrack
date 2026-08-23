namespace RandomTaskTrack.Data.Models.Recipes;

public class RecipeFamily
{
    public int Id { get; set; }

    /// <summary>The source's own cuisine value, sent to the API verbatim.</summary>
    public string Code { get; set; } = "";

    public string Name { get; set; } = "";
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}
