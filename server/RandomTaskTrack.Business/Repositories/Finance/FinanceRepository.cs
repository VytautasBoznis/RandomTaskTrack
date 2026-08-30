using Dapper;
using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Models.Finance;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Finance;

namespace RandomTaskTrack.Business.Repositories.Finance;

/// <inheritdoc cref="IFinanceRepository"/>
public class FinanceRepository : IFinanceRepository
{
    private const string AccountColumns =
        "id, name, kind, currency, note, created_at, updated_at";

    private const string FlowColumns =
        "id, kind, name, amount, currency, cadence, day_of_month, month_of_year, starts_on, ends_on, category, is_active, created_at, updated_at";

    private const string EntryColumns =
        "id, flow_id, account_id, kind, name, amount, currency, occurred_on, category, note, created_at";

    private const string HoldingColumns =
        "id, account_id, symbol, name, currency, last_price, last_price_at, created_at";

    private const string TradeColumns =
        "id, holding_id, side, quantity, price, fee, traded_on, note, created_at";

    private const string DividendColumns =
        "id, holding_id, name, amount, currency, cadence, day_of_month, month_of_year, starts_on, ends_on, is_active, created_at, updated_at";

    private const string DepositColumns =
        "id, name, principal, currency, annual_rate, compounding, opened_on, matures_on, source_account_id, target_account_id, note, created_at, updated_at";

    private const string DebtColumns =
        "id, name, principal, currency, annual_rate, payment, starts_on, ends_on, asset_value, " +
        "down_payment, down_payment_account_id, disburses_to_account_id, note, created_at, updated_at";

    private const string DebtPaymentColumns =
        "id, debt_id, amount, paid_on, account_id, note, created_at";

    private const string TargetColumns =
        "id, label, target_on, amount, note, created_at";

    // ── Accounts ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Cash accounts before brokerages, then by name. That is the order the
    /// dropdowns and the cards use, and "which one is my current account" is
    /// answered by it being at the top.
    /// </summary>
    public async Task<List<FinanceAccount>> GetAccountsAsync(IUnitOfWork unitOfWork)
    {
        var rows = await unitOfWork.Connection.QueryAsync<FinanceAccount>(
            $@"SELECT {AccountColumns}
               FROM tracker.fin_accounts
               ORDER BY kind, name",
            transaction: unitOfWork.Transaction);

        return rows.ToList();
    }

    public async Task<FinanceAccount?> GetAccountAsync(Guid id, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<FinanceAccount>(
            $"SELECT {AccountColumns} FROM tracker.fin_accounts WHERE id = @id",
            new { id },
            unitOfWork.Transaction);
    }

    public async Task<FinanceAccount?> GetAccountByNameAsync(string name, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<FinanceAccount>(
            $"SELECT {AccountColumns} FROM tracker.fin_accounts WHERE lower(name) = lower(@name)",
            new { name },
            unitOfWork.Transaction);
    }

