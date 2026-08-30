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

public class UpdateAccountOperation : BaseOperation<UpdateAccountRequest, UpdateAccountResponse>
{
    private readonly IFinanceRepository _financeRepository;

    public UpdateAccountOperation(
        ILogger<UpdateAccountOperation> logger,
        IValidator<UpdateAccountRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IFinanceRepository financeRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _financeRepository = financeRepository;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<UpdateAccountResponse> Execute(UpdateAccountRequest request, IUnitOfWork unitOfWork)
    {
        FinanceAccount account = await FinanceGuards.ResolveAccountAsync(request.Id, _financeRepository, unitOfWork);

        if (request.Name is not null)
        {
            account.Name = request.Name.Trim();
            await FinanceGuards.GuardNameFreeAsync(account.Name, account.Id, _financeRepository, unitOfWork);
        }

        // Changing the kind is allowed and moves nothing: every account carries
        // a balance and can hold shares either way, so this only changes which
        // list the account appears in.
        account.Kind = request.Kind ?? account.Kind;
        account.Note = request.Note ?? account.Note;

        // Re-denominating does not restate the entries, which keep the currency
        // they were logged in. The balance simply converts into the new one.
        if (request.Currency is not null)
        {
            account.Currency = await FinanceGuards.ResolveCurrencyAsync(request.Currency, _financeRepository, unitOfWork);
        }

        await _financeRepository.UpdateAccountAsync(account, unitOfWork);

        return new UpdateAccountResponse
        {
            Account = await _financeRepository.GetAccountAsync(account.Id, unitOfWork)
                      ?? throw new NotFoundException("Account not found after update", ExceptionCodes.FINANCE_ACCOUNT_NOT_FOUND)
        };
    }
}
