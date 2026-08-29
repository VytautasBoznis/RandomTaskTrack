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
using RandomTaskTrack.Interfaces.Repositories.Recurrences;
using RandomTaskTrack.Interfaces.Services;

namespace RandomTaskTrack.Business.Operations.Plants;

/// <summary>
/// Turns chosen lines of the suggested care into real recurrences in the
/// `plants` domain, so watering shows up on the dashboard next to everything
/// else and lands in the completion log like everything else.
///
/// Anchored on completion, not on the calendar: watering three days late means
/// the next one is three days later too, which is the whole difference between
/// a schedule that survives a holiday and one that just accumulates overdue
/// rows.
/// </summary>
public class CreatePlantScheduleOperation : BaseOperation<CreatePlantScheduleRequest, CreatePlantScheduleResponse>
{
    private readonly IPlantsRepository _plantsRepository;
    private readonly IRecurrencesRepository _recurrencesRepository;
    private readonly IDomainsRepository _domainsRepository;
    private readonly IRecurrenceMaterializer _materializer;
    private readonly IClock _clock;

    public CreatePlantScheduleOperation(
        ILogger<CreatePlantScheduleOperation> logger,
        IValidator<CreatePlantScheduleRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IPlantsRepository plantsRepository,
        IRecurrencesRepository recurrencesRepository,
        IDomainsRepository domainsRepository,
        IRecurrenceMaterializer materializer,
        IClock clock) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _plantsRepository = plantsRepository;
        _recurrencesRepository = recurrencesRepository;
        _domainsRepository = domainsRepository;
        _materializer = materializer;
        _clock = clock;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<CreatePlantScheduleResponse> Execute(CreatePlantScheduleRequest request, IUnitOfWork unitOfWork)
    {
        Plant plant = await _plantsRepository.GetByIdAsync(request.PlantId, unitOfWork)
                      ?? throw new NotFoundException($"No plant with id {request.PlantId}", ExceptionCodes.PLANT_NOT_FOUND);

        TaskDomain domain = await _domainsRepository.GetByCodeAsync(DomainCodes.Plants, unitOfWork)
                            ?? throw new NotFoundException($"No '{DomainCodes.Plants}' domain to file the care under.", ExceptionCodes.DOMAIN_NOT_FOUND);

        List<RecurrenceListItemDto> existing =
            await _plantsRepository.GetCareRecurrencesAsync([plant.Id], unitOfWork);

        // A tablet turns one press into two often enough that this is worth
        // guarding: the same care line for the same plant is the same schedule,
        // and the response carries the schedule back so nothing is hidden by it.
        var scheduled = existing
            .Select(recurrence => PlantMapper.CareTitleOf(recurrence.Data))
            .Where(title => title is not null)
            .ToHashSet(StringComparer.OrdinalIgnoreCase)!;

        int materialized = 0;

        foreach (PlantCareTask care in request.Tasks)
        {
            if (!scheduled.Add(care.Title))
            {
                continue;
            }

            var recurrence = new TaskRecurrence
            {
                Id = Guid.NewGuid(),
                DomainId = domain.Id,

                // The plant's name is in the title because the dashboard mixes
                // every tracker together, and "Water" on its own says nothing
                // when five plants are on the board.
                Title = $"{care.Title} — {plant.Name}",
                Notes = string.IsNullOrWhiteSpace(care.Notes) ? null : care.Notes,
                Data = PlantMapper.PayloadFor(plant.Id, care.Title),
                RuleType = RecurrenceRuleType.IntervalDays,
                IntervalDays = care.IntervalDays,
                AnchorMode = RecurrenceAnchorMode.FromCompletion,
                StartsOn = _clock.Today,
                IsActive = true
            };

            await _recurrencesRepository.CreateAsync(recurrence, unitOfWork);

            // Materialize now rather than waiting for the hourly sweep, so the
            // first watering is on the board before the user leaves the tab.
            materialized += await _materializer.MaterializeOneAsync(recurrence, unitOfWork, CancellationToken.None);
        }

        return new CreatePlantScheduleResponse
        {
            Plant = await PlantLoader.LoadAsync(plant.Id, _plantsRepository, unitOfWork),
            MaterializedTaskCount = materialized
        };
    }
}
