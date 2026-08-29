using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Data.Request.Finance;
using RandomTaskTrack.Data.Response.Finance;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Finance;

namespace RandomTaskTrack.Business.Operations.Finance;

public class DeleteTradeOperation : BaseOperation<DeleteTradeRequest, DeleteTradeResponse>
{
    private readonly IFinanceRepository _financeRepository;

    public DeleteTradeOperation(
        ILogger<DeleteTradeOperation> logger,
        IValidator<DeleteTradeRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IFinanceRepository financeRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _financeRepository = financeRepository;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<DeleteTradeResponse> Execute(DeleteTradeRequest request, IUnitOfWork unitOfWork)
    {
        return new DeleteTradeResponse
        {
            Success = await _financeRepository.DeleteTradeAsync(request.Id, unitOfWork)
        };
    }
}
