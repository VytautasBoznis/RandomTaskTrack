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

public class UpdateTargetOperation : BaseOperation<UpdateTargetRequest, UpdateTargetResponse>
{
    private readonly IFinanceRepository _financeRepository;

    public UpdateTargetOperation(
        ILogger<UpdateTargetOperation> logger,
        IValidator<UpdateTargetRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IFinanceRepository financeRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _financeRepository = financeRepository;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<UpdateTargetResponse> Execute(UpdateTargetRequest request, IUnitOfWork unitOfWork)
    {
        FinanceTarget target = await _financeRepository.GetTargetAsync(request.Id, unitOfWork)
                               ?? throw new NotFoundException("Target not found", ExceptionCodes.FINANCE_TARGET_NOT_FOUND);

        target.Label = request.Label ?? target.Label;
        target.TargetOn = request.TargetOn ?? target.TargetOn;
        target.Amount = request.Amount ?? target.Amount;
        target.Note = request.Note ?? target.Note;

        await _financeRepository.UpdateTargetAsync(target, unitOfWork);

        return new UpdateTargetResponse
        {
            Target = await _financeRepository.GetTargetAsync(target.Id, unitOfWork)
                     ?? throw new NotFoundException("Target not found after update", ExceptionCodes.FINANCE_TARGET_NOT_FOUND)
        };
    }
}
