using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Exceptions;
using RandomTaskTrack.Data.Models.Tasks;
using RandomTaskTrack.Data.Request.Recurrences;
using RandomTaskTrack.Data.Response.Recurrences;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Domains;
using RandomTaskTrack.Interfaces.Repositories.Recurrences;
using RandomTaskTrack.Interfaces.Services;

namespace RandomTaskTrack.Business.Operations.Recurrences;

public class CreateRecurrenceOperation : BaseOperation<CreateRecurrenceRequest, CreateRecurrenceResponse>
{
    private readonly IRecurrencesRepository _recurrencesRepository;
    private readonly IDomainsRepository _domainsRepository;
    private readonly IRecurrenceMaterializer _materializer;
    private readonly IClock _clock;

    public CreateRecurrenceOperation(
        ILogger<CreateRecurrenceOperation> logger,
        IValidator<CreateRecurrenceRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IRecurrencesRepository recurrencesRepository,
        IDomainsRepository domainsRepository,
        IRecurrenceMaterializer materializer,
        IClock clock) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _recurrencesRepository = recurrencesRepository;
        _domainsRepository = domainsRepository;
        _materializer = materializer;
        _clock = clock;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<CreateRecurrenceResponse> Execute(CreateRecurrenceRequest request, IUnitOfWork unitOfWork)
    {
        _ = await _domainsRepository.GetByIdAsync(request.DomainId, unitOfWork)
            ?? throw new NotFoundException($"No domain with id {request.DomainId}", ExceptionCodes.DOMAIN_NOT_FOUND);

        var recurrence = new TaskRecurrence
        {
            Id = Guid.NewGuid(),
            DomainId = request.DomainId,
            Title = request.Title,
            Notes = request.Notes,
            Data = string.IsNullOrWhiteSpace(request.Data) ? "{}" : request.Data,
            RuleType = request.RuleType,
            IntervalDays = request.IntervalDays,
            DaysOfWeek = request.DaysOfWeek,
            DayOfMonth = request.DayOfMonth,
            AnchorMode = request.AnchorMode,
            TimeOfDay = request.TimeOfDay,
            StartsOn = request.StartsOn ?? _clock.Today,
            EndsOn = request.EndsOn,
            IsActive = true
        };

        await _recurrencesRepository.CreateAsync(recurrence, unitOfWork);

        // Materialize immediately rather than waiting for the background sweep,
        // so the tasks show up on the dashboard the moment they are created.
        int materialized = await _materializer.MaterializeOneAsync(recurrence, unitOfWork, CancellationToken.None);

        return new CreateRecurrenceResponse
        {
            Recurrence = await _recurrencesRepository.GetByIdAsync(recurrence.Id, unitOfWork)
                         ?? throw new NotFoundException("Recurrence not found after create", ExceptionCodes.RECURRENCE_NOT_FOUND),
            MaterializedTaskCount = materialized
        };
    }
}