    public async Task CreateAccountAsync(FinanceAccount account, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            @"INSERT INTO tracker.fin_accounts (id, name, kind, currency, note)
              VALUES (@Id, @Name, @Kind, @Currency, @Note)",
            new { account.Id, account.Name, Kind = (int)account.Kind, account.Currency, account.Note },
            unitOfWork.Transaction);
    }

    public async Task UpdateAccountAsync(FinanceAccount account, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            @"UPDATE tracker.fin_accounts
              SET name       = @Name,
                  kind       = @Kind,
                  currency   = @Currency,
                  note       = @Note,
                  updated_at = now()
              WHERE id = @Id",
            new { account.Id, account.Name, Kind = (int)account.Kind, account.Currency, account.Note },
            unitOfWork.Transaction);
    }

    public async Task<bool> DeleteAccountAsync(Guid id, IUnitOfWork unitOfWork)
    {
        int affected = await unitOfWork.Connection.ExecuteAsync(
            "DELETE FROM tracker.fin_accounts WHERE id = @id",
            new { id },
            unitOfWork.Transaction);

        return affected > 0;
    }

    /// <summary>
    /// What is still attached to an account. The foreign keys already refuse
    /// the delete, but they refuse it as a constraint name — this is what lets
    /// the operation say "3 entries and 2 holdings" instead.
    /// </summary>
    public async Task<int> CountAccountUsesAsync(Guid id, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.ExecuteScalarAsync<int>(
            @"SELECT (SELECT count(*) FROM tracker.fin_entries  WHERE account_id = @id)
                   + (SELECT count(*) FROM tracker.fin_holdings WHERE account_id = @id)
                   + (SELECT count(*) FROM tracker.fin_deposits
                      WHERE source_account_id = @id OR target_account_id = @id)
                   + (SELECT count(*) FROM tracker.fin_debts
                      WHERE down_payment_account_id = @id OR disburses_to_account_id = @id)
                   + (SELECT count(*) FROM tracker.fin_debt_payments WHERE account_id = @id)",
            new { id },
            unitOfWork.Transaction);
    }

    // ── Currencies ───────────────────────────────────────────────────────────

    public async Task<List<Currency>> GetCurrenciesAsync(IUnitOfWork unitOfWork)
    {
        var rows = await unitOfWork.Connection.QueryAsync<Currency>(
            @"SELECT code, name, rate_to_base, updated_at
              FROM tracker.fin_currencies
              ORDER BY code",
            transaction: unitOfWork.Transaction);

        return rows.ToList();
    }

    public async Task<Currency?> GetCurrencyAsync(string code, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<Currency>(
            @"SELECT code, name, rate_to_base, updated_at
              FROM tracker.fin_currencies
              WHERE upper(code) = upper(@code)",
            new { code },
            unitOfWork.Transaction);
    }

    /// <summary>
    /// Rate only. The refresh must never invent a currency the user has not
    /// added — an unknown code coming back from the quote source is a bug in
    /// the symbol, not a new currency to start tracking.
    /// </summary>
    public async Task UpsertCurrencyRateAsync(string code, decimal rateToBase, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            @"UPDATE tracker.fin_currencies
              SET rate_to_base = @rateToBase,
                  updated_at   = now()
              WHERE upper(code) = upper(@code)",
            new { code, rateToBase },
            unitOfWork.Transaction);
    }

    // ── Flows ────────────────────────────────────────────────────────────────

    public async Task<List<FinanceFlow>> GetFlowsAsync(bool includeInactive, IUnitOfWork unitOfWork)
    {
        var rows = await unitOfWork.Connection.QueryAsync<FinanceFlow>(
            $@"SELECT {FlowColumns}
               FROM tracker.fin_flows
               WHERE (@includeInactive OR is_active)
               ORDER BY kind, name",
            new { includeInactive },
            unitOfWork.Transaction);

        return rows.ToList();
    }

    public async Task<FinanceFlow?> GetFlowAsync(Guid id, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<FinanceFlow>(
            $"SELECT {FlowColumns} FROM tracker.fin_flows WHERE id = @id",
            new { id },
            unitOfWork.Transaction);
    }

    public async Task CreateFlowAsync(FinanceFlow flow, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            @"INSERT INTO tracker.fin_flows
                  (id, kind, name, amount, currency, cadence, day_of_month, month_of_year,
                   starts_on, ends_on, category, is_active)
              VALUES (@Id, @Kind, @Name, @Amount, @Currency, @Cadence, @DayOfMonth, @MonthOfYear,
                      @StartsOn, @EndsOn, @Category, @IsActive)",
            ToFlowParameters(flow),
            unitOfWork.Transaction);
    }

    public async Task UpdateFlowAsync(FinanceFlow flow, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            @"UPDATE tracker.fin_flows
              SET name          = @Name,
                  amount        = @Amount,
                  currency      = @Currency,
                  cadence       = @Cadence,
                  day_of_month  = @DayOfMonth,
                  month_of_year = @MonthOfYear,
                  starts_on     = @StartsOn,
                  ends_on       = @EndsOn,
                  category      = @Category,
                  is_active     = @IsActive,
                  updated_at    = now()
              WHERE id = @Id",
            ToFlowParameters(flow),
            unitOfWork.Transaction);
    }

    public async Task<bool> DeleteFlowAsync(Guid id, IUnitOfWork unitOfWork)
    {
        int affected = await unitOfWork.Connection.ExecuteAsync(
            "DELETE FROM tracker.fin_flows WHERE id = @id",
            new { id },
            unitOfWork.Transaction);

        return affected > 0;
    }

    // Enums go to the database as ints. Dapper would send them as their
    // underlying type anyway, but Npgsql infers the parameter type from the CLR
    // type and refuses an enum against an int column, so the cast is explicit.
    private static object ToFlowParameters(FinanceFlow flow) => new
    {
        flow.Id,
        Kind = (int)flow.Kind,
        flow.Name,
        flow.Amount,
        flow.Currency,
        Cadence = (int)flow.Cadence,
        flow.DayOfMonth,
        flow.MonthOfYear,
        flow.StartsOn,
        flow.EndsOn,
        flow.Category,
        flow.IsActive
    };

    // ── Ledger ───────────────────────────────────────────────────────────────

    public async Task<List<LedgerEntry>> QueryEntriesAsync(
        DateOnly? from,
        DateOnly? to,
        FinanceFlowKind? kind,
        string? search,
        int limit,
        IUnitOfWork unitOfWork)
    {
        var rows = await unitOfWork.Connection.QueryAsync<LedgerEntry>(
            // Every optional filter is cast, the same way RecipesRepository and
            // RecurrencesRepository do it. A null parameter whose only use is
            // "IS NULL" gives Postgres nothing to infer a type from, and it
            // refuses to plan the statement at all — 42P08, not a wrong answer.
            // "Show me the whole ledger" sends null for both dates, so this is
            // the ordinary path rather than an edge case.
            $@"SELECT {EntryColumns}
               FROM tracker.fin_entries
               WHERE (@from::date IS NULL OR occurred_on >= @from::date)
                 AND (@to::date   IS NULL OR occurred_on <= @to::date)
                 AND (@kind::int  IS NULL OR kind = @kind::int)
                 AND (@search::text IS NULL OR name ILIKE '%' || @search || '%'
                                            OR category ILIKE '%' || @search || '%')
               ORDER BY occurred_on DESC, created_at DESC
               LIMIT @limit",
            new { from, to, kind = (int?)kind, search, limit },
            unitOfWork.Transaction);

        return rows.ToList();
    }

    public async Task<LedgerEntry?> GetEntryAsync(Guid id, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<LedgerEntry>(
            $"SELECT {EntryColumns} FROM tracker.fin_entries WHERE id = @id",
            new { id },
            unitOfWork.Transaction);
    }

    public async Task CreateEntryAsync(LedgerEntry entry, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            @"INSERT INTO tracker.fin_entries
                  (id, flow_id, account_id, kind, name, amount, currency, occurred_on, category, note)
              VALUES (@Id, @FlowId, @AccountId, @Kind, @Name, @Amount, @Currency, @OccurredOn, @Category, @Note)",
            new
            {
                entry.Id,
                entry.FlowId,
                entry.AccountId,
                Kind = (int)entry.Kind,
                entry.Name,
                entry.Amount,
                entry.Currency,
                entry.OccurredOn,
                entry.Category,
                entry.Note
            },
            unitOfWork.Transaction);
    }

    public async Task UpdateEntryAsync(LedgerEntry entry, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            @"UPDATE tracker.fin_entries
              SET account_id  = @AccountId,
                  name        = @Name,
                  amount      = @Amount,
                  currency    = @Currency,
                  occurred_on = @OccurredOn,
                  category    = @Category,
                  note        = @Note
              WHERE id = @Id",
            new
            {
                entry.Id,
                entry.AccountId,
                entry.Name,
                entry.Amount,
                entry.Currency,
                entry.OccurredOn,
                entry.Category,
                entry.Note
            },
            unitOfWork.Transaction);
    }

    public async Task<bool> DeleteEntryAsync(Guid id, IUnitOfWork unitOfWork)
    {
        int affected = await unitOfWork.Connection.ExecuteAsync(
            "DELETE FROM tracker.fin_entries WHERE id = @id",
            new { id },
            unitOfWork.Transaction);

        return affected > 0;
    }

    public async Task<List<AccountCash>> GetCashByAccountAsync(IUnitOfWork unitOfWork)
    {
        // kind 1 is income, 2 is expense. Signing here rather than in C# keeps
        // the whole ledger out of memory — it only ever grows.
        var rows = await unitOfWork.Connection.QueryAsync<AccountCash>(
            @"SELECT account_id,
                     currency,
                     SUM(CASE WHEN kind = 1 THEN amount ELSE -amount END) AS amount
              FROM tracker.fin_entries
              GROUP BY account_id, currency",
            transaction: unitOfWork.Transaction);

        return rows.ToList();
    }

    public async Task<List<MonthlyTotal>> GetMonthlyTotalsAsync(DateOnly from, IUnitOfWork unitOfWork)
    {
        var rows = await unitOfWork.Connection.QueryAsync<MonthlyTotal>(
            @"SELECT date_trunc('month', occurred_on)::date       AS month,
                     currency,
                     SUM(CASE WHEN kind = 1 THEN amount ELSE 0 END) AS income,
                     SUM(CASE WHEN kind = 2 THEN amount ELSE 0 END) AS expenses
              FROM tracker.fin_entries
              WHERE occurred_on >= @from
              GROUP BY 1, 2
              ORDER BY 1",
            new { from },
            unitOfWork.Transaction);

        return rows.ToList();
    }

    // ── Holdings and trades ──────────────────────────────────────────────────

    public async Task<List<Holding>> GetHoldingsAsync(IUnitOfWork unitOfWork)
    {
        var rows = await unitOfWork.Connection.QueryAsync<Holding>(
            $@"SELECT {HoldingColumns}
               FROM tracker.fin_holdings
               ORDER BY symbol",
            transaction: unitOfWork.Transaction);

        return rows.ToList();
    }

    public async Task<Holding?> GetHoldingAsync(Guid id, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<Holding>(
            $"SELECT {HoldingColumns} FROM tracker.fin_holdings WHERE id = @id",
            new { id },
            unitOfWork.Transaction);
    }

    /// <summary>
    /// Scoped to one account, because the symbol is only unique within one —
    /// the same ETF in a taxable account and a pension is two holdings.
    /// </summary>
    public async Task<Holding?> GetHoldingBySymbolAsync(Guid accountId, string symbol, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<Holding>(
            $@"SELECT {HoldingColumns}
               FROM tracker.fin_holdings
               WHERE account_id = @accountId AND lower(symbol) = lower(@symbol)",
            new { accountId, symbol },
            unitOfWork.Transaction);
    }

    public async Task CreateHoldingAsync(Holding holding, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            @"INSERT INTO tracker.fin_holdings (id, account_id, symbol, name, currency)
              VALUES (@Id, @AccountId, @Symbol, @Name, @Currency)",
            new { holding.Id, holding.AccountId, holding.Symbol, holding.Name, holding.Currency },
            unitOfWork.Transaction);
    }

    public async Task UpdateHoldingAsync(Holding holding, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            @"UPDATE tracker.fin_holdings
              SET account_id = @AccountId,
                  symbol     = @Symbol,
                  name       = @Name,
                  currency   = @Currency
              WHERE id = @Id",
            new { holding.Id, holding.AccountId, holding.Symbol, holding.Name, holding.Currency },
            unitOfWork.Transaction);
    }

    /// <summary>
    /// By symbol, not by id: one quote answers for every account holding it, so
    /// the refresh asks the source once and writes the price to all of them.
    /// </summary>
    public async Task<int> UpdatePricesBySymbolAsync(string symbol, decimal price, DateTime asOf, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.ExecuteAsync(
            @"UPDATE tracker.fin_holdings
              SET last_price    = @price,
                  last_price_at = @asOf
              WHERE lower(symbol) = lower(@symbol)",
            new { symbol, price, asOf },
            unitOfWork.Transaction);
    }

    public async Task<bool> DeleteHoldingAsync(Guid id, IUnitOfWork unitOfWork)
    {
        int affected = await unitOfWork.Connection.ExecuteAsync(
            "DELETE FROM tracker.fin_holdings WHERE id = @id",
            new { id },
            unitOfWork.Transaction);

        return affected > 0;
    }

    /// <summary>
    /// Every trade, not one holding's. Positions are summed for the whole
    /// portfolio on every overview, so one query beats N.
    /// </summary>
    public async Task<List<Trade>> GetTradesAsync(IUnitOfWork unitOfWork)
    {
        var rows = await unitOfWork.Connection.QueryAsync<Trade>(
            $@"SELECT {TradeColumns}
               FROM tracker.fin_trades
               ORDER BY traded_on DESC, created_at DESC",
            transaction: unitOfWork.Transaction);

        return rows.ToList();
    }

    public async Task<Trade?> GetTradeAsync(Guid id, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<Trade>(
            $"SELECT {TradeColumns} FROM tracker.fin_trades WHERE id = @id",
            new { id },
            unitOfWork.Transaction);
    }

    public async Task CreateTradeAsync(Trade trade, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            @"INSERT INTO tracker.fin_trades
                  (id, holding_id, side, quantity, price, fee, traded_on, note)
              VALUES (@Id, @HoldingId, @Side, @Quantity, @Price, @Fee, @TradedOn, @Note)",
            ToTradeParameters(trade),
            unitOfWork.Transaction);
    }

    public async Task UpdateTradeAsync(Trade trade, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            @"UPDATE tracker.fin_trades
              SET side      = @Side,
                  quantity  = @Quantity,
                  price     = @Price,
                  fee       = @Fee,
                  traded_on = @TradedOn,
                  note      = @Note
              WHERE id = @Id",
            ToTradeParameters(trade),
            unitOfWork.Transaction);
    }

    public async Task<bool> DeleteTradeAsync(Guid id, IUnitOfWork unitOfWork)
    {
        int affected = await unitOfWork.Connection.ExecuteAsync(
            "DELETE FROM tracker.fin_trades WHERE id = @id",
            new { id },
            unitOfWork.Transaction);

        return affected > 0;
    }

    private static object ToTradeParameters(Trade trade) => new
    {
        trade.Id,
        trade.HoldingId,
        Side = (int)trade.Side,
        trade.Quantity,
        trade.Price,
        trade.Fee,
        trade.TradedOn,
        trade.Note
    };

    // ── Dividends ────────────────────────────────────────────────────────────

    public async Task<List<Dividend>> GetDividendsAsync(bool includeInactive, IUnitOfWork unitOfWork)
    {
        var rows = await unitOfWork.Connection.QueryAsync<Dividend>(
            $@"SELECT {DividendColumns}
               FROM tracker.fin_dividends
               WHERE (@includeInactive OR is_active)
               ORDER BY name",
            new { includeInactive },
            unitOfWork.Transaction);

        return rows.ToList();
    }

    public async Task<Dividend?> GetDividendAsync(Guid id, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<Dividend>(
            $"SELECT {DividendColumns} FROM tracker.fin_dividends WHERE id = @id",
            new { id },
            unitOfWork.Transaction);
    }

    public async Task CreateDividendAsync(Dividend dividend, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            @"INSERT INTO tracker.fin_dividends
                  (id, holding_id, name, amount, currency, cadence, day_of_month, month_of_year,
                   starts_on, ends_on, is_active)
              VALUES (@Id, @HoldingId, @Name, @Amount, @Currency, @Cadence, @DayOfMonth, @MonthOfYear,
                      @StartsOn, @EndsOn, @IsActive)",
            ToDividendParameters(dividend),
            unitOfWork.Transaction);
    }

    public async Task UpdateDividendAsync(Dividend dividend, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            @"UPDATE tracker.fin_dividends
              SET name          = @Name,
                  amount        = @Amount,
                  currency      = @Currency,
                  cadence       = @Cadence,
                  day_of_month  = @DayOfMonth,
                  month_of_year = @MonthOfYear,
                  starts_on     = @StartsOn,
                  ends_on       = @EndsOn,
                  is_active     = @IsActive,
                  updated_at    = now()
              WHERE id = @Id",
            ToDividendParameters(dividend),
            unitOfWork.Transaction);
    }

    public async Task<bool> DeleteDividendAsync(Guid id, IUnitOfWork unitOfWork)
    {
        int affected = await unitOfWork.Connection.ExecuteAsync(
            "DELETE FROM tracker.fin_dividends WHERE id = @id",
            new { id },
            unitOfWork.Transaction);

        return affected > 0;
    }

    private static object ToDividendParameters(Dividend dividend) => new
    {
        dividend.Id,
        dividend.HoldingId,
        dividend.Name,
        dividend.Amount,
        dividend.Currency,
        Cadence = (int)dividend.Cadence,
        dividend.DayOfMonth,
        dividend.MonthOfYear,
        dividend.StartsOn,
        dividend.EndsOn,
        dividend.IsActive
    };

    // ── Deposits ─────────────────────────────────────────────────────────────

    public async Task<List<Deposit>> GetDepositsAsync(IUnitOfWork unitOfWork)
    {
        var rows = await unitOfWork.Connection.QueryAsync<Deposit>(
            $@"SELECT {DepositColumns}
               FROM tracker.fin_deposits
               ORDER BY matures_on NULLS LAST, name",
            transaction: unitOfWork.Transaction);

        return rows.ToList();
    }

    public async Task<Deposit?> GetDepositAsync(Guid id, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<Deposit>(
            $"SELECT {DepositColumns} FROM tracker.fin_deposits WHERE id = @id",
            new { id },
            unitOfWork.Transaction);
    }

    public async Task CreateDepositAsync(Deposit deposit, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            @"INSERT INTO tracker.fin_deposits
                  (id, name, principal, currency, annual_rate, compounding, opened_on, matures_on,
                   source_account_id, target_account_id, note)
              VALUES (@Id, @Name, @Principal, @Currency, @AnnualRate, @Compounding, @OpenedOn, @MaturesOn,
                      @SourceAccountId, @TargetAccountId, @Note)",
            ToDepositParameters(deposit),
            unitOfWork.Transaction);
    }

    public async Task UpdateDepositAsync(Deposit deposit, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            @"UPDATE tracker.fin_deposits
              SET name        = @Name,
                  principal   = @Principal,
                  currency    = @Currency,
                  annual_rate = @AnnualRate,
                  compounding = @Compounding,
                  opened_on   = @OpenedOn,
                  matures_on  = @MaturesOn,
                  source_account_id = @SourceAccountId,
                  target_account_id = @TargetAccountId,
                  note        = @Note,
                  updated_at  = now()
              WHERE id = @Id",
            ToDepositParameters(deposit),
            unitOfWork.Transaction);
    }

    public async Task<bool> DeleteDepositAsync(Guid id, IUnitOfWork unitOfWork)
    {
        int affected = await unitOfWork.Connection.ExecuteAsync(
            "DELETE FROM tracker.fin_deposits WHERE id = @id",
            new { id },
            unitOfWork.Transaction);

        return affected > 0;
    }

    private static object ToDepositParameters(Deposit deposit) => new
    {
        deposit.Id,
        deposit.Name,
        deposit.Principal,
        deposit.Currency,
        deposit.AnnualRate,
        Compounding = (int)deposit.Compounding,
        deposit.OpenedOn,
        deposit.MaturesOn,
        deposit.SourceAccountId,
        deposit.TargetAccountId,
        deposit.Note
    };

    // ── Debts ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Soonest to start first, which puts the debt you are still paying above
    /// the one you signed for next year.
    /// </summary>
    public async Task<List<Debt>> GetDebtsAsync(IUnitOfWork unitOfWork)
    {
        var rows = await unitOfWork.Connection.QueryAsync<Debt>(
            $@"SELECT {DebtColumns}
               FROM tracker.fin_debts
               ORDER BY starts_on, name",
            transaction: unitOfWork.Transaction);

        return rows.ToList();
    }

    public async Task<Debt?> GetDebtAsync(Guid id, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<Debt>(
            $"SELECT {DebtColumns} FROM tracker.fin_debts WHERE id = @id",
            new { id },
            unitOfWork.Transaction);
    }

    public async Task CreateDebtAsync(Debt debt, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            @"INSERT INTO tracker.fin_debts
                  (id, name, principal, currency, annual_rate, payment, starts_on, ends_on,
                   asset_value, down_payment, down_payment_account_id, disburses_to_account_id, note)
              VALUES (@Id, @Name, @Principal, @Currency, @AnnualRate, @Payment, @StartsOn, @EndsOn,
                      @AssetValue, @DownPayment, @DownPaymentAccountId, @DisbursesToAccountId, @Note)",
            ToDebtParameters(debt),
            unitOfWork.Transaction);
    }

    public async Task UpdateDebtAsync(Debt debt, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            @"UPDATE tracker.fin_debts
              SET name                    = @Name,
                  principal               = @Principal,
                  currency                = @Currency,
                  annual_rate             = @AnnualRate,
                  payment                 = @Payment,
                  starts_on               = @StartsOn,
                  ends_on                 = @EndsOn,
                  asset_value             = @AssetValue,
                  down_payment            = @DownPayment,
                  down_payment_account_id = @DownPaymentAccountId,
                  disburses_to_account_id = @DisbursesToAccountId,
                  note                    = @Note,
                  updated_at              = now()
              WHERE id = @Id",
            ToDebtParameters(debt),
            unitOfWork.Transaction);
    }

    public async Task<bool> DeleteDebtAsync(Guid id, IUnitOfWork unitOfWork)
    {
        int affected = await unitOfWork.Connection.ExecuteAsync(
            "DELETE FROM tracker.fin_debts WHERE id = @id",
            new { id },
            unitOfWork.Transaction);

        return affected > 0;
    }

    private static object ToDebtParameters(Debt debt) => new
    {
        debt.Id,
        debt.Name,
        debt.Principal,
        debt.Currency,
        debt.AnnualRate,
        debt.Payment,
        debt.StartsOn,
        debt.EndsOn,
        debt.AssetValue,
        debt.DownPayment,
        debt.DownPaymentAccountId,
        debt.DisbursesToAccountId,
        debt.Note
    };

    /// <summary>
    /// In date order, which is the order the schedule has to apply them in — a
    /// chunk changes what every month after it costs.
    /// </summary>
    public async Task<List<DebtPayment>> GetDebtPaymentsAsync(IUnitOfWork unitOfWork)
    {
        var rows = await unitOfWork.Connection.QueryAsync<DebtPayment>(
            $@"SELECT {DebtPaymentColumns}
               FROM tracker.fin_debt_payments
               ORDER BY paid_on, created_at",
            transaction: unitOfWork.Transaction);

        return rows.ToList();
    }

    public async Task CreateDebtPaymentAsync(DebtPayment payment, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            @"INSERT INTO tracker.fin_debt_payments (id, debt_id, amount, paid_on, account_id, note)
              VALUES (@Id, @DebtId, @Amount, @PaidOn, @AccountId, @Note)",
            new { payment.Id, payment.DebtId, payment.Amount, payment.PaidOn, payment.AccountId, payment.Note },
            unitOfWork.Transaction);
    }

    public async Task<bool> DeleteDebtPaymentAsync(Guid id, IUnitOfWork unitOfWork)
    {
        int affected = await unitOfWork.Connection.ExecuteAsync(
            "DELETE FROM tracker.fin_debt_payments WHERE id = @id",
            new { id },
            unitOfWork.Transaction);

        return affected > 0;
    }

    // ── Targets ──────────────────────────────────────────────────────────────

    public async Task<List<FinanceTarget>> GetTargetsAsync(IUnitOfWork unitOfWork)
    {
        var rows = await unitOfWork.Connection.QueryAsync<FinanceTarget>(
            $@"SELECT {TargetColumns}
               FROM tracker.fin_targets
               ORDER BY target_on NULLS LAST, amount",
            transaction: unitOfWork.Transaction);

        return rows.ToList();
    }

    public async Task<FinanceTarget?> GetTargetAsync(Guid id, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<FinanceTarget>(
            $"SELECT {TargetColumns} FROM tracker.fin_targets WHERE id = @id",
            new { id },
            unitOfWork.Transaction);
    }

    public async Task CreateTargetAsync(FinanceTarget target, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            @"INSERT INTO tracker.fin_targets (id, label, target_on, amount, note)
              VALUES (@Id, @Label, @TargetOn, @Amount, @Note)",
            new { target.Id, target.Label, target.TargetOn, target.Amount, target.Note },
            unitOfWork.Transaction);
    }

    public async Task UpdateTargetAsync(FinanceTarget target, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            @"UPDATE tracker.fin_targets
              SET label     = @Label,
                  target_on = @TargetOn,
                  amount    = @Amount,
                  note      = @Note
              WHERE id = @Id",
            new { target.Id, target.Label, target.TargetOn, target.Amount, target.Note },
            unitOfWork.Transaction);
    }

    public async Task<bool> DeleteTargetAsync(Guid id, IUnitOfWork unitOfWork)
    {
        int affected = await unitOfWork.Connection.ExecuteAsync(
            "DELETE FROM tracker.fin_targets WHERE id = @id",
            new { id },
            unitOfWork.Transaction);

        return affected > 0;
    }
}
