using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Data.Request.Finance;
using RandomTaskTrack.Data.Response.Finance;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Finance;

namespace RandomTaskTrack.Business.Operations.Finance;

public class DeleteDebtOperation : BaseOperation<DeleteDebtRequest, DeleteDebtResponse>
{
    private readonly IFinanceRepository _financeRepository;

    public DeleteDebtOperation(
        ILogger<DeleteDebtOperation> logger,
        IValidator<DeleteDebtRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IFinanceRepository financeRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _financeRepository = financeRepository;
    }

    protected override bool RequiresTransaction => true;

    /// <summary>
    /// The lump sums go with it, by the cascade on fin_debt_payments. They are
    /// events under this debt's terms and mean nothing without them.
    /// </summary>
    protected override async Task<DeleteDebtResponse> Execute(DeleteDebtRequest request, IUnitOfWork unitOfWork)
    {
        return new DeleteDebtResponse
        {
            Success = await _financeRepository.DeleteDebtAsync(request.Id, unitOfWork)
        };
    }
}
