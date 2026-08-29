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

public class CreateTargetOperation : BaseOperation<CreateTargetRequest, CreateTargetResponse>
{
    private readonly IFinanceRepository _financeRepository;

    public CreateTargetOperation(
        ILogger<CreateTargetOperation> logger,
        IValidator<CreateTargetRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IFinanceRepository financeRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _financeRepository = financeRepository;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<CreateTargetResponse> Execute(CreateTargetRequest request, IUnitOfWork unitOfWork)
    {
        var target = new FinanceTarget
        {
            Id = Guid.NewGuid(),
            Label = request.Label,
            TargetOn = request.TargetOn,
            Amount = request.Amount,
            Note = request.Note
        };

        await _financeRepository.CreateTargetAsync(target, unitOfWork);

        return new CreateTargetResponse
        {
            Target = await _financeRepository.GetTargetAsync(target.Id, unitOfWork)
                     ?? throw new NotFoundException("Target not found after create", ExceptionCodes.FINANCE_TARGET_NOT_FOUND)
        };
    }
}
