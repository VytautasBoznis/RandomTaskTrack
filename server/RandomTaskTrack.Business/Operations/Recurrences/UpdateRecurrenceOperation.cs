using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Exceptions;
using RandomTaskTrack.Data.Models.Tasks;
using RandomTaskTrack.Data.Request.Recurrences;
using RandomTaskTrack.Data.Response.Recurrences;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Recurrences;
using RandomTaskTrack.Interfaces.Repositories.Tasks;
using RandomTaskTrack.Interfaces.Services;

namespace RandomTaskTrack.Business.Operations.Recurrences;

public class UpdateRecurrenceOperation : BaseOperation<UpdateRecurrenceRequest, UpdateRecurrenceResponse>
{
    private readonly IRecurrencesRepository _recurrencesRepository;
    private readonly ITasksRepository _tasksRepository;
    private readonly IRecurrenceMaterializer _materializer;
    private readonly IClock _clock;

    public UpdateRecurrenceOperation(
        ILogger<UpdateRecurrenceOperation> logger,
        IValidator<UpdateRecurrenceRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IRecurrencesRepository recurrencesRepository,
        ITasksRepository tasksRepository,
        IRecurrenceMaterializer materializer,
        IClock clock) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _recurrencesRepository = recurrencesRepository;
        _tasksRepository = tasksRepository;
        _materializer = materializer;
        _clock = clock;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<UpdateRecurrenceResponse> Execute(UpdateRecurrenceRequest request, IUnitOfWork unitOfWork)
    {
        TaskRecurrence recurrence = await _recurrencesRepository.GetRawByIdAsync(request.Id, unitOfWork)
                                    ?? throw new NotFoundException($"No recurrence with id {request.Id}", ExceptionCodes.RECURRENCE_NOT_FOUND);

        bool scheduleChanged =
            (request.RuleType.HasValue && request.RuleType.Value != recurrence.RuleType) ||
            (request.IntervalDays.HasValue && request.IntervalDays != recurrence.IntervalDays) ||
            (request.DaysOfWeek is not null) ||
            (request.DayOfMonth.HasValue && request.DayOfMonth != recurrence.DayOfMonth) ||
            (request.AnchorMode.HasValue && request.AnchorMode.Value != recurrence.AnchorMode) ||
            (request.TimeOfDay.HasValue && request.TimeOfDay != recurrence.TimeOfDay);

        recurrence.Title = request.Title ?? recurrence.Title;
        recurrence.Notes = request.Notes ?? recurrence.Notes;
        recurrence.Data = string.IsNullOrWhiteSpace(request.Data) ? recurrence.Data : request.Data;
        recurrence.RuleType = request.RuleType ?? recurrence.RuleType;
        recurrence.IntervalDays = request.IntervalDays ?? recurrence.IntervalDays;
        recurrence.DaysOfWeek = request.DaysOfWeek ?? recurrence.DaysOfWeek;
        recurrence.DayOfMonth = request.DayOfMonth ?? recurrence.DayOfMonth;
        recurrence.AnchorMode = request.AnchorMode ?? recurrence.AnchorMode;
        recurrence.TimeOfDay = request.TimeOfDay ?? recurrence.TimeOfDay;
        recurrence.EndsOn = request.EndsOn ?? recurrence.EndsOn;
        recurrence.IsActive = request.IsActive ?? recurrence.IsActive;

        await _recurrencesRepository.UpdateAsync(recurrence, unitOfWork);

        int materialized = 0;

        if (scheduleChanged)
        {
            // The schedule moved, so already-materialized future instances are
            // wrong. Drop the pending ones from today forward, reset the
            // watermark, and rebuild. Completed history is untouched.
            await _tasksRepository.DeletePendingByRecurrenceAsync(recurrence.Id, _clock.Today, unitOfWork);
            recurrence.LastDueOn = null;

            materialized = await _materializer.MaterializeOneAsync(recurrence, unitOfWork, CancellationToken.None);
        }
        else if (recurrence.IsActive)
        {
            materialized = await _materializer.MaterializeOneAsync(recurrence, unitOfWork, CancellationToken.None);
        }

        _logger.LogInformation(
            "Updated recurrence {Id}; scheduleChanged={ScheduleChanged}, materialized={Materialized}",
            recurrence.Id, scheduleChanged, materialized);

        return new UpdateRecurrenceResponse
        {
            Recurrence = await _recurrencesRepository.GetByIdAsync(recurrence.Id, unitOfWork)
                         ?? throw new NotFoundException("Recurrence not found after update", ExceptionCodes.RECURRENCE_NOT_FOUND)
        };
    }
}
