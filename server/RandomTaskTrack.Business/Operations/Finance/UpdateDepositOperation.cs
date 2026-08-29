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

public class UpdateDepositOperation : BaseOperation<UpdateDepositRequest, UpdateDepositResponse>
{
    private readonly IFinanceRepository _financeRepository;

    public UpdateDepositOperation(
        ILogger<UpdateDepositOperation> logger,
        IValidator<UpdateDepositRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IFinanceRepository financeRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _financeRepository = financeRepository;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<UpdateDepositResponse> Execute(UpdateDepositRequest request, IUnitOfWork unitOfWork)
    {
        Deposit deposit = await _financeRepository.GetDepositAsync(request.Id, unitOfWork)
                          ?? throw new NotFoundException("Deposit not found", ExceptionCodes.FINANCE_DEPOSIT_NOT_FOUND);

        deposit.Name = request.Name ?? deposit.Name;
        deposit.Principal = request.Principal ?? deposit.Principal;
        deposit.AnnualRate = request.AnnualRate ?? deposit.AnnualRate;
        deposit.Compounding = request.Compounding ?? deposit.Compounding;
        deposit.OpenedOn = request.OpenedOn ?? deposit.OpenedOn;
        deposit.MaturesOn = request.MaturesOn ?? deposit.MaturesOn;
        deposit.Note = request.Note ?? deposit.Note;

        if (request.Currency is not null)
        {
            deposit.Currency = await FinanceGuards.ResolveCurrencyAsync(request.Currency, _financeRepository, unitOfWork);
        }

        await _financeRepository.UpdateDepositAsync(deposit, unitOfWork);

        return new UpdateDepositResponse
        {
            Deposit = await _financeRepository.GetDepositAsync(deposit.Id, unitOfWork)
                      ?? throw new NotFoundException("Deposit not found after update", ExceptionCodes.FINANCE_DEPOSIT_NOT_FOUND)
        };
    }
}
