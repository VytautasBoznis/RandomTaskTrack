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
        TaskId = pick.TaskId
    };

    /// <summary>Monday of the week `date` falls in, ISO style.</summary>
    public static DateOnly MondayOf(DateOnly date) => date.AddDays(-(((int)date.DayOfWeek + 6) % 7));

    private static List<T> Deserialize<T>(string json) =>
        string.IsNullOrWhiteSpace(json) ? new List<T>() : JsonSerializer.Deserialize<List<T>>(json, Options) ?? new List<T>();
}
