using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Models.Exceptions;
using RandomTaskTrack.Data.Models.Tasks;
using RandomTaskTrack.Data.Request.Tasks;
using RandomTaskTrack.Data.Response.Tasks;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Recurrences;
using RandomTaskTrack.Interfaces.Repositories.Tasks;
using RandomTaskTrack.Interfaces.Services;

namespace RandomTaskTrack.Business.Operations.Tasks;

/// <summary>
/// Ticking a box does three things, and they have to happen together:
/// mark the task, append the completion log row, and — for a from-completion
/// recurrence — spawn the next instance measured from now.
/// </summary>
public class CompleteTaskOperation : BaseOperation<CompleteTaskRequest, CompleteTaskResponse>
{
    private readonly ITasksRepository _tasksRepository;
    private readonly ICompletionsRepository _completionsRepository;
    private readonly IRecurrencesRepository _recurrencesRepository;
    private readonly IRecurrenceMaterializer _materializer;
    private readonly IClock _clock;

    public CompleteTaskOperation(
        ILogger<CompleteTaskOperation> logger,
        IValidator<CompleteTaskRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        ITasksRepository tasksRepository,
        ICompletionsRepository completionsRepository,
        IRecurrencesRepository recurrencesRepository,
        IRecurrenceMaterializer materializer,
        IClock clock) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _tasksRepository = tasksRepository;
        _completionsRepository = completionsRepository;
        _recurrencesRepository = recurrencesRepository;
        _materializer = materializer;
        _clock = clock;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<CompleteTaskResponse> Execute(CompleteTaskRequest request, IUnitOfWork unitOfWork)
    {
        TaskItem task = await _tasksRepository.GetRawByIdAsync(request.Id, unitOfWork)
                        ?? throw new NotFoundException($"No task with id {request.Id}", ExceptionCodes.TASK_NOT_FOUND);

        if (task.Status != TaskItemStatus.Pending)
        {
            throw new BadRequestException(
                "Task is already completed",
                ExceptionCodes.TASK_ALREADY_COMPLETED,
                $"Task {task.Id} is {task.Status}.");
        }

        DateTime completedAt = _clock.UtcNow;

        await _tasksRepository.MarkCompletedAsync(task.Id, request.Status, completedAt, unitOfWork);

        // planned vs actual is the whole point of this table. When the user
        // ticks the box without editing anything, actual == planned, and that
        // is still a meaningful record.
        var completion = new TaskCompletion
        {
            Id = Guid.NewGuid(),
            TaskId = task.Id,
            DomainId = task.DomainId,
            Status = request.Status,
            PlannedData = task.Data,
            ActualData = string.IsNullOrWhiteSpace(request.ActualData) ? task.Data : request.ActualData,
            Note = request.Note,
            DueOn = task.DueOn,
            CompletedAt = completedAt
        };

        await _completionsRepository.CreateAsync(completion, unitOfWork);

        var response = new CompleteTaskResponse
        {
            CompletionId = completion.Id,
            Task = await _tasksRepository.GetByIdAsync(task.Id, unitOfWork)
                   ?? throw new NotFoundException("Task not found after completion", ExceptionCodes.TASK_NOT_FOUND)
        };

        await ChainNextOccurrenceAsync(task, completedAt, response, unitOfWork);

        return response;
    }

    /// <summary>
    /// From-completion recurrences only ever have one open instance, so the
    /// next one is created here rather than by the horizon sweep. Doing it late
    /// is exactly the point: complete a 7-day chore on day 9 and the next is
    /// day 16, not day 14.
    /// </summary>
    private async Task ChainNextOccurrenceAsync(TaskItem task, DateTime completedAt, CompleteTaskResponse response, IUnitOfWork unitOfWork)
    {
        if (!task.RecurrenceId.HasValue)
        {
            return;
        }

        TaskRecurrence? recurrence = await _recurrencesRepository.GetRawByIdAsync(task.RecurrenceId.Value, unitOfWork);

        if (recurrence is null || recurrence.AnchorMode != RecurrenceAnchorMode.FromCompletion)
        {
            return;
        }

        DateOnly completedOn = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(completedAt, _clock.TimeZone));
        DateOnly? nextDue = _materializer.GetNextDueAfterCompletion(recurrence, completedOn);

        if (!nextDue.HasValue)
        {
            return;
        }

        var next = new TaskItem
        {
            Id = Guid.NewGuid(),
            DomainId = recurrence.DomainId,
            RecurrenceId = recurrence.Id,
            Title = recurrence.Title,
            Notes = recurrence.Notes,
            Data = recurrence.Data,
            DueOn = nextDue.Value,
            DueTime = recurrence.TimeOfDay,
            Status = TaskItemStatus.Pending
        };

        // Can no-op if an instance already sits on that date; either way the
        // watermark moves so the sweep does not double up.
        if (await _tasksRepository.TryCreateFromRecurrenceAsync(next, unitOfWork))
        {
            response.NextTaskId = next.Id;
            response.NextDueOn = next.DueOn;
        }

        await _recurrencesRepository.UpdateLastDueOnAsync(recurrence.Id, nextDue.Value, unitOfWork);
    }
}
