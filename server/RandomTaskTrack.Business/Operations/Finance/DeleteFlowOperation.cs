using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Data.Request.Finance;
using RandomTaskTrack.Data.Response.Finance;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Finance;

namespace RandomTaskTrack.Business.Operations.Finance;

/// <summary>
/// Ledger entries that referenced this flow keep their row and lose the link —
/// fin_entries.flow_id is ON DELETE SET NULL. Deleting the definition of a rent
/// payment must not erase the record that the rent was paid.
/// </summary>
public class DeleteFlowOperation : BaseOperation<DeleteFlowRequest, DeleteFlowResponse>
{
    private readonly IFinanceRepository _financeRepository;

    public DeleteFlowOperation(
        ILogger<DeleteFlowOperation> logger,
        IValidator<DeleteFlowRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IFinanceRepository financeRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _financeRepository = financeRepository;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<DeleteFlowResponse> Execute(DeleteFlowRequest request, IUnitOfWork unitOfWork)
    {
        return new DeleteFlowResponse
        {
            Success = await _financeRepository.DeleteFlowAsync(request.Id, unitOfWork)
        };
    }
}
