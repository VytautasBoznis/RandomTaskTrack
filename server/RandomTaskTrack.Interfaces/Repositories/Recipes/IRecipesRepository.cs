using RandomTaskTrack.Data.Dtos.Recipes;
using RandomTaskTrack.Data.Models.Recipes;
using RandomTaskTrack.Interfaces.Base;

namespace RandomTaskTrack.Interfaces.Repositories.Recipes;

public interface IRecipesRepository
{
    Task<List<RecipeFamily>> GetFamiliesAsync(IUnitOfWork unitOfWork);

    Task<RecipeFamily?> GetFamilyByIdAsync(int id, IUnitOfWork unitOfWork);

    /// <summary>The active family that has gone longest without a dish.</summary>
    Task<RecipeFamily?> GetLeastRecentlyUsedFamilyAsync(IUnitOfWork unitOfWork);

    Task<RecipePick?> GetCurrentPickAsync(DateOnly weekOf, IUnitOfWork unitOfWork);

    Task<RecipePick?> GetPickAsync(Guid id, IUnitOfWork unitOfWork);

    Task<Recipe?> GetRecipeAsync(Guid id, IUnitOfWork unitOfWork);

    /// <summary>
    /// One banked dish of that family that is fair game this week: never the
    /// weekly dish before, and not already rerolled out of this week. Null when
    /// the pool is dry and the source has to be called.
    /// </summary>
    Task<Recipe?> GetPoolRecipeAsync(int? familyId, DateOnly weekOf, IUnitOfWork unitOfWork);

    /// <summary>Banks a pull. Dishes already in the library are left alone, so
    /// their rating and notes survive being pulled again.</summary>
    Task SaveRecipesAsync(List<Recipe> recipes, IUnitOfWork unitOfWork);

    Task<List<RecipeHistoryItemDto>> QueryHistoryAsync(string? search, string[]? tags, bool? cooked, DateOnly weekOf, IUnitOfWork unitOfWork);

    Task<RecipeHistoryItemDto?> GetHistoryItemAsync(Guid recipeId, IUnitOfWork unitOfWork);

    /// <summary>Library rows for dishes just saved, looked up the way the source
    /// names them — the ids are the library's, not the caller's.</summary>
    Task<List<RecipeHistoryItemDto>> GetHistoryItemsBySourceAsync(string source, string[] externalIds, IUnitOfWork unitOfWork);

    Task UpdateRecipeMetaAsync(Guid recipeId, int? rating, string notes, string[] tags, IUnitOfWork unitOfWork);

    /// <summary>False when another caller already picked this week's dish.</summary>
    Task<bool> TryCreatePickAsync(RecipePick pick, IUnitOfWork unitOfWork);

    Task SupersedePickAsync(Guid pickId, IUnitOfWork unitOfWork);

    Task SetPickTaskAsync(Guid pickId, Guid taskId, IUnitOfWork unitOfWork);
}
