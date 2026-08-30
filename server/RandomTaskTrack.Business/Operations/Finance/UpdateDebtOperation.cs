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

public class UpdateDebtOperation : BaseOperation<UpdateDebtRequest, UpdateDebtResponse>
{
    private readonly IFinanceRepository _financeRepository;

    public UpdateDebtOperation(
        ILogger<UpdateDebtOperation> logger,
        IValidator<UpdateDebtRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IFinanceRepository financeRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _financeRepository = financeRepository;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<UpdateDebtResponse> Execute(UpdateDebtRequest request, IUnitOfWork unitOfWork)
    {
        Debt debt = await _financeRepository.GetDebtAsync(request.Id, unitOfWork)
                    ?? throw new NotFoundException("Debt not found", ExceptionCodes.FINANCE_DEBT_NOT_FOUND);

        debt.Name = request.Name ?? debt.Name;
        debt.Principal = request.Principal ?? debt.Principal;
        debt.AnnualRate = request.AnnualRate ?? debt.AnnualRate;
        debt.Payment = request.Payment ?? debt.Payment;
        debt.StartsOn = request.StartsOn ?? debt.StartsOn;
        debt.EndsOn = request.EndsOn ?? debt.EndsOn;
        debt.AssetValue = request.AssetValue ?? debt.AssetValue;
        debt.DownPayment = request.DownPayment ?? debt.DownPayment;
        debt.DownPaymentAccountId = request.DownPaymentAccountId ?? debt.DownPaymentAccountId;
        debt.DisbursesToAccountId = request.DisbursesToAccountId ?? debt.DisbursesToAccountId;
        debt.Note = request.Note ?? debt.Note;

        if (request.Currency is not null)
        {
            debt.Currency = await FinanceGuards.ResolveCurrencyAsync(request.Currency, _financeRepository, unitOfWork);
        }

        await FinanceGuards.GuardDebtAsync(debt, _financeRepository, unitOfWork);
        await _financeRepository.UpdateDebtAsync(debt, unitOfWork);

        return new UpdateDebtResponse
        {
            Debt = await _financeRepository.GetDebtAsync(debt.Id, unitOfWork)
                   ?? throw new NotFoundException("Debt not found after update", ExceptionCodes.FINANCE_DEBT_NOT_FOUND)
        };
    }
}
