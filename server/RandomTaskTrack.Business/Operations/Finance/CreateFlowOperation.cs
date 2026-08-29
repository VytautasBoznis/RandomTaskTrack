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

public class CreateFlowOperation : BaseOperation<CreateFlowRequest, CreateFlowResponse>
{
    private readonly IFinanceRepository _financeRepository;

    public CreateFlowOperation(
        ILogger<CreateFlowOperation> logger,
        IValidator<CreateFlowRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IFinanceRepository financeRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _financeRepository = financeRepository;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<CreateFlowResponse> Execute(CreateFlowRequest request, IUnitOfWork unitOfWork)
    {
        var flow = new FinanceFlow
        {
            Id = Guid.NewGuid(),
            Kind = request.Kind,
            Name = request.Name,
            Amount = request.Amount,
            Currency = await FinanceGuards.ResolveCurrencyAsync(request.Currency, _financeRepository, unitOfWork),
            Cadence = request.Cadence,
            DayOfMonth = request.DayOfMonth,
            MonthOfYear = request.MonthOfYear,
            StartsOn = request.StartsOn,
            EndsOn = request.EndsOn,
            Category = request.Category,
            IsActive = true
        };

        await _financeRepository.CreateFlowAsync(flow, unitOfWork);

        return new CreateFlowResponse
        {
            Flow = await _financeRepository.GetFlowAsync(flow.Id, unitOfWork)
                   ?? throw new NotFoundException("Flow not found after create", ExceptionCodes.FINANCE_FLOW_NOT_FOUND)
        };
    }
}
