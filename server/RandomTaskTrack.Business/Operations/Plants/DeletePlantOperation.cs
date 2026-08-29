using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Data.Dtos.Tasks;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Exceptions;
using RandomTaskTrack.Data.Models.Plants;
using RandomTaskTrack.Data.Request.Plants;
using RandomTaskTrack.Data.Response.Plants;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Plants;
using RandomTaskTrack.Interfaces.Repositories.Recurrences;

namespace RandomTaskTrack.Business.Operations.Plants;

/// <summary>
/// Takes the plant and everything scheduled for it.
///
/// This is the price of linking care tasks by payload instead of by foreign
/// key: nothing in the database stops a watering task outliving the plant, so
/// the delete has to sweep up after itself. Completed history stays — what was
/// done was done.
/// </summary>
public class DeletePlantOperation : BaseOperation<DeletePlantRequest, DeletePlantResponse>
{
    private readonly IPlantsRepository _plantsRepository;
    private readonly IRecurrencesRepository _recurrencesRepository;

    public DeletePlantOperation(
        ILogger<DeletePlantOperation> logger,
        IValidator<DeletePlantRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IPlantsRepository plantsRepository,
        IRecurrencesRepository recurrencesRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _plantsRepository = plantsRepository;
        _recurrencesRepository = recurrencesRepository;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<DeletePlantResponse> Execute(DeletePlantRequest request, IUnitOfWork unitOfWork)
    {
        Plant plant = await _plantsRepository.GetByIdAsync(request.Id, unitOfWork)
                      ?? throw new NotFoundException($"No plant with id {request.Id}", ExceptionCodes.PLANT_NOT_FOUND);

        List<RecurrenceListItemDto> recurrences =
            await _plantsRepository.GetCareRecurrencesAsync([plant.Id], unitOfWork);

        // Tasks first: task_tasks.recurrence_id is ON DELETE SET NULL, so
        // removing the schedules first would cut the pending instances loose
        // before this had a chance to find them.
        int deletedTasks = await _plantsRepository.DeletePendingTasksAsync(plant.Id, unitOfWork);

        int deletedRecurrences = 0;

        foreach (RecurrenceListItemDto recurrence in recurrences)
        {
            if (await _recurrencesRepository.DeleteAsync(recurrence.Id, unitOfWork))
            {
                deletedRecurrences++;
            }
        }

        return new DeletePlantResponse
        {
            Success = await _plantsRepository.DeleteAsync(plant.Id, unitOfWork),
            DeletedRecurrenceCount = deletedRecurrences,
            DeletedTaskCount = deletedTasks
        };
    }
}
