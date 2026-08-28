using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Business.Recipes;
using RandomTaskTrack.Data.Dtos.Recipes;
using RandomTaskTrack.Data.Models.Recipes;
using RandomTaskTrack.Data.Request.Recipes;
using RandomTaskTrack.Data.Response.Recipes;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Recipes;
using RandomTaskTrack.Interfaces.Repositories.Recipes;

namespace RandomTaskTrack.Business.Operations.Recipes;

/// <summary>
/// Banks the dishes ticked on the search results. They go in with no family —
/// a free-text search matches whatever it matches, and guessing a cuisine from
/// it would corrupt the rotation's idea of which family is overdue.
///
/// Saved dishes have no pick row, which is exactly what "in the pool" means, so
/// the ones not chosen as this week's dish are pickable from the history tab
/// later and the rotation can offer them on its own.
/// </summary>
public class SaveRecipesOperation : BaseOperation<SaveRecipesRequest, SaveRecipesResponse>
{
    private readonly IRecipesRepository _recipesRepository;
    private readonly IRecipeSource _source;

    public SaveRecipesOperation(
        ILogger<SaveRecipesOperation> logger,
        IValidator<SaveRecipesRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IRecipesRepository recipesRepository,
        IRecipeSource source) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _recipesRepository = recipesRepository;
        _source = source;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<SaveRecipesResponse> Execute(SaveRecipesRequest request, IUnitOfWork unitOfWork)
    {
        List<Recipe> recipes = request.Recipes
            .Select(dish => RecipeMapper.ToRecipe(dish, _source.Name, familyId: null))
            .ToList();

        await _recipesRepository.SaveRecipesAsync(recipes, unitOfWork);

        // Read back by external id rather than trusting the ids just generated:
        // a dish already in the library kept its original row, and the caller
        // needs the id that is actually there to be able to cook it.
        List<RecipeHistoryItemDto> saved = await _recipesRepository.GetHistoryItemsBySourceAsync(
            _source.Name,
            recipes.Select(recipe => recipe.ExternalId).ToArray(),
            unitOfWork);

        _logger.LogInformation("Saved {Count} searched dishes to the library", saved.Count);

        return new SaveRecipesResponse { Recipes = saved };
    }
}
