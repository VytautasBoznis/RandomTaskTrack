using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Business.Learning;
using RandomTaskTrack.Data.Dtos.Tasks;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Models.Exceptions;
using RandomTaskTrack.Data.Models.Learning;
using RandomTaskTrack.Data.Request.Learning;
using RandomTaskTrack.Data.Response.Learning;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Learning;
using RandomTaskTrack.Interfaces.Repositories.Tasks;
using RandomTaskTrack.Interfaces.Services;

namespace RandomTaskTrack.Business.Operations.Learning;

/// <summary>
/// Advances a step, or records what came of it. Both are this one endpoint: an
/// exam that was sat and failed is a status and an outcome written in the same
/// gesture, and splitting them would mean two round trips to say one thing.
///
/// Finishing a step also closes whatever it has on the board. The dashboard
/// shows a path as one line pointing here, so this is the only place the task
/// can be cleared from — left open it would sit there overdue forever.
/// </summary>
public class UpdateLearningStepOperation : BaseOperation<UpdateLearningStepRequest, UpdateLearningStepResponse>
{
    private readonly ILearningRepository _learningRepository;
    private readonly ITasksRepository _tasksRepository;
    private readonly IClock _clock;

    public UpdateLearningStepOperation(
        ILogger<UpdateLearningStepOperation> logger,
        IValidator<UpdateLearningStepRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        ILearningRepository learningRepository,
        ITasksRepository tasksRepository,
        IClock clock) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _learningRepository = learningRepository;
        _tasksRepository = tasksRepository;
        _clock = clock;
    }

    /// <summary>Two tables now: the step, and the task it had on the board.</summary>
    protected override bool RequiresTransaction => true;

    protected override async Task<UpdateLearningStepResponse> Execute(UpdateLearningStepRequest request, IUnitOfWork unitOfWork)
    {
        LearningStep step = await _learningRepository.GetStepAsync(request.Id, unitOfWork)
                            ?? throw new NotFoundException($"No learning step with id {request.Id}", ExceptionCodes.LEARNING_STEP_NOT_FOUND);

        step.Title = request.Title.Trim();
        step.Kind = request.Kind;
        step.Status = request.Status;
        step.TargetOn = request.TargetOn;
        step.Notes = request.Notes?.Trim() ?? "";
        step.Outcome = request.Outcome?.Trim() ?? "";
        step.Provider = Clean(request.Provider);
        step.Url = Clean(request.Url);
        step.Cost = Clean(request.Cost);
        step.Hours = request.Hours;
        step.SortOrder = request.SortOrder;

        await _learningRepository.UpdateStepAsync(step, unitOfWork);

        await CloseBoardTaskAsync(step, unitOfWork);

        return new UpdateLearningStepResponse
        {
            Goal = await LearningLoader.LoadGoalAsync(step.GoalId, _clock.Today, _learningRepository, unitOfWork)
        };
    }

    /// <summary>
    /// Finished the step, so finish what it had on the board. Done lands in the
    /// completion log as done and a dropped step as skipped — the distinction is
    /// the point of the log, and a step decided against was not achieved.
    ///
    /// Reads pending tasks only, so this is safe to re-run: editing a step that
    /// was already finished finds nothing left to close.
    /// </summary>
    private async Task CloseBoardTaskAsync(LearningStep step, IUnitOfWork unitOfWork)
    {
        if (step.Status != LearningStepStatus.Done && step.Status != LearningStepStatus.Dropped)
        {
            return;
        }

        List<TaskListItemDto> open = await _learningRepository.GetStepTasksAsync([step.Id], unitOfWork);

        TaskItemStatus status = step.Status == LearningStepStatus.Done
            ? TaskItemStatus.Done
            : TaskItemStatus.Skipped;

        foreach (TaskListItemDto task in open)
        {
            await _tasksRepository.MarkCompletedAsync(task.Id, status, DateTime.UtcNow, unitOfWork);
        }
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
