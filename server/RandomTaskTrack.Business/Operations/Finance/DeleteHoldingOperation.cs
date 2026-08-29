using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Data.Request.Finance;
using RandomTaskTrack.Data.Response.Finance;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Finance;

namespace RandomTaskTrack.Business.Operations.Finance;

/// <summary>
/// Cascades to the trades and dividends hanging off the holding — the schema
/// says ON DELETE CASCADE for both. Destructive and irreversible: the trade
/// history goes with it.
/// </summary>
public class DeleteHoldingOperation : BaseOperation<DeleteHoldingRequest, DeleteHoldingResponse>
{
    private readonly IFinanceRepository _financeRepository;

    public DeleteHoldingOperation(
        ILogger<DeleteHoldingOperation> logger,
        IValidator<DeleteHoldingRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IFinanceRepository financeRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _financeRepository = financeRepository;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<DeleteHoldingResponse> Execute(DeleteHoldingRequest request, IUnitOfWork unitOfWork)
    {
        return new DeleteHoldingResponse
        {
            Success = await _financeRepository.DeleteHoldingAsync(request.Id, unitOfWork)
        };
    }
}
