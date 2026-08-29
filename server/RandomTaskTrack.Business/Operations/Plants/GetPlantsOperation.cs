using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Business.Plants;
using RandomTaskTrack.Data.Dtos.Plants;
using RandomTaskTrack.Data.Dtos.Tasks;
using RandomTaskTrack.Data.Models.Plants;
using RandomTaskTrack.Data.Request.Plants;
using RandomTaskTrack.Data.Response.Plants;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Plants;

namespace RandomTaskTrack.Business.Operations.Plants;

/// <summary>
/// The whole tab in one round trip: every plant, its care schedule and the
/// tasks that schedule has on the board. Three queries rather than three per
/// plant — the tasks and recurrences are fetched for all of them at once and
/// grouped here.
/// </summary>
public class GetPlantsOperation : BaseOperation<GetPlantsRequest, GetPlantsResponse>
{
    private readonly IPlantsRepository _plantsRepository;

    public GetPlantsOperation(
        ILogger<GetPlantsOperation> logger,
        IValidator<GetPlantsRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IPlantsRepository plantsRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _plantsRepository = plantsRepository;
    }

    protected override async Task<GetPlantsResponse> Execute(GetPlantsRequest request, IUnitOfWork unitOfWork)
    {
        List<Plant> plants = await _plantsRepository.GetAllAsync(unitOfWork);

        if (plants.Count == 0)
        {
            return new GetPlantsResponse();
        }

        Guid[] ids = plants.Select(plant => plant.Id).ToArray();

        List<TaskListItemDto> tasks = await _plantsRepository.GetPendingTasksAsync(ids, unitOfWork);
        List<RecurrenceListItemDto> recurrences = await _plantsRepository.GetCareRecurrencesAsync(ids, unitOfWork);
        List<PlantPhotoDto> photos = await _plantsRepository.GetPhotosAsync(ids, unitOfWork);

        var tasksByPlant = Group(tasks, task => PlantMapper.PlantIdOf(task.Data));
        var recurrencesByPlant = Group(recurrences, recurrence => PlantMapper.PlantIdOf(recurrence.Data));
        var photosByPlant = Group(photos, photo => photo.PlantId);

        return new GetPlantsResponse
        {
            Plants = plants
                .Select(plant => PlantMapper.ToDto(
                    plant,
                    tasksByPlant.GetValueOrDefault(plant.Id) ?? new List<TaskListItemDto>(),
                    recurrencesByPlant.GetValueOrDefault(plant.Id) ?? new List<RecurrenceListItemDto>(),
                    photosByPlant.GetValueOrDefault(plant.Id) ?? new List<PlantPhotoDto>()))
                .ToList()
        };
    }

    /// <summary>
    /// The rows come back filtered to these plants already, so a payload that
    /// no longer parses to an id can only be a race with a delete. Dropped
    /// rather than defended against.
    /// </summary>
    private static Dictionary<Guid, List<T>> Group<T>(List<T> rows, Func<T, Guid?> plantIdOf) =>
        rows.Select(row => (PlantId: plantIdOf(row), Row: row))
            .Where(pair => pair.PlantId.HasValue)
            .GroupBy(pair => pair.PlantId!.Value)
            .ToDictionary(group => group.Key, group => group.Select(pair => pair.Row).ToList());
}
