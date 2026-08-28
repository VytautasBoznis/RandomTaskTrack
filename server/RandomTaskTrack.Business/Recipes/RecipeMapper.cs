using System.Text.Json;
using RandomTaskTrack.Data.Dtos.Recipes;
using RandomTaskTrack.Data.Models.Recipes;

namespace RandomTaskTrack.Business.Recipes;

/// <summary>
/// Ingredients and steps are stored as jsonb because they are a list the UI
/// renders, not something the database ever filters on. This is the one place
/// that knows their encoding.
/// </summary>
internal static class RecipeMapper
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string Serialize<T>(List<T> value) => JsonSerializer.Serialize(value, Options);

    /// <summary>
    /// A pulled dish as a library row. familyId is null for targeted search,
    /// where the cuisine is whatever the query happened to match.
    /// </summary>
    public static Recipe ToRecipe(SourceRecipe pulled, string source, int? familyId) => new()
    {
        Id = Guid.NewGuid(),
        Source = source,
        ExternalId = pulled.ExternalId,
        FamilyId = familyId,
        Title = pulled.Title,
        ImageUrl = pulled.ImageUrl,
        SourceUrl = pulled.SourceUrl,
        ReadyMinutes = pulled.ReadyMinutes,
        Servings = pulled.Servings,
        Ingredients = Serialize(pulled.Ingredients),
        Steps = Serialize(pulled.Steps)
    };

    public static WeeklyDishDto ToDish(RecipePick pick, Recipe recipe, string? familyName) => new()
    {
        PickId = pick.Id,
        WeekOf = pick.WeekOf,
        RecipeId = recipe.Id,
        Title = recipe.Title,
        FamilyName = familyName,
        ImageUrl = recipe.ImageUrl,
        SourceUrl = recipe.SourceUrl,
        ReadyMinutes = recipe.ReadyMinutes,
        Servings = recipe.Servings,
        Ingredients = Deserialize<RecipeIngredient>(recipe.Ingredients),
        Steps = Deserialize<string>(recipe.Steps),
        Rating = recipe.Rating,
        Notes = recipe.Notes,
        Tags = recipe.Tags,
        TaskId = pick.TaskId
    };

    /// <summary>Monday of the week `date` falls in, ISO style.</summary>
    public static DateOnly MondayOf(DateOnly date) => date.AddDays(-(((int)date.DayOfWeek + 6) % 7));

    /// <summary>
    /// Trimmed, lowercased, de-duplicated, blanks dropped. The UI sends one
    /// comma-separated string; normalising here means the tag filter can rely on
    /// exact matches whoever calls the API.
    /// </summary>
    public static string[] NormaliseTags(string[]? tags) => tags is null
        ? Array.Empty<string>()
        : tags.Select(tag => tag.Trim().ToLowerInvariant())
              .Where(tag => tag.Length > 0)
              .Distinct()
              .ToArray();

    private static List<T> Deserialize<T>(string json) =>
        string.IsNullOrWhiteSpace(json) ? new List<T>() : JsonSerializer.Deserialize<List<T>>(json, Options) ?? new List<T>();
}
