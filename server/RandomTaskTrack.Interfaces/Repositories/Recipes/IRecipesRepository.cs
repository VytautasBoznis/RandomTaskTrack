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

    Task<List<string>> GetSeenExternalIdsAsync(string source, IUnitOfWork unitOfWork);

    Task CreateRecipeAsync(Recipe recipe, IUnitOfWork unitOfWork);

    /// <summary>False when another caller already picked this week's dish.</summary>
    Task<bool> TryCreatePickAsync(RecipePick pick, IUnitOfWork unitOfWork);

    Task SupersedePickAsync(Guid pickId, IUnitOfWork unitOfWork);

    Task SetPickTaskAsync(Guid pickId, Guid taskId, IUnitOfWork unitOfWork);
}
