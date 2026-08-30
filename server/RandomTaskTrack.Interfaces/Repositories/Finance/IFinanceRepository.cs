using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Models.Finance;
using RandomTaskTrack.Interfaces.Base;

namespace RandomTaskTrack.Interfaces.Repositories.Finance;

/// <summary>
/// One repository for the whole scope rather than seven. These tables are
/// always read together — the overview and the projection each need every one
/// of them — so splitting them would only mean seven constructor arguments in
/// every operation for no isolation anyone benefits from.
/// </summary>
public interface IFinanceRepository
{
    // ── Accounts ─────────────────────────────────────────────────────────────
    Task<List<FinanceAccount>> GetAccountsAsync(IUnitOfWork unitOfWork);
    Task<FinanceAccount?> GetAccountAsync(Guid id, IUnitOfWork unitOfWork);
    Task<FinanceAccount?> GetAccountByNameAsync(string name, IUnitOfWork unitOfWork);
    Task CreateAccountAsync(FinanceAccount account, IUnitOfWork unitOfWork);
    Task UpdateAccountAsync(FinanceAccount account, IUnitOfWork unitOfWork);
    Task<bool> DeleteAccountAsync(Guid id, IUnitOfWork unitOfWork);

    /// <summary>Entries, holdings and deposits still pointing at an account.</summary>
    Task<int> CountAccountUsesAsync(Guid id, IUnitOfWork unitOfWork);

    // ── Currencies ───────────────────────────────────────────────────────────
    Task<List<Currency>> GetCurrenciesAsync(IUnitOfWork unitOfWork);
    Task<Currency?> GetCurrencyAsync(string code, IUnitOfWork unitOfWork);
    Task UpsertCurrencyRateAsync(string code, decimal rateToBase, IUnitOfWork unitOfWork);

    // ── Flows ────────────────────────────────────────────────────────────────
    Task<List<FinanceFlow>> GetFlowsAsync(bool includeInactive, IUnitOfWork unitOfWork);
    Task<FinanceFlow?> GetFlowAsync(Guid id, IUnitOfWork unitOfWork);
    Task CreateFlowAsync(FinanceFlow flow, IUnitOfWork unitOfWork);
    Task UpdateFlowAsync(FinanceFlow flow, IUnitOfWork unitOfWork);
    Task<bool> DeleteFlowAsync(Guid id, IUnitOfWork unitOfWork);

    // ── Ledger ───────────────────────────────────────────────────────────────
    Task<List<LedgerEntry>> QueryEntriesAsync(
        DateOnly? from,
        DateOnly? to,
        FinanceFlowKind? kind,
        string? search,
        int limit,
        IUnitOfWork unitOfWork);

    Task<LedgerEntry?> GetEntryAsync(Guid id, IUnitOfWork unitOfWork);
    Task CreateEntryAsync(LedgerEntry entry, IUnitOfWork unitOfWork);
    Task UpdateEntryAsync(LedgerEntry entry, IUnitOfWork unitOfWork);
    Task<bool> DeleteEntryAsync(Guid id, IUnitOfWork unitOfWork);

    /// <summary>
    /// Every entry ever, netted per account and currency. Balances start from
    /// this: they are derived, never typed in.
    /// </summary>
    Task<List<AccountCash>> GetCashByAccountAsync(IUnitOfWork unitOfWork);

    /// <summary>Actual income and expenses per month, for the history half of the chart.</summary>
    Task<List<MonthlyTotal>> GetMonthlyTotalsAsync(DateOnly from, IUnitOfWork unitOfWork);

    // ── Holdings and trades ──────────────────────────────────────────────────
    Task<List<Holding>> GetHoldingsAsync(IUnitOfWork unitOfWork);
    Task<Holding?> GetHoldingAsync(Guid id, IUnitOfWork unitOfWork);
    Task<Holding?> GetHoldingBySymbolAsync(Guid accountId, string symbol, IUnitOfWork unitOfWork);
    Task CreateHoldingAsync(Holding holding, IUnitOfWork unitOfWork);
    Task UpdateHoldingAsync(Holding holding, IUnitOfWork unitOfWork);

    /// <summary>One quote answers for every account holding that symbol.</summary>
    Task<int> UpdatePricesBySymbolAsync(string symbol, decimal price, DateTime asOf, IUnitOfWork unitOfWork);

    Task<bool> DeleteHoldingAsync(Guid id, IUnitOfWork unitOfWork);

    Task<List<Trade>> GetTradesAsync(IUnitOfWork unitOfWork);
    Task<Trade?> GetTradeAsync(Guid id, IUnitOfWork unitOfWork);
    Task CreateTradeAsync(Trade trade, IUnitOfWork unitOfWork);
    Task UpdateTradeAsync(Trade trade, IUnitOfWork unitOfWork);
    Task<bool> DeleteTradeAsync(Guid id, IUnitOfWork unitOfWork);

    // ── Dividends ────────────────────────────────────────────────────────────
    Task<List<Dividend>> GetDividendsAsync(bool includeInactive, IUnitOfWork unitOfWork);
    Task<Dividend?> GetDividendAsync(Guid id, IUnitOfWork unitOfWork);
    Task CreateDividendAsync(Dividend dividend, IUnitOfWork unitOfWork);
    Task UpdateDividendAsync(Dividend dividend, IUnitOfWork unitOfWork);
    Task<bool> DeleteDividendAsync(Guid id, IUnitOfWork unitOfWork);

    // ── Deposits ─────────────────────────────────────────────────────────────
    Task<List<Deposit>> GetDepositsAsync(IUnitOfWork unitOfWork);
    Task<Deposit?> GetDepositAsync(Guid id, IUnitOfWork unitOfWork);
    Task CreateDepositAsync(Deposit deposit, IUnitOfWork unitOfWork);
    Task UpdateDepositAsync(Deposit deposit, IUnitOfWork unitOfWork);
    Task<bool> DeleteDepositAsync(Guid id, IUnitOfWork unitOfWork);

    // ── Targets ──────────────────────────────────────────────────────────────
    Task<List<FinanceTarget>> GetTargetsAsync(IUnitOfWork unitOfWork);
    Task<FinanceTarget?> GetTargetAsync(Guid id, IUnitOfWork unitOfWork);
    Task CreateTargetAsync(FinanceTarget target, IUnitOfWork unitOfWork);
    Task UpdateTargetAsync(FinanceTarget target, IUnitOfWork unitOfWork);
    Task<bool> DeleteTargetAsync(Guid id, IUnitOfWork unitOfWork);
}
