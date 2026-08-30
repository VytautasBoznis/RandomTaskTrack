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
/// Only an empty account can go. Deleting one with history would either take
/// the ledger with it or leave entries pointing nowhere, and both are worse
/// than being told to clear it out first — an account is a container, not a
/// record of anything itself.
/// </summary>
public class DeleteAccountOperation : BaseOperation<DeleteAccountRequest, DeleteAccountResponse>
{
    private readonly IFinanceRepository _financeRepository;

    public DeleteAccountOperation(
        ILogger<DeleteAccountOperation> logger,
        IValidator<DeleteAccountRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IFinanceRepository financeRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _financeRepository = financeRepository;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<DeleteAccountResponse> Execute(DeleteAccountRequest request, IUnitOfWork unitOfWork)
    {
        FinanceAccount account = await FinanceGuards.ResolveAccountAsync(request.Id, _financeRepository, unitOfWork);

        int uses = await _financeRepository.CountAccountUsesAsync(account.Id, unitOfWork);

        if (uses > 0)
        {
            throw new BadRequestException(
                $"'{account.Name}' still has {uses} entries, holdings or deposits attached.",
                ExceptionCodes.FINANCE_ACCOUNT_IN_USE,
                "Move or delete them first.");
        }

        return new DeleteAccountResponse
        {
            Success = await _financeRepository.DeleteAccountAsync(account.Id, unitOfWork)
        };
    }
}
