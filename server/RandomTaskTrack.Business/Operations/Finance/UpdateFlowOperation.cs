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
/// Partial patch: anything the request leaves null keeps its current value,
/// which is what lets the UI pause a flow without resending the schedule.
/// </summary>
public class UpdateFlowOperation : BaseOperation<UpdateFlowRequest, UpdateFlowResponse>
{
    private readonly IFinanceRepository _financeRepository;

    public UpdateFlowOperation(
        ILogger<UpdateFlowOperation> logger,
        IValidator<UpdateFlowRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IFinanceRepository financeRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _financeRepository = financeRepository;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<UpdateFlowResponse> Execute(UpdateFlowRequest request, IUnitOfWork unitOfWork)
    {
        FinanceFlow flow = await _financeRepository.GetFlowAsync(request.Id, unitOfWork)
                           ?? throw new NotFoundException("Flow not found", ExceptionCodes.FINANCE_FLOW_NOT_FOUND);

        flow.Name = request.Name ?? flow.Name;
        flow.Amount = request.Amount ?? flow.Amount;
        flow.Cadence = request.Cadence ?? flow.Cadence;
        flow.DayOfMonth = request.DayOfMonth ?? flow.DayOfMonth;
        flow.MonthOfYear = request.MonthOfYear ?? flow.MonthOfYear;
        flow.StartsOn = request.StartsOn ?? flow.StartsOn;
        flow.EndsOn = request.EndsOn ?? flow.EndsOn;
        flow.Category = request.Category ?? flow.Category;
        flow.IsActive = request.IsActive ?? flow.IsActive;

        if (request.Currency is not null)
        {
            flow.Currency = await FinanceGuards.ResolveCurrencyAsync(request.Currency, _financeRepository, unitOfWork);
        }

        await _financeRepository.UpdateFlowAsync(flow, unitOfWork);

        return new UpdateFlowResponse
        {
            Flow = await _financeRepository.GetFlowAsync(flow.Id, unitOfWork)
                   ?? throw new NotFoundException("Flow not found after update", ExceptionCodes.FINANCE_FLOW_NOT_FOUND)
        };
    }
}
