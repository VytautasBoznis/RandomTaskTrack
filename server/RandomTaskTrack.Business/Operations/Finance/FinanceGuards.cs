using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Exceptions;
using RandomTaskTrack.Data.Models.Finance;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Finance;

namespace RandomTaskTrack.Business.Operations.Finance;

internal static class FinanceGuards
{
    /// <summary>
    /// Resolves a currency code to the exact casing stored in
    /// tracker.fin_currencies. Every money table has a foreign key to that
    /// column and the column is plain text, so "usd" would be rejected by the
    /// database with a constraint name rather than by us with a sentence —
    /// and a lowercase code from a chat tool call is not a mistake worth
    /// failing over.
    /// </summary>
    public static async Task<string> ResolveCurrencyAsync(
        string code,
        IFinanceRepository repository,
        IUnitOfWork unitOfWork)
    {
        Currency? currency = await repository.GetCurrencyAsync(code, unitOfWork);

        if (currency is null)
        {
            throw new BadRequestException(
                $"Unknown currency '{code}'.",
                ExceptionCodes.FINANCE_CURRENCY_NOT_FOUND,
                "Add it to tracker.fin_currencies with a rate first.");
        }

        return currency.Code;
    }

    /// <summary>
    /// An account id that points at nothing would be refused by the foreign key
    /// as a constraint name. Every entry, holding and deposit names an account,
    /// so this is the one check they all share.
    /// </summary>
    public static async Task<FinanceAccount> ResolveAccountAsync(
        Guid id,
        IFinanceRepository repository,
        IUnitOfWork unitOfWork)
    {
        return await repository.GetAccountAsync(id, unitOfWork)
               ?? throw new NotFoundException("Account not found", ExceptionCodes.FINANCE_ACCOUNT_NOT_FOUND);
    }

    /// <summary>
    /// The two ends of a deposit, checked and defaulted together.
    ///
    /// The target falls back to the source because money that leaves an account
    /// with nowhere to come back to would simply disappear from the balances on
    /// the maturity date. A target without a source is refused for the mirror
    /// reason: it would credit interest on a principal that never left.
    ///
    /// Both null is the legacy shape and stays legal — a deposit whose transfer
    /// was logged by hand before accounts existed moves nothing by itself.
    /// </summary>
    public static async Task<(Guid? Source, Guid? Target)> ResolveDepositAccountsAsync(
        Guid? sourceId,
        Guid? targetId,
        IFinanceRepository repository,
        IUnitOfWork unitOfWork)
    {
        if (sourceId is null && targetId is not null)
        {
            throw new BadRequestException(
                "A deposit that lands somewhere has to come from somewhere.",
                ExceptionCodes.FINANCE_ACCOUNT_NOT_FOUND,
                "Pick the account the principal comes out of.");
        }

        if (sourceId is null)
        {
            return (null, null);
        }

        FinanceAccount source = await ResolveAccountAsync(sourceId.Value, repository, unitOfWork);
        FinanceAccount target = targetId is null
            ? source
            : await ResolveAccountAsync(targetId.Value, repository, unitOfWork);

        return (source.Id, target.Id);
    }

    /// <summary>
    /// The two ends of a debt, checked together, and the one arithmetic rule it
    /// has to satisfy.
    ///
    /// A payment at or below the first month's interest never touches the
    /// principal: the balance grows every month, the schedule runs to its
    /// 50-year cap, and the projection shows a debt that is never paid off. It
    /// is a typo — a decimal point, a yearly figure typed as a monthly one —
    /// and catching it here is the difference between a sentence saying so and
    /// a chart that is quietly nonsense for the next thirty years.
    ///
    /// This runs on the merged debt rather than on the request, because on
    /// update the principal can come from the request and the rate from the
    /// stored row, and neither half means anything without the other.
    /// </summary>
    public static async Task GuardDebtAsync(
        Debt debt,
        IFinanceRepository repository,
        IUnitOfWork unitOfWork)
    {
        if (debt.EndsOn.HasValue && debt.EndsOn.Value < debt.StartsOn)
        {
            throw new BadRequestException(
                "A debt cannot finish before it starts.",
                ExceptionCodes.FINANCE_DEBT_IMPOSSIBLE,
                "Check the start and end dates.");
        }

        decimal firstMonthInterest = debt.Principal * debt.AnnualRate / 100m / 12m;

        if (debt.AnnualRate > 0 && debt.Payment <= firstMonthInterest)
        {
            throw new BadRequestException(
                $"A payment of {debt.Payment:0.##} never clears this debt — the first month's interest alone is "
                + $"{firstMonthInterest:0.##}.",
                ExceptionCodes.FINANCE_DEBT_IMPOSSIBLE,
                "Raise the payment, or check whether the rate is a yearly percentage.");
        }

        if (debt.DownPaymentAccountId.HasValue)
        {
            await ResolveAccountAsync(debt.DownPaymentAccountId.Value, repository, unitOfWork);
        }

        if (debt.DisbursesToAccountId.HasValue)
        {
            await ResolveAccountAsync(debt.DisbursesToAccountId.Value, repository, unitOfWork);
        }
    }

    /// <summary>
    /// Names are what the dropdowns show, so two accounts called "Savings" is a
    /// typo every time. ux_fin_accounts_name would catch it, but as a
    /// constraint name rather than a sentence.
    /// </summary>
    public static async Task GuardNameFreeAsync(
        string name,
        Guid? exceptId,
        IFinanceRepository repository,
        IUnitOfWork unitOfWork)
    {
        FinanceAccount? existing = await repository.GetAccountByNameAsync(name, unitOfWork);

        if (existing is not null && existing.Id != exceptId)
        {
            throw new BadRequestException(
                $"There is already an account called '{existing.Name}'.",
                ExceptionCodes.FINANCE_ACCOUNT_NAME_EXISTS,
                "Pick another name.");
        }
    }
}
