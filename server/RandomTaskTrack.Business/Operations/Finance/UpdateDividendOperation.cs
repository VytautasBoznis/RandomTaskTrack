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

public class UpdateDividendOperation : BaseOperation<UpdateDividendRequest, UpdateDividendResponse>
{
    private readonly IFinanceRepository _financeRepository;

    public UpdateDividendOperation(
        ILogger<UpdateDividendOperation> logger,
        IValidator<UpdateDividendRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IFinanceRepository financeRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _financeRepository = financeRepository;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<UpdateDividendResponse> Execute(UpdateDividendRequest request, IUnitOfWork unitOfWork)
    {
        Dividend dividend = await _financeRepository.GetDividendAsync(request.Id, unitOfWork)
                            ?? throw new NotFoundException("Dividend not found", ExceptionCodes.FINANCE_DIVIDEND_NOT_FOUND);

        dividend.Name = request.Name ?? dividend.Name;
        dividend.Amount = request.Amount ?? dividend.Amount;
        dividend.Cadence = request.Cadence ?? dividend.Cadence;
        dividend.DayOfMonth = request.DayOfMonth ?? dividend.DayOfMonth;
        dividend.MonthOfYear = request.MonthOfYear ?? dividend.MonthOfYear;
        dividend.StartsOn = request.StartsOn ?? dividend.StartsOn;
        dividend.EndsOn = request.EndsOn ?? dividend.EndsOn;
        dividend.IsActive = request.IsActive ?? dividend.IsActive;

        if (request.Currency is not null)
        {
            dividend.Currency = await FinanceGuards.ResolveCurrencyAsync(request.Currency, _financeRepository, unitOfWork);
        }

        await _financeRepository.UpdateDividendAsync(dividend, unitOfWork);

        return new UpdateDividendResponse
        {
            Dividend = await _financeRepository.GetDividendAsync(dividend.Id, unitOfWork)
                       ?? throw new NotFoundException("Dividend not found after update", ExceptionCodes.FINANCE_DIVIDEND_NOT_FOUND)
        };
    }
}
