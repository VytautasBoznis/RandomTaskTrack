using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Data.Dtos.Finance;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Models.Exceptions;
using RandomTaskTrack.Data.Models.Finance;
using RandomTaskTrack.Data.Request.Finance;
using RandomTaskTrack.Data.Response.Finance;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Finance;
using RandomTaskTrack.Interfaces.Services;

namespace RandomTaskTrack.Business.Operations.Finance;

/// <summary>
/// "The bank says 4,180." Types the number you can see and writes the
/// difference as one adjustment entry, rather than storing the number itself.
///
/// That is the whole point: a stored balance is the one figure in the scope
/// that could disagree with the ledger under it, and it would start disagreeing
/// the first time an old entry was corrected. An adjustment keeps the total
/// derived, dates the correction, and leaves it visible in the ledger where a
/// run of them says the entries are being missed.
/// </summary>
public class SetAccountBalanceOperation : BaseOperation<SetAccountBalanceRequest, SetAccountBalanceResponse>
{
    private readonly IFinanceRepository _financeRepository;
    private readonly IFinanceProjector _projector;
    private readonly IClock _clock;

    public SetAccountBalanceOperation(
        ILogger<SetAccountBalanceOperation> logger,
        IValidator<SetAccountBalanceRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IFinanceRepository financeRepository,
        IFinanceProjector projector,
        IClock clock) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _financeRepository = financeRepository;
        _projector = projector;
        _clock = clock;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<SetAccountBalanceResponse> Execute(SetAccountBalanceRequest request, IUnitOfWork unitOfWork)
    {
        FinanceAccount account = await FinanceGuards.ResolveAccountAsync(request.Id, _financeRepository, unitOfWork);

        // Asking the projector rather than re-deriving the balance here. The
        // formula has deposits in it as well as entries, and a second copy of
        // it would only be a second chance to get it wrong.
        FinanceOverviewDto overview = await _projector.BuildOverviewAsync(unitOfWork);

        AccountDto current = overview.Accounts.FirstOrDefault(a => a.Id == account.Id)
                             ?? throw new NotFoundException("Account not found", ExceptionCodes.FINANCE_ACCOUNT_NOT_FOUND);

        decimal difference = request.Balance - current.Balance;

        // Already right. An entry for nothing would just be noise in the ledger.
        if (difference == 0)
        {
            return new SetAccountBalanceResponse();
        }

        var entry = new LedgerEntry
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            Kind = difference > 0 ? FinanceFlowKind.Income : FinanceFlowKind.Expense,
            Name = "Balance adjustment",
            Amount = Math.Abs(difference),
            Currency = account.Currency,
            OccurredOn = request.OccurredOn ?? _clock.Today,
            Note = request.Note
        };

        await _financeRepository.CreateEntryAsync(entry, unitOfWork);

        _logger.LogInformation(
            "Adjusted {Account} by {Difference} {Currency} to reach {Balance}",
            account.Name, difference, account.Currency, request.Balance);

        return new SetAccountBalanceResponse
        {
            Entry = await _financeRepository.GetEntryAsync(entry.Id, unitOfWork)
                    ?? throw new NotFoundException("Entry not found after create", ExceptionCodes.FINANCE_ENTRY_NOT_FOUND)
        };
    }
}
