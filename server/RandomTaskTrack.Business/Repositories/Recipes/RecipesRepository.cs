using Dapper;
using RandomTaskTrack.Data.Dtos.Recipes;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Models.Recipes;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Recipes;

namespace RandomTaskTrack.Business.Repositories.Recipes;

public class RecipesRepository : IRecipesRepository
{
    private const string SelectFamily = "id, code, name, is_active, sort_order";

    private const string SelectRecipe = @"
        r.id, r.source, r.external_id, r.family_id, r.title, r.image_url, r.source_url,
        r.ready_minutes, r.servings, r.ingredients::text AS ingredients,
        r.steps::text AS steps, r.rating, r.notes, r.tags, r.pulled_at";

    /// <summary>
    /// What makes a library row fair game this week. A dish that was once the
    /// weekly dish (status 1) is spent for good; one rerolled out of this week
    /// is only out until the week turns over; one tagged "not picked" is never
    /// offered at all.
    ///
    /// Only the rotation asks this. The history list deliberately does not, so
    /// a skipped dish is still there to be searched for, and naming one outright
    /// still cooks it.
    /// </summary>
    private const string InThePool = @"
        NOT EXISTS (SELECT 1
                    FROM tracker.recipe_picks p
                    WHERE p.recipe_id = r.id
                      AND (p.status = 1 OR p.week_of = @weekOf))
        AND NOT (r.tags && @skipTags::text[])";

    /// <summary>
    /// The library as the history list shows it. week_of is the week it was the
    /// dish, or null if it has only ever been banked — the one column the
    /// cooked/not-cooked filter turns on, so it is computed in a subselect and
    /// filtered on from the outside.
    /// </summary>
    private const string SelectHistory = @"
        SELECT r.id AS recipe_id, r.title, f.name AS family_name, r.image_url,
               r.source_url, r.ready_minutes, r.servings, r.rating, r.notes,
               r.tags, r.pulled_at,
               (SELECT max(p.week_of)
                FROM tracker.recipe_picks p
                WHERE p.recipe_id = r.id AND p.status = 1) AS week_of
        FROM tracker.recipe_recipes r
        LEFT JOIN tracker.recipe_families f ON f.id = r.family_id";

    public async Task<List<RecipeFamily>> GetFamiliesAsync(IUnitOfWork unitOfWork)
    {
        var rows = await unitOfWork.Connection.QueryAsync<RecipeFamily>(
            $@"SELECT {SelectFamily}
               FROM tracker.recipe_families
               WHERE is_active
               ORDER BY sort_order, name",
            transaction: unitOfWork.Transaction);

        return rows.ToList();
    }

    public async Task<RecipeFamily?> GetFamilyByIdAsync(int id, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<RecipeFamily>(
            $"SELECT {SelectFamily} FROM tracker.recipe_families WHERE id = @id",
            new { id },
            unitOfWork.Transaction);
    }

    /// <summary>
    /// The rotation. A family that has never produced a dish sorts first
    /// (NULLS FIRST), which is what walks through the whole list before any
    /// cuisine comes round twice.
    /// </summary>
    public async Task<RecipeFamily?> GetLeastRecentlyUsedFamilyAsync(IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<RecipeFamily>(
            $@"SELECT {SelectFamily}
               FROM tracker.recipe_families f
               LEFT JOIN (
                   SELECT r.family_id, max(p.created_at) AS last_picked
                   FROM tracker.recipe_picks p
                   INNER JOIN tracker.recipe_recipes r ON r.id = p.recipe_id
                   GROUP BY r.family_id
               ) used ON used.family_id = f.id
               WHERE f.is_active
               ORDER BY used.last_picked NULLS FIRST, f.sort_order
               LIMIT 1",
            transaction: unitOfWork.Transaction);
    }

    public async Task<RecipePick?> GetCurrentPickAsync(DateOnly weekOf, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<RecipePick>(
            @"SELECT id, recipe_id, week_of, status, task_id, created_at
              FROM tracker.recipe_picks
              WHERE week_of = @weekOf AND status = @current",
            new { weekOf, current = (int)RecipePickStatus.Current },
            unitOfWork.Transaction);
    }

