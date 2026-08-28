using Dapper;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Exceptions;
using RandomTaskTrack.Data.Models.Recipes;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Recipes;

namespace RandomTaskTrack.Business.Recipes.Sources;

/// <summary>
/// Search over the locally imported corpus. No key, no quota, no network — and
/// where it earns its place, no gaps: measured against the live Spoonacular API,
/// ramen is 5 there and 1,061 here, pad thai 0 and 490, bibimbap 0 and 93.
///
/// It cannot do the weekly rotation. The corpus has no cuisine labels, no
/// images, no cook times and no servings, which is exactly what the rotation and
/// the dish card are built from — hence the split in HybridRecipeSource.
/// </summary>
public class CatalogRecipeSource : IRecipeSource
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public string Name => RecipeSourceNames.Catalog;

    public CatalogRecipeSource(IUnitOfWorkFactory unitOfWorkFactory)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    public Task<List<SourceRecipe>> PullAsync(string cuisine, CancellationToken cancellationToken) =>
        throw new RecipeSourceException(
            "The local catalog cannot pick by cuisine.",
            ExceptionCodes.RECIPE_SOURCE_NOT_CONFIGURED,
            "It has no cuisine labels. The weekly rotation needs an API source — set Recipes:ApiKey.");

    /// <summary>
    /// Whether anything has been imported. The difference between "nobody has
    /// pressed Load" and "loaded, and this dish genuinely is not in it" — which
    /// look identical from an empty result and want opposite handling.
    /// </summary>
    public async Task<bool> HasAnyAsync(CancellationToken cancellationToken)
    {
        await using IUnitOfWork unitOfWork = await _unitOfWorkFactory.CreateAsync();

        return await unitOfWork.Connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS (SELECT 1 FROM tracker.recipe_catalog)");
    }

    public async Task<List<SourceRecipe>> SearchAsync(string query, int number, CancellationToken cancellationToken)
    {
        // Its own connection rather than the caller's: search is read-only and
        // has no business joining whatever transaction the request is running.
        await using IUnitOfWork unitOfWork = await _unitOfWorkFactory.CreateAsync();

        var rows = await unitOfWork.Connection.QueryAsync<CatalogRow>(
            // plainto_tsquery ANDs the words, so "chicken ramen" means both.
            //
            // Three tiebreakers, in the order they matter when choosing dinner.
            //
            // Pictured first: both corpora are in here and only the small one
            // has photos, so otherwise the 2.2M text-only entries bury the 32k
            // you can actually look at.
            //
            // Then readable: the big corpus was scraped line by line, so some of
            // its methods come out shredded — ["Cook chicken, debone and cut
            // into small pieces (reserve 1/2 cup", "liquid).", "Cook",
            // "noodles; drain"]. Mean step length separates those from prose
            // without needing to parse anything, and 30 characters is comfortably
            // below a real instruction and above a fragment.
            //
            // Then shortest title, a cheap relevance stand-in: "Chicken Ramen"
            // beats "Chicken Ramen Salad With Almonds" and costs nothing next to
            // ts_rank over two million rows.
            @"SELECT external_id, title, ingredients::text AS ingredients,
                     steps::text AS steps, link, image_url, ready_minutes, servings
              FROM tracker.recipe_catalog
              WHERE to_tsvector('english', title) @@ plainto_tsquery('english', @query)
              ORDER BY (image_url IS NULL),
                       (length(steps::text) / greatest(jsonb_array_length(steps), 1)) < 30,
                       length(title)
              LIMIT @number",
            new { query, number });

        return rows.Select(row => new SourceRecipe
        {
            Source = Name,
            ExternalId = row.ExternalId,
            Title = row.Title,
            ImageUrl = row.ImageUrl,
            SourceUrl = Absolute(row.Link),
            ReadyMinutes = row.ReadyMinutes,
            Servings = row.Servings,
            Ingredients = RecipeMapper.Deserialize<RecipeIngredient>(row.Ingredients),
            Steps = RecipeMapper.Deserialize<string>(row.Steps)
        }).ToList();
    }

    /// <summary>The corpus stores bare hosts ("www.cookbooks.com/…"), which an
    /// href would read as a relative path.</summary>
    private static string? Absolute(string? link) => string.IsNullOrWhiteSpace(link)
        ? null
        : link.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? link : $"https://{link}";

    private sealed class CatalogRow
    {
        public string ExternalId { get; set; } = "";
        public string Title { get; set; } = "";
        public string Ingredients { get; set; } = "[]";
        public string Steps { get; set; } = "[]";
        public string? Link { get; set; }
        public string? ImageUrl { get; set; }
        public int? ReadyMinutes { get; set; }
        public int? Servings { get; set; }
    }
}
