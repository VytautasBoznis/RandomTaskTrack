using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Business.Plants;
using RandomTaskTrack.Data.Dtos.Tasks;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Models.Exceptions;
using RandomTaskTrack.Data.Models.Plants;
using RandomTaskTrack.Data.Models.Tasks;
using RandomTaskTrack.Data.Request.Plants;
using RandomTaskTrack.Data.Response.Plants;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Domains;
using RandomTaskTrack.Interfaces.Repositories.Plants;
using RandomTaskTrack.Interfaces.Repositories.Tasks;

namespace RandomTaskTrack.Business.Operations.Plants;

/// <summary>
/// Puts a seed packet's plan on the board against a real sowing date.
///
/// One-off dated tasks, not recurrences — the deliberate opposite of a care
/// schedule. Sowing happens once, germination is watched for once, and the
/// harvest is a date to look forward to; a repeating "sow" would be nonsense.
/// The whole chain is dated from the sowing day the user picks, so sowing a
/// fortnight late moves everything after it by a fortnight.
/// </summary>
public class CreateSowingPlanOperation : BaseOperation<CreateSowingPlanRequest, CreateSowingPlanResponse>
{
    private readonly IPlantsRepository _plantsRepository;
    private readonly ITasksRepository _tasksRepository;
    private readonly IDomainsRepository _domainsRepository;

    public CreateSowingPlanOperation(
        ILogger<CreateSowingPlanOperation> logger,
        IValidator<CreateSowingPlanRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IPlantsRepository plantsRepository,
        ITasksRepository tasksRepository,
        IDomainsRepository domainsRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _plantsRepository = plantsRepository;
        _tasksRepository = tasksRepository;
        _domainsRepository = domainsRepository;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<CreateSowingPlanResponse> Execute(CreateSowingPlanRequest request, IUnitOfWork unitOfWork)
    {
        Plant plant = await _plantsRepository.GetByIdAsync(request.PlantId, unitOfWork)
                      ?? throw new NotFoundException($"No plant with id {request.PlantId}", ExceptionCodes.PLANT_NOT_FOUND);

        TaskDomain domain = await _domainsRepository.GetByCodeAsync(DomainCodes.Plants, unitOfWork)
                            ?? throw new NotFoundException($"No '{DomainCodes.Plants}' domain to file the sowing under.", ExceptionCodes.DOMAIN_NOT_FOUND);

        List<TaskListItemDto> pending = await _plantsRepository.GetPendingTasksAsync([plant.Id], unitOfWork);

        // Same double-press guard the care schedule has, on the same key.
        var already = pending
            .Select(task => PlantMapper.CareTitleOf(task.Data))
            .Where(title => title is not null)
            .ToHashSet(StringComparer.OrdinalIgnoreCase)!;

        int created = 0;

        foreach (PlantSowingStep step in request.Steps)
        {
            if (!already.Add(step.Title))
            {
                continue;
            }

            await _tasksRepository.CreateAsync(new TaskItem
            {
                Id = Guid.NewGuid(),
                DomainId = domain.Id,
                Title = $"{step.Title} — {plant.Name}",
                Notes = string.IsNullOrWhiteSpace(step.Notes) ? null : step.Notes,
                Data = PlantMapper.PayloadFor(plant.Id, step.Title),
                DueOn = request.SowOn.AddDays(step.DayOffset),
                Status = TaskItemStatus.Pending
            }, unitOfWork);

            created++;
        }

        return new CreateSowingPlanResponse
        {
            Plant = await PlantLoader.LoadAsync(plant.Id, _plantsRepository, unitOfWork),
            CreatedTaskCount = created
        };
    }
}
