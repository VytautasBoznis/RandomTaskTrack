using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Models.Exceptions;
using RandomTaskTrack.Data.Models.Finance;
using RandomTaskTrack.Data.Request.Finance;
using RandomTaskTrack.Data.Response.Finance;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Finance;

namespace RandomTaskTrack.Business.Operations.Finance;

public class CreateDepositOperation : BaseOperation<CreateDepositRequest, CreateDepositResponse>
{
    private readonly IFinanceRepository _financeRepository;

    public CreateDepositOperation(
        ILogger<CreateDepositOperation> logger,
        IValidator<CreateDepositRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IFinanceRepository financeRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _financeRepository = financeRepository;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<CreateDepositResponse> Execute(CreateDepositRequest request, IUnitOfWork unitOfWork)
    {
        var deposit = new Deposit
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Principal = request.Principal,
            Currency = await FinanceGuards.ResolveCurrencyAsync(request.Currency, _financeRepository, unitOfWork),
            AnnualRate = request.AnnualRate,

            // Annual is what a term deposit is quoted at unless it says
            // otherwise, so it is the default rather than a required choice.
            Compounding = request.Compounding ?? DepositCompounding.Annual,
            OpenedOn = request.OpenedOn,
            MaturesOn = request.MaturesOn,
            Note = request.Note
        };

        await _financeRepository.CreateDepositAsync(deposit, unitOfWork);

        return new CreateDepositResponse
        {
            Deposit = await _financeRepository.GetDepositAsync(deposit.Id, unitOfWork)
                      ?? throw new NotFoundException("Deposit not found after create", ExceptionCodes.FINANCE_DEPOSIT_NOT_FOUND)
        };
    }
}
