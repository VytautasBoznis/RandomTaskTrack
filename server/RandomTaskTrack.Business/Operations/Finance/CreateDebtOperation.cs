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

public class CreateDebtOperation : BaseOperation<CreateDebtRequest, CreateDebtResponse>
{
    private readonly IFinanceRepository _financeRepository;

    public CreateDebtOperation(
        ILogger<CreateDebtOperation> logger,
        IValidator<CreateDebtRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IFinanceRepository financeRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _financeRepository = financeRepository;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<CreateDebtResponse> Execute(CreateDebtRequest request, IUnitOfWork unitOfWork)
    {
        var debt = new Debt
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Principal = request.Principal,
            Currency = await FinanceGuards.ResolveCurrencyAsync(request.Currency, _financeRepository, unitOfWork),
            AnnualRate = request.AnnualRate,
            Payment = request.Payment,
            StartsOn = request.StartsOn,
            EndsOn = request.EndsOn,
            AssetValue = request.AssetValue,
            DownPayment = request.DownPayment,
            DownPaymentAccountId = request.DownPaymentAccountId,
            DisbursesToAccountId = request.DisbursesToAccountId,
            Note = request.Note
        };

        await FinanceGuards.GuardDebtAsync(debt, _financeRepository, unitOfWork);
        await _financeRepository.CreateDebtAsync(debt, unitOfWork);

        return new CreateDebtResponse
        {
            Debt = await _financeRepository.GetDebtAsync(debt.Id, unitOfWork)
                   ?? throw new NotFoundException("Debt not found after create", ExceptionCodes.FINANCE_DEBT_NOT_FOUND)
        };
    }
}
