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
    /// Two calls, deliberately. complexSearch is a search index rather than a
    /// recipe store: it hands back dishes whose analyzedInstructions is an empty
    /// array even with addRecipeInformation set — Spoonacular's own worked
    /// example does exactly that — and this source drops anything with no
    /// method, so a one-call search silently returned nothing at all.
    ///
    /// So complexSearch is used for ids only, and the dishes come from
    /// informationBulk, which returns the same shape /recipes/random does and so
    /// goes through the identical readers below.
    ///
    /// instructionsRequired is deliberately *not* set. It looks like the right
    /// filter and is actively harmful: searching "chicken ramen" with it returns
    /// nothing, without it returns recipe 637908, and that recipe has four
    /// perfectly good analyzed steps. Casting the wide net and judging the real
    /// payload is the only reliable order.
    /// </summary>
    public async Task<List<SourceRecipe>> SearchAsync(string query, int number, CancellationToken cancellationToken)
    {
        string searchUrl = $"{_options.BaseUrl.TrimEnd('/')}/recipes/complexSearch" +
                           $"?query={Uri.EscapeDataString(query)}" +
                           $"&number={number}";

        List<string> ids = await GetIdsAsync(searchUrl, $"query {query}", cancellationToken);

        if (ids.Count == 0)
        {
            _logger.LogInformation("Spoonacular matched nothing for {Query}", query);

            return new List<SourceRecipe>();
        }

        string bulkUrl = $"{_options.BaseUrl.TrimEnd('/')}/recipes/informationBulk" +
                         $"?ids={Uri.EscapeDataString(string.Join(',', ids))}";

        // The bulk response is a bare array, so there is no property to look in.
        List<SourceRecipe> candidates = await GetCandidatesAsync(bulkUrl, null, $"query {query}", cancellationToken);

        // Worth a line each time: "matched 10, 10 usable" and "matched 10, 0
        // usable" are very different problems, and the second one is what sent
        // this search back to the drawing board once already.
        _logger.LogInformation(
            "Spoonacular matched {Matched} for {Query}; {Usable} had a method",
            ids.Count, query, candidates.Count);

        return candidates;
    }

    private async Task<List<string>> GetIdsAsync(string url, string what, CancellationToken cancellationToken)
    {
        using JsonDocument document = await GetJsonAsync(url, what, cancellationToken);

        var ids = new List<string>();

        if (!document.RootElement.TryGetProperty("results", out JsonElement results) || results.ValueKind != JsonValueKind.Array)
        {
            return ids;
        }

        foreach (JsonElement result in results.EnumerateArray())
        {
            string id = ReadId(result);

            if (id.Length > 0)
            {
                ids.Add(id);
            }
        }

        return ids;
    }

    private async Task<JsonDocument> GetJsonAsync(string url, string what, CancellationToken cancellationToken)
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

        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }

    /// <param name="arrayName">The property holding the array, or null when the
    /// response is itself one.</param>
    private async Task<List<SourceRecipe>> GetCandidatesAsync(string url, string? arrayName, string what, CancellationToken cancellationToken)
    {
        using JsonDocument document = await GetJsonAsync(url, what, cancellationToken);

        var candidates = new List<SourceRecipe>();

        JsonElement array = document.RootElement;

        if (arrayName is not null && !document.RootElement.TryGetProperty(arrayName, out array))
        {
            return candidates;
        }

        if (array.ValueKind != JsonValueKind.Array)
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
