using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Data.Request.Finance;
using RandomTaskTrack.Data.Response.Finance;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Finance;

namespace RandomTaskTrack.Business.Operations.Finance;

public class DeleteTargetOperation : BaseOperation<DeleteTargetRequest, DeleteTargetResponse>
{
    private readonly IFinanceRepository _financeRepository;

    public DeleteTargetOperation(
        ILogger<DeleteTargetOperation> logger,
        IValidator<DeleteTargetRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IFinanceRepository financeRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _financeRepository = financeRepository;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<DeleteTargetResponse> Execute(DeleteTargetRequest request, IUnitOfWork unitOfWork)
    {
        return new DeleteTargetResponse
        {
            Success = await _financeRepository.DeleteTargetAsync(request.Id, unitOfWork)
        };
    }
}
