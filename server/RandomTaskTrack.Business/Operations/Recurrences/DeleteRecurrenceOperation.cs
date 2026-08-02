using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Exceptions;
using RandomTaskTrack.Data.Request.Recurrences;
using RandomTaskTrack.Data.Response.Recurrences;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Recurrences;
using RandomTaskTrack.Interfaces.Repositories.Tasks;
using RandomTaskTrack.Interfaces.Services;

namespace RandomTaskTrack.Business.Operations.Recurrences;

public class DeleteRecurrenceOperation : BaseOperation<DeleteRecurrenceRequest, DeleteRecurrenceResponse>
{
    private readonly IRecurrencesRepository _recurrencesRepository;
    private readonly ITasksRepository _tasksRepository;
    private readonly IClock _clock;

    public DeleteRecurrenceOperation(
        ILogger<DeleteRecurrenceOperation> logger,
        IValidator<DeleteRecurrenceRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IRecurrencesRepository recurrencesRepository,
        ITasksRepository tasksRepository,
        IClock clock) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _recurrencesRepository = recurrencesRepository;
        _tasksRepository = tasksRepository;
        _clock = clock;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<DeleteRecurrenceResponse> Execute(DeleteRecurrenceRequest request, IUnitOfWork unitOfWork)
    {
        int removed = 0;

        // Only pending future instances go. Anything already completed stays as
        // history, which is why the FK is ON DELETE SET NULL rather than CASCADE.
        if (request.DeleteFutureTasks)
        {
            removed = await _tasksRepository.DeletePendingByRecurrenceAsync(request.Id, _clock.Today, unitOfWork);
        }

        bool deleted = await _recurrencesRepository.DeleteAsync(request.Id, unitOfWork);

        if (!deleted)
        {
            throw new NotFoundException("No recurrence with that id", ExceptionCodes.RECURRENCE_NOT_FOUND);
        }

        return new DeleteRecurrenceResponse { Success = true, DeletedTaskCount = removed };
    }
}
