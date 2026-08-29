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
}
