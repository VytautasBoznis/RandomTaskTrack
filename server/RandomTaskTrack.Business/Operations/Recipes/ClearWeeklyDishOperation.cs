using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Business.Recipes;
using RandomTaskTrack.Data.Models.Recipes;
using RandomTaskTrack.Data.Request.Recipes;
using RandomTaskTrack.Data.Response.Recipes;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Recipes;
using RandomTaskTrack.Interfaces.Services;

namespace RandomTaskTrack.Business.Operations.Recipes;

/// <summary>
/// "Actually, no." The way out of a dish you did not want, which reroll is not:
/// reroll needs the rotation, and the rotation needs an API call that can fail
/// on quota — leaving a dish picked from search with no way off the board.
///
/// The pick is superseded rather than deleted, which is the same state a reroll
/// leaves behind: not counted as cooked, and back in the pool once the week
/// turns over.
/// </summary>
public class ClearWeeklyDishOperation : BaseOperation<ClearWeeklyDishRequest, ClearWeeklyDishResponse>
{
    private readonly IRecipesRepository _recipesRepository;
    private readonly IClock _clock;

    public ClearWeeklyDishOperation(
        ILogger<ClearWeeklyDishOperation> logger,
        IValidator<ClearWeeklyDishRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IRecipesRepository recipesRepository,
        IClock clock) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _recipesRepository = recipesRepository;
        _clock = clock;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<ClearWeeklyDishResponse> Execute(ClearWeeklyDishRequest request, IUnitOfWork unitOfWork)
    {
        DateOnly weekOf = RecipeMapper.MondayOf(_clock.Today);

        RecipePick? current = await _recipesRepository.GetCurrentPickAsync(weekOf, unitOfWork);

        if (current is null)
        {
            return new ClearWeeklyDishResponse { Cleared = false };
        }

        await _recipesRepository.SupersedePickAsync(current.Id, unitOfWork);

        _logger.LogInformation("Cleared the dish for week of {WeekOf}", weekOf);

        return new ClearWeeklyDishResponse { Cleared = true };
    }
}
