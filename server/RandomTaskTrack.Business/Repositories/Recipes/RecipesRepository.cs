using Dapper;
using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Models.Recipes;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Recipes;

namespace RandomTaskTrack.Business.Repositories.Recipes;

public class RecipesRepository : IRecipesRepository
{
    private const string SelectFamily = "id, code, name, is_active, sort_order";

    private const string SelectRecipe = @"
        id, source, external_id, family_id, title, image_url, source_url,
        ready_minutes, servings, ingredients::text AS ingredients,
        steps::text AS steps, pulled_at";

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
            $"SELECT {SelectRecipe} FROM tracker.recipe_recipes WHERE id = @id",
            new { id },
            unitOfWork.Transaction);
    }

    public async Task<List<string>> GetSeenExternalIdsAsync(string source, IUnitOfWork unitOfWork)
    {
        var rows = await unitOfWork.Connection.QueryAsync<string>(
            "SELECT external_id FROM tracker.recipe_recipes WHERE source = @source",
            new { source },
            unitOfWork.Transaction);

        return rows.ToList();
    }

    public async Task CreateRecipeAsync(Recipe recipe, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            @"INSERT INTO tracker.recipe_recipes
                  (id, source, external_id, family_id, title, image_url, source_url,
                   ready_minutes, servings, ingredients, steps)
              VALUES
                  (@Id, @Source, @ExternalId, @FamilyId, @Title, @ImageUrl, @SourceUrl,
                   @ReadyMinutes, @Servings, @Ingredients::jsonb, @Steps::jsonb)",
            recipe,
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