    public async Task<bool> HasAnyPickAsync(DateOnly weekOf, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS (SELECT 1 FROM tracker.recipe_picks WHERE week_of = @weekOf)",
            new { weekOf },
            unitOfWork.Transaction);
    }

    public async Task<RecipePick?> GetPickAsync(Guid id, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<RecipePick>(
            @"SELECT id, recipe_id, week_of, status, task_id, created_at
              FROM tracker.recipe_picks
              WHERE id = @id",
            new { id },
            unitOfWork.Transaction);
    }

    public async Task<Recipe?> GetRecipeAsync(Guid id, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<Recipe>(
            $"SELECT {SelectRecipe} FROM tracker.recipe_recipes r WHERE r.id = @id",
            new { id },
            unitOfWork.Transaction);
    }

    /// <summary>
    /// Random rather than oldest-first: the pool is a bag of dishes nobody has
    /// chosen between, and the whole scope is a lucky dip.
    /// </summary>
    public async Task<Recipe?> GetPoolRecipeAsync(int? familyId, DateOnly weekOf, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<Recipe>(
            $@"SELECT {SelectRecipe}
               FROM tracker.recipe_recipes r
               WHERE (@familyId::int IS NULL OR r.family_id = @familyId)
                 AND {InThePool}
               ORDER BY random()
               LIMIT 1",
            new { familyId, weekOf, skipTags = new[] { RecipeTags.NotPicked } },
            unitOfWork.Transaction);
    }

    /// <summary>
    /// ux_recipe_recipes_external is what makes banking a whole pull safe to
    /// repeat: a dish the source hands back a second time keeps the rating and
    /// notes it already has instead of being reset.
    /// </summary>
    public async Task SaveRecipesAsync(List<Recipe> recipes, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            @"INSERT INTO tracker.recipe_recipes
                  (id, source, external_id, family_id, title, image_url, source_url,
                   ready_minutes, servings, ingredients, steps)
              VALUES
                  (@Id, @Source, @ExternalId, @FamilyId, @Title, @ImageUrl, @SourceUrl,
                   @ReadyMinutes, @Servings, @Ingredients::jsonb, @Steps::jsonb)
              ON CONFLICT (source, external_id) DO NOTHING",
            recipes,
            unitOfWork.Transaction);
    }

    /// <summary>
    /// Cooked means the week it was the dish has passed. This week's dish is
    /// neither cooked nor back in the pool, so it only appears unfiltered — it
    /// has its own tab.
    /// </summary>
    public async Task<List<RecipeHistoryItemDto>> QueryHistoryAsync(
        string? search, string[]? tags, bool? cooked, DateOnly weekOf, IUnitOfWork unitOfWork)
    {
        var rows = await unitOfWork.Connection.QueryAsync<RecipeHistoryItemDto>(
            $@"SELECT * FROM (
                   {SelectHistory}
                   WHERE (@search::text IS NULL OR r.title ILIKE @search OR r.notes ILIKE @search)
                     AND (@tags::text[] IS NULL OR r.tags && @tags::text[])
               ) x
               WHERE (@cooked::boolean IS NULL
                      OR (@cooked AND x.week_of IS NOT NULL AND x.week_of < @weekOf)
                      OR (NOT @cooked AND x.week_of IS NULL))
               ORDER BY x.week_of DESC NULLS LAST, x.pulled_at DESC",
            new
            {
                search = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%",
                tags = tags is { Length: > 0 } ? tags : null,
                cooked,
                weekOf
            },
            unitOfWork.Transaction);

        return rows.ToList();
    }

    public async Task<RecipeHistoryItemDto?> GetHistoryItemAsync(Guid recipeId, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<RecipeHistoryItemDto>(
            $"{SelectHistory} WHERE r.id = @recipeId",
            new { recipeId },
            unitOfWork.Transaction);
    }

    public async Task<List<RecipeHistoryItemDto>> GetHistoryItemsBySourceAsync(string source, string[] externalIds, IUnitOfWork unitOfWork)
    {
        var rows = await unitOfWork.Connection.QueryAsync<RecipeHistoryItemDto>(
            $@"{SelectHistory}
               WHERE r.source = @source AND r.external_id = ANY(@externalIds::text[])
               ORDER BY r.title",
            new { source, externalIds },
            unitOfWork.Transaction);

        return rows.ToList();
    }

    public async Task UpdateRecipeMetaAsync(Guid recipeId, int? rating, string notes, string[] tags, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            @"UPDATE tracker.recipe_recipes
              SET rating = @rating,
                  notes  = @notes,
                  tags   = @tags
              WHERE id = @recipeId",
            new { recipeId, rating, notes, tags },
            unitOfWork.Transaction);
    }

    /// <summary>
    /// ux_recipe_picks_current is partial, so the conflict target has to repeat
    /// its predicate for Postgres to infer the index.
    /// </summary>
    public async Task<bool> TryCreatePickAsync(RecipePick pick, IUnitOfWork unitOfWork)
    {
        int affected = await unitOfWork.Connection.ExecuteAsync(
            @"INSERT INTO tracker.recipe_picks (id, recipe_id, week_of, status)
              VALUES (@Id, @RecipeId, @WeekOf, @Status)
              ON CONFLICT (week_of) WHERE status = 1 DO NOTHING",
            new { pick.Id, pick.RecipeId, pick.WeekOf, Status = (int)pick.Status },
            unitOfWork.Transaction);

        return affected > 0;
    }

    public async Task SupersedePickAsync(Guid pickId, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            "UPDATE tracker.recipe_picks SET status = @rerolled WHERE id = @pickId",
            new { pickId, rerolled = (int)RecipePickStatus.Rerolled },
            unitOfWork.Transaction);
    }

    public async Task SetPickTaskAsync(Guid pickId, Guid taskId, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            "UPDATE tracker.recipe_picks SET task_id = @taskId WHERE id = @pickId",
            new { pickId, taskId },
            unitOfWork.Transaction);
    }
}
