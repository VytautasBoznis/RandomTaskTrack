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

/// <summary>
/// A new pot. It starts at zero rather than taking an opening balance: what is
/// in it is the ledger's answer, and "Set balance" is how the first number gets
/// there — as an entry, like every other movement.
/// </summary>
public class CreateAccountOperation : BaseOperation<CreateAccountRequest, CreateAccountResponse>
{
    private readonly IFinanceRepository _financeRepository;

    public CreateAccountOperation(
        ILogger<CreateAccountOperation> logger,
        IValidator<CreateAccountRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IFinanceRepository financeRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _financeRepository = financeRepository;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<CreateAccountResponse> Execute(CreateAccountRequest request, IUnitOfWork unitOfWork)
    {
        string name = request.Name.Trim();

        await FinanceGuards.GuardNameFreeAsync(name, null, _financeRepository, unitOfWork);

        var account = new FinanceAccount
        {
            Id = Guid.NewGuid(),
            Name = name,
            Kind = request.Kind,
            Currency = await FinanceGuards.ResolveCurrencyAsync(request.Currency, _financeRepository, unitOfWork),
            Note = request.Note
        };

        await _financeRepository.CreateAccountAsync(account, unitOfWork);

        return new CreateAccountResponse
        {
            Account = await _financeRepository.GetAccountAsync(account.Id, unitOfWork)
                      ?? throw new NotFoundException("Account not found after create", ExceptionCodes.FINANCE_ACCOUNT_NOT_FOUND)
        };
    }
}
