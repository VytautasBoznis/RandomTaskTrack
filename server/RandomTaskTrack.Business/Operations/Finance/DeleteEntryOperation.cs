using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Data.Request.Finance;
using RandomTaskTrack.Data.Response.Finance;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Finance;

namespace RandomTaskTrack.Business.Operations.Finance;

public class DeleteEntryOperation : BaseOperation<DeleteEntryRequest, DeleteEntryResponse>
{
    private readonly IFinanceRepository _financeRepository;

    public DeleteEntryOperation(
        ILogger<DeleteEntryOperation> logger,
        IValidator<DeleteEntryRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IFinanceRepository financeRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _financeRepository = financeRepository;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<DeleteEntryResponse> Execute(DeleteEntryRequest request, IUnitOfWork unitOfWork)
    {
        return new DeleteEntryResponse
        {
            Success = await _financeRepository.DeleteEntryAsync(request.Id, unitOfWork)
        };
    }
}
