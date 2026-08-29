using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Business.Recipes;
using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Models.Recipes;
using RandomTaskTrack.Data.Models.Tasks;
using RandomTaskTrack.Data.Request.Recipes;
using RandomTaskTrack.Data.Response.Recipes;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Recipes;
using RandomTaskTrack.Interfaces.Repositories.Tasks;
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
///
/// A dish put on the board goes with it. Leaving the cooking task behind is the
/// thing that made this feel like it had not worked: the dish was off This week
/// but still sitting under Today, with no obvious connection between the two.
/// </summary>
public class ClearWeeklyDishOperation : BaseOperation<ClearWeeklyDishRequest, ClearWeeklyDishResponse>
{
    private readonly IRecipesRepository _recipesRepository;
    private readonly ITasksRepository _tasksRepository;
    private readonly IClock _clock;

    public ClearWeeklyDishOperation(
        ILogger<ClearWeeklyDishOperation> logger,
        IValidator<ClearWeeklyDishRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IRecipesRepository recipesRepository,
        ITasksRepository tasksRepository,
        IClock clock) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _recipesRepository = recipesRepository;
        _tasksRepository = tasksRepository;
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

        // Only while it is still pending. Once it has been ticked off there is a
        // completion behind it, and task_completions cascades — cancelling next
        // week's plan is not a reason to erase what was cooked. The pick's
        // task_id clears itself either way (ON DELETE SET NULL).
        if (current.TaskId.HasValue)
        {
            TaskItem? task = await _tasksRepository.GetRawByIdAsync(current.TaskId.Value, unitOfWork);

            if (task is not null && task.Status == TaskItemStatus.Pending)
            {
                await _tasksRepository.DeleteAsync(task.Id, unitOfWork);
            }
        }

        _logger.LogInformation("Cleared the dish for week of {WeekOf}", weekOf);

        return new ClearWeeklyDishResponse { Cleared = true };
    }
}
