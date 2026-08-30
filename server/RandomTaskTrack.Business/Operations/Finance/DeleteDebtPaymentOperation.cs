using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Data.Request.Finance;
using RandomTaskTrack.Data.Response.Finance;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Finance;

namespace RandomTaskTrack.Business.Operations.Finance;

public class DeleteDebtPaymentOperation : BaseOperation<DeleteDebtPaymentRequest, DeleteDebtPaymentResponse>
{
    private readonly IFinanceRepository _financeRepository;

    public DeleteDebtPaymentOperation(
        ILogger<DeleteDebtPaymentOperation> logger,
        IValidator<DeleteDebtPaymentRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IFinanceRepository financeRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _financeRepository = financeRepository;
    }

    protected override bool RequiresTransaction => true;

    /// <summary>
    /// Undoes the whole chunk: the cash goes back to the account it left and
    /// the payoff date moves out again, both because the schedule is amortised
    /// on read rather than written down anywhere.
    /// </summary>
    protected override async Task<DeleteDebtPaymentResponse> Execute(
        DeleteDebtPaymentRequest request,
        IUnitOfWork unitOfWork)
    {
        return new DeleteDebtPaymentResponse
        {
            Success = await _financeRepository.DeleteDebtPaymentAsync(request.Id, unitOfWork)
        };
    }
}
