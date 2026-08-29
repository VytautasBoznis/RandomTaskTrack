using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Data.Request.Finance;
using RandomTaskTrack.Data.Response.Finance;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Finance;

namespace RandomTaskTrack.Business.Operations.Finance;

public class DeleteDepositOperation : BaseOperation<DeleteDepositRequest, DeleteDepositResponse>
{
    private readonly IFinanceRepository _financeRepository;

    public DeleteDepositOperation(
        ILogger<DeleteDepositOperation> logger,
        IValidator<DeleteDepositRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IFinanceRepository financeRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _financeRepository = financeRepository;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<DeleteDepositResponse> Execute(DeleteDepositRequest request, IUnitOfWork unitOfWork)
    {
        return new DeleteDepositResponse
        {
            Success = await _financeRepository.DeleteDepositAsync(request.Id, unitOfWork)
        };
    }
}
