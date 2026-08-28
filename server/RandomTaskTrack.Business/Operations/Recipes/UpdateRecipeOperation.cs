using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Business.Recipes;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Exceptions;
using RandomTaskTrack.Data.Models.Recipes;
using RandomTaskTrack.Data.Request.Recipes;
using RandomTaskTrack.Data.Response.Recipes;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Recipes;

namespace RandomTaskTrack.Business.Operations.Recipes;

/// <summary>
/// The verdict on a dish — stars, what to do differently, and tags. Lives on the
/// recipe rather than on the week it was cooked: cook a dish twice and you are
/// correcting one opinion, not keeping two.
/// </summary>
public class UpdateRecipeOperation : BaseOperation<UpdateRecipeRequest, UpdateRecipeResponse>
{
    private readonly IRecipesRepository _recipesRepository;

    public UpdateRecipeOperation(
        ILogger<UpdateRecipeOperation> logger,
        IValidator<UpdateRecipeRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IRecipesRepository recipesRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _recipesRepository = recipesRepository;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<UpdateRecipeResponse> Execute(UpdateRecipeRequest request, IUnitOfWork unitOfWork)
    {
        Recipe recipe = await _recipesRepository.GetRecipeAsync(request.RecipeId, unitOfWork)
                        ?? throw new NotFoundException($"No recipe with id {request.RecipeId}", ExceptionCodes.RECIPE_NOT_FOUND);

        // Null leaves a field alone, as in UpdateNoteRequest. ClearRating is the
        // exception, because "no longer rated" is not something null can say.
        int? rating = request.ClearRating ? null : request.Rating ?? recipe.Rating;

        await _recipesRepository.UpdateRecipeMetaAsync(
            recipe.Id,
            rating,
            request.Notes ?? recipe.Notes,
            request.Tags is null ? recipe.Tags : RecipeMapper.NormaliseTags(request.Tags),
            unitOfWork);

        return new UpdateRecipeResponse
        {
            Recipe = await _recipesRepository.GetHistoryItemAsync(recipe.Id, unitOfWork)
                     ?? throw new NotFoundException("Recipe not found after update", ExceptionCodes.RECIPE_NOT_FOUND)
        };
    }
}
