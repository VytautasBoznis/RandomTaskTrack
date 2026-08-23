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
using RandomTaskTrack.Interfaces.Services;

namespace RandomTaskTrack.Business.Operations.Recipes;

/// <summary>
/// Swaps this week's dish for another. The old pick is kept as Rerolled rather
/// than deleted, which is what stops the same dish coming back next week.
/// </summary>
public class RerollDishOperation : BaseOperation<RerollDishRequest, RerollDishResponse>
{
    private readonly IRecipesRepository _recipesRepository;
    private readonly IRecipePicker _picker;
    private readonly IClock _clock;

    public RerollDishOperation(
        ILogger<RerollDishOperation> logger,
        IValidator<RerollDishRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IRecipesRepository recipesRepository,
        IRecipePicker picker,
        IClock clock) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _recipesRepository = recipesRepository;
        _picker = picker;
        _clock = clock;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<RerollDishResponse> Execute(RerollDishRequest request, IUnitOfWork unitOfWork)
    {
        DateOnly weekOf = RecipeMapper.MondayOf(_clock.Today);

        RecipePick? current = await _recipesRepository.GetCurrentPickAsync(weekOf, unitOfWork);

        if (current is not null)
        {
            // Superseded first so the partial unique index has room for the new
            // pick. If the pull then fails the transaction takes this back with
            // it, so a failed reroll leaves the week's dish exactly as it was.
            await _recipesRepository.SupersedePickAsync(current.Id, unitOfWork);
        }

        RecipePick pick = await _picker.PickAsync(weekOf, request.FamilyId, unitOfWork, CancellationToken.None);

        Recipe recipe = await _recipesRepository.GetRecipeAsync(pick.RecipeId, unitOfWork)
                        ?? throw new NotFoundException("The picked dish is missing.", ExceptionCodes.RECIPE_PICK_NOT_FOUND);

        RecipeFamily? family = recipe.FamilyId.HasValue
            ? await _recipesRepository.GetFamilyByIdAsync(recipe.FamilyId.Value, unitOfWork)
            : null;

        return new RerollDishResponse { Dish = RecipeMapper.ToDish(pick, recipe, family?.Name) };
    }
}
