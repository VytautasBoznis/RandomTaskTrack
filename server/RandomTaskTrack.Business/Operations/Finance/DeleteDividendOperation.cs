using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Data.Request.Finance;
using RandomTaskTrack.Data.Response.Finance;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Finance;

namespace RandomTaskTrack.Business.Operations.Finance;

public class DeleteDividendOperation : BaseOperation<DeleteDividendRequest, DeleteDividendResponse>
{
    private readonly IFinanceRepository _financeRepository;

    public DeleteDividendOperation(
        ILogger<DeleteDividendOperation> logger,
        IValidator<DeleteDividendRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IFinanceRepository financeRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _financeRepository = financeRepository;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<DeleteDividendResponse> Execute(DeleteDividendRequest request, IUnitOfWork unitOfWork)
    {
        return new DeleteDividendResponse
        {
            Success = await _financeRepository.DeleteDividendAsync(request.Id, unitOfWork)
        };
    }
}
