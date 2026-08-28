using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Business.Recipes;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Models.Exceptions;
using RandomTaskTrack.Data.Models.Recipes;
using RandomTaskTrack.Data.Request.Recipes;
using RandomTaskTrack.Data.Response.Recipes;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Recipes;
using RandomTaskTrack.Interfaces.Services;

namespace RandomTaskTrack.Business.Operations.Recipes;

/// <summary>
/// Cook this one. The manual counterpart to a reroll: same supersede-then-insert
/// dance, but the dish is named instead of drawn, so no source call and no
/// cuisine rotation is involved.
/// </summary>
public class SetWeeklyDishOperation : BaseOperation<SetWeeklyDishRequest, SetWeeklyDishResponse>
{
    private readonly IRecipesRepository _recipesRepository;
    private readonly IClock _clock;

    public SetWeeklyDishOperation(
        ILogger<SetWeeklyDishOperation> logger,
        IValidator<SetWeeklyDishRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IRecipesRepository recipesRepository,
        IClock clock) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _recipesRepository = recipesRepository;
        _clock = clock;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<SetWeeklyDishResponse> Execute(SetWeeklyDishRequest request, IUnitOfWork unitOfWork)
    {
        DateOnly weekOf = RecipeMapper.MondayOf(_clock.Today);

        Recipe recipe = await _recipesRepository.GetRecipeAsync(request.RecipeId, unitOfWork)
                        ?? throw new NotFoundException($"No recipe with id {request.RecipeId}", ExceptionCodes.RECIPE_NOT_FOUND);

        RecipePick? current = await _recipesRepository.GetCurrentPickAsync(weekOf, unitOfWork);

        if (current is not null)
        {
            if (current.RecipeId == recipe.Id)
            {
                return await BuildResponseAsync(current, recipe, unitOfWork);
            }

            // Superseded first so the partial unique index has room for the new
            // pick, exactly as a reroll does.
            await _recipesRepository.SupersedePickAsync(current.Id, unitOfWork);
        }

        var pick = new RecipePick
        {
            Id = Guid.NewGuid(),
            RecipeId = recipe.Id,
            WeekOf = weekOf,
            Status = RecipePickStatus.Current
        };

        if (!await _recipesRepository.TryCreatePickAsync(pick, unitOfWork))
        {
            // Another tab set a dish between the supersede and the insert.
            pick = await _recipesRepository.GetCurrentPickAsync(weekOf, unitOfWork)
                   ?? throw new NotFoundException("The week's dish disappeared mid-pick.", ExceptionCodes.RECIPE_PICK_NOT_FOUND);

            recipe = await _recipesRepository.GetRecipeAsync(pick.RecipeId, unitOfWork)
                     ?? throw new NotFoundException("The picked dish is missing.", ExceptionCodes.RECIPE_PICK_NOT_FOUND);
        }

        _logger.LogInformation("Set {Title} as the dish for week of {WeekOf}", recipe.Title, weekOf);

        return await BuildResponseAsync(pick, recipe, unitOfWork);
    }

    private async Task<SetWeeklyDishResponse> BuildResponseAsync(RecipePick pick, Recipe recipe, IUnitOfWork unitOfWork)
    {
        RecipeFamily? family = recipe.FamilyId.HasValue
            ? await _recipesRepository.GetFamilyByIdAsync(recipe.FamilyId.Value, unitOfWork)
            : null;

        return new SetWeeklyDishResponse { Dish = RecipeMapper.ToDish(pick, recipe, family?.Name) };
    }
}
