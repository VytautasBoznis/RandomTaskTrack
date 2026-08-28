using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RandomTaskTrack.Data.Models.ConfigurationOptions;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Exceptions;
using RandomTaskTrack.Data.Models.Recipes;
using RandomTaskTrack.Interfaces.Recipes;

namespace RandomTaskTrack.Business.Recipes.Sources;

/// <summary>
/// Spoonacular's /recipes/random returns whole recipes — ingredients and
/// instructions included — so one call per pull is enough. complexSearch needs
/// addRecipeInformation and fillIngredients to return the same thing, but with
/// them it does, so targeted search is also one call. The free tier is metered
/// per call, which is why neither path ever makes a second one.
/// </summary>
public partial class SpoonacularRecipeSource : IRecipeSource
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly RecipeOptions _options;
    private readonly ILogger<SpoonacularRecipeSource> _logger;

    public string Name => RecipeSourceNames.Spoonacular;

    public SpoonacularRecipeSource(
        IHttpClientFactory httpClientFactory,
        IOptions<RecipeOptions> options,
        ILogger<SpoonacularRecipeSource> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Every usable candidate, not just the first. One call is metered the same
    /// whether the caller keeps one dish or ten, and the caller banks them all.
    /// </summary>
    public async Task<List<SourceRecipe>> PullAsync(string cuisine, CancellationToken cancellationToken)
    {
        string url = $"{_options.BaseUrl.TrimEnd('/')}/recipes/random" +
                     $"?number={_options.CandidatesPerPull}" +
                     $"&include-tags={Uri.EscapeDataString(cuisine)}";

        return await GetCandidatesAsync(url, "recipes", $"cuisine {cuisine}", cancellationToken);
    }

    /// <summary>
    /// complexSearch rather than random. addRecipeInformation and fillIngredients
    /// make it return the same recipe shape random does, so one call is still
    /// enough and every reader below is shared — the array is just named
    /// "results" instead of "recipes". instructionsRequired drops the dishes that
    /// would fail the no-method check anyway.
    /// </summary>
    public async Task<List<SourceRecipe>> SearchAsync(string query, int number, CancellationToken cancellationToken)
    {
        string url = $"{_options.BaseUrl.TrimEnd('/')}/recipes/complexSearch" +
                     $"?query={Uri.EscapeDataString(query)}" +
                     $"&number={number}" +
                     "&addRecipeInformation=true" +
                     "&fillIngredients=true" +
                     "&instructionsRequired=true";

        return await GetCandidatesAsync(url, "results", $"query {query}", cancellationToken);
    }

    private async Task<List<SourceRecipe>> GetCandidatesAsync(string url, string arrayName, string what, CancellationToken cancellationToken)
    {
        HttpClient client = _httpClientFactory.CreateClient(RecipeSourceNames.Spoonacular);

        using HttpResponseMessage response = await client.GetAsync(
            $"{url}&apiKey={Uri.EscapeDataString(_options.ApiKey)}",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            // The key is in the query string, so the URL never goes in a log line.
            _logger.LogError("Spoonacular returned {Status} for {What}", (int)response.StatusCode, what);

            throw new RecipeSourceException(
                "The recipe service could not be reached.",
                ExceptionCodes.RECIPE_SOURCE_FAILED,
                $"HTTP {(int)response.StatusCode}. A 402 means the daily free quota is spent.");
        }

        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using JsonDocument document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var candidates = new List<SourceRecipe>();

        if (!document.RootElement.TryGetProperty(arrayName, out JsonElement array) || array.ValueKind != JsonValueKind.Array)
        {
            return candidates;
        }

        foreach (JsonElement candidate in array.EnumerateArray())
        {
            string externalId = ReadId(candidate);

            if (externalId.Length == 0)
            {
                continue;
            }

            List<string> steps = ReadSteps(candidate);

            // A dish with no method is useless here — the point is to cook it.
            if (steps.Count == 0)
            {
                continue;
            }

            candidates.Add(new SourceRecipe
            {
                ExternalId = externalId,
                Title = ReadString(candidate, "title") ?? "Untitled dish",
                ImageUrl = ReadString(candidate, "image"),
                SourceUrl = ReadString(candidate, "sourceUrl"),
                ReadyMinutes = ReadInt(candidate, "readyInMinutes"),
                Servings = ReadInt(candidate, "servings"),
                Ingredients = ReadIngredients(candidate),
                Steps = steps
            });
        }

        return candidates;
    }

    private static string ReadId(JsonElement recipe)
    {
        if (!recipe.TryGetProperty("id", out JsonElement id))
        {
            return "";
        }

        return id.ValueKind == JsonValueKind.Number ? id.GetInt64().ToString() : id.GetString() ?? "";
    }

    private static string? ReadString(JsonElement element, string name) =>
        element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static int? ReadInt(JsonElement element, string name) =>
        element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.Number
            ? value.GetInt32()
            : null;

    private static List<RecipeIngredient> ReadIngredients(JsonElement recipe)
    {
        var ingredients = new List<RecipeIngredient>();

        if (!recipe.TryGetProperty("extendedIngredients", out JsonElement extended) || extended.ValueKind != JsonValueKind.Array)
        {
            return ingredients;
        }

        foreach (JsonElement ingredient in extended.EnumerateArray())
        {
            // `original` is the line as written in the recipe ("2 tbsp olive
            // oil"), which is what you want on a shopping list. Fall back to
            // the parsed name when it is missing.
            string? original = ReadString(ingredient, "original");
            string? name = ReadString(ingredient, "name");

            if (string.IsNullOrWhiteSpace(original) && string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            ingredients.Add(new RecipeIngredient
            {
                Item = string.IsNullOrWhiteSpace(original) ? name! : original!,
                Amount = BuildAmount(ingredient)
            });
        }

        return ingredients;
    }

    private static string? BuildAmount(JsonElement ingredient)
    {
        if (!ingredient.TryGetProperty("amount", out JsonElement amount) || amount.ValueKind != JsonValueKind.Number)
        {
            return null;
        }

        string? unit = ReadString(ingredient, "unit");

        return string.IsNullOrWhiteSpace(unit)
            ? amount.GetDouble().ToString("0.##")
            : $"{amount.GetDouble():0.##} {unit}";
    }

    private static List<string> ReadSteps(JsonElement recipe)
    {
        var steps = new List<string>();

        if (recipe.TryGetProperty("analyzedInstructions", out JsonElement analyzed) && analyzed.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement group in analyzed.EnumerateArray())
            {
                if (!group.TryGetProperty("steps", out JsonElement groupSteps) || groupSteps.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (JsonElement step in groupSteps.EnumerateArray())
                {
                    string? text = ReadString(step, "step");

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        steps.Add(text!.Trim());
                    }
                }
            }
        }

        if (steps.Count > 0)
        {
            return steps;
        }

        // Not every recipe has been through Spoonacular's parser; those carry
        // the method as one blob of HTML instead.
        string? instructions = ReadString(recipe, "instructions");

        if (string.IsNullOrWhiteSpace(instructions))
        {
            return steps;
        }

        string plain = HtmlTag().Replace(instructions!, "\n");

        steps.AddRange(plain
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => line.Length > 1));

        return steps;
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlTag();
}
