using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Exceptions;
using RandomTaskTrack.Data.Models.Finance;
using RandomTaskTrack.Data.Request.Finance;
using RandomTaskTrack.Data.Response.Finance;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Finance;

namespace RandomTaskTrack.Business.Operations.Finance;

/// <summary>
/// Logs money that actually moved. This is what makes current cash a derived
/// number rather than one typed into a box and left to rot.
/// </summary>
public class CreateEntryOperation : BaseOperation<CreateEntryRequest, CreateEntryResponse>
{
    private readonly IFinanceRepository _financeRepository;

    public CreateEntryOperation(
        ILogger<CreateEntryOperation> logger,
        IValidator<CreateEntryRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IFinanceRepository financeRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _financeRepository = financeRepository;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<CreateEntryResponse> Execute(CreateEntryRequest request, IUnitOfWork unitOfWork)
    {
        // A flow_id pointing at nothing would be accepted by the column and
        // then quietly mean "not from a flow", so it is checked here.
        if (request.FlowId.HasValue && await _financeRepository.GetFlowAsync(request.FlowId.Value, unitOfWork) is null)
        {
            throw new NotFoundException("Flow not found", ExceptionCodes.FINANCE_FLOW_NOT_FOUND);
        }

        var entry = new LedgerEntry
        {
            Id = Guid.NewGuid(),
            FlowId = request.FlowId,
            Kind = request.Kind,
            Name = request.Name,
            Amount = request.Amount,
            Currency = await FinanceGuards.ResolveCurrencyAsync(request.Currency, _financeRepository, unitOfWork),
            OccurredOn = request.OccurredOn,
            Category = request.Category,
            Note = request.Note
        };

        await _financeRepository.CreateEntryAsync(entry, unitOfWork);

        return new CreateEntryResponse
        {
            Entry = await _financeRepository.GetEntryAsync(entry.Id, unitOfWork)
                    ?? throw new NotFoundException("Entry not found after create", ExceptionCodes.FINANCE_ENTRY_NOT_FOUND)
        };
    }
}
