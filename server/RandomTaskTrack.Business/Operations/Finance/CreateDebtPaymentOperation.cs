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

public class CreateDebtPaymentOperation : BaseOperation<CreateDebtPaymentRequest, CreateDebtPaymentResponse>
{
    private readonly IFinanceRepository _financeRepository;

    public CreateDebtPaymentOperation(
        ILogger<CreateDebtPaymentOperation> logger,
        IValidator<CreateDebtPaymentRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IFinanceRepository financeRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _financeRepository = financeRepository;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<CreateDebtPaymentResponse> Execute(
        CreateDebtPaymentRequest request,
        IUnitOfWork unitOfWork)
    {
        Debt debt = await _financeRepository.GetDebtAsync(request.DebtId, unitOfWork)
                    ?? throw new NotFoundException("Debt not found", ExceptionCodes.FINANCE_DEBT_NOT_FOUND);

        if (request.PaidOn < debt.StartsOn)
        {
            throw new BadRequestException(
                $"That is before {debt.Name} starts on {debt.StartsOn:yyyy-MM-dd}.",
                ExceptionCodes.FINANCE_DEBT_IMPOSSIBLE,
                "You cannot pay off a debt you have not taken out yet.");
        }

        if (request.AccountId.HasValue)
        {
            await FinanceGuards.ResolveAccountAsync(request.AccountId.Value, _financeRepository, unitOfWork);
        }

        // Not capped at the outstanding balance. Amortise clamps the balance at
        // zero anyway, and refusing an overshoot here would mean recomputing the
        // whole schedule to say "you can only pay 4,182.19 more" — a number that
        // changes the moment any other chunk is edited.
        var payment = new DebtPayment
        {
            Id = Guid.NewGuid(),
            DebtId = debt.Id,
            Amount = request.Amount,
            PaidOn = request.PaidOn,
            AccountId = request.AccountId,
            Note = request.Note
        };

        await _financeRepository.CreateDebtPaymentAsync(payment, unitOfWork);

        return new CreateDebtPaymentResponse { Payment = payment };
    }
}
