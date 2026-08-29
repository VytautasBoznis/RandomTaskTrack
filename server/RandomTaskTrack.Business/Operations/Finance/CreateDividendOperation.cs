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

public class CreateDividendOperation : BaseOperation<CreateDividendRequest, CreateDividendResponse>
{
    private readonly IFinanceRepository _financeRepository;

    public CreateDividendOperation(
        ILogger<CreateDividendOperation> logger,
        IValidator<CreateDividendRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IFinanceRepository financeRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _financeRepository = financeRepository;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<CreateDividendResponse> Execute(CreateDividendRequest request, IUnitOfWork unitOfWork)
    {
        if (request.HoldingId.HasValue && await _financeRepository.GetHoldingAsync(request.HoldingId.Value, unitOfWork) is null)
        {
            throw new NotFoundException("Holding not found", ExceptionCodes.FINANCE_HOLDING_NOT_FOUND);
        }

        var dividend = new Dividend
        {
            Id = Guid.NewGuid(),
            HoldingId = request.HoldingId,
            Name = request.Name,
            Amount = request.Amount,
            Currency = await FinanceGuards.ResolveCurrencyAsync(request.Currency, _financeRepository, unitOfWork),
            Cadence = request.Cadence,
            DayOfMonth = request.DayOfMonth,
            MonthOfYear = request.MonthOfYear,
            StartsOn = request.StartsOn,
            EndsOn = request.EndsOn,
            IsActive = true
        };

        await _financeRepository.CreateDividendAsync(dividend, unitOfWork);

        return new CreateDividendResponse
        {
            Dividend = await _financeRepository.GetDividendAsync(dividend.Id, unitOfWork)
                       ?? throw new NotFoundException("Dividend not found after create", ExceptionCodes.FINANCE_DIVIDEND_NOT_FOUND)
        };
    }
}
