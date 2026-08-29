using Dapper;
using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Models.Finance;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Finance;

namespace RandomTaskTrack.Business.Repositories.Finance;

/// <inheritdoc cref="IFinanceRepository"/>
public class FinanceRepository : IFinanceRepository
{
    private const string FlowColumns =
        "id, kind, name, amount, currency, cadence, day_of_month, month_of_year, starts_on, ends_on, category, is_active, created_at, updated_at";

    private const string EntryColumns =
        "id, flow_id, kind, name, amount, currency, occurred_on, category, note, created_at";

    private const string HoldingColumns =
        "id, symbol, name, currency, last_price, last_price_at, created_at";

    private const string TradeColumns =
        "id, holding_id, side, quantity, price, fee, traded_on, note, created_at";

    private const string DividendColumns =
        "id, holding_id, name, amount, currency, cadence, day_of_month, month_of_year, starts_on, ends_on, is_active, created_at, updated_at";

    private const string DepositColumns =
        "id, name, principal, currency, annual_rate, compounding, opened_on, matures_on, note, created_at, updated_at";

    private const string TargetColumns =
        "id, label, target_on, amount, note, created_at";

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
            $@"SELECT {EntryColumns}
               FROM tracker.fin_entries
               WHERE (@from IS NULL OR occurred_on >= @from)
                 AND (@to   IS NULL OR occurred_on <= @to)
                 AND (@kind IS NULL OR kind = @kind)
                 AND (@search IS NULL OR name ILIKE '%' || @search || '%'
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
                  (id, flow_id, kind, name, amount, currency, occurred_on, category, note)
              VALUES (@Id, @FlowId, @Kind, @Name, @Amount, @Currency, @OccurredOn, @Category, @Note)",
            new
            {
                entry.Id,
                entry.FlowId,
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
              SET name        = @Name,
                  amount      = @Amount,
                  currency    = @Currency,
                  occurred_on = @OccurredOn,
                  category    = @Category,
                  note        = @Note
              WHERE id = @Id",
            new
            {
                entry.Id,
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

    public async Task<List<CurrencyAmount>> GetCashByCurrencyAsync(IUnitOfWork unitOfWork)
    {
        // kind 1 is income, 2 is expense. Signing here rather than in C# keeps
        // the whole ledger out of memory — it only ever grows.
        var rows = await unitOfWork.Connection.QueryAsync<CurrencyAmount>(
            @"SELECT currency,
                     SUM(CASE WHEN kind = 1 THEN amount ELSE -amount END) AS amount
              FROM tracker.fin_entries
              GROUP BY currency",
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

    public async Task<Holding?> GetHoldingBySymbolAsync(string symbol, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<Holding>(
            $"SELECT {HoldingColumns} FROM tracker.fin_holdings WHERE lower(symbol) = lower(@symbol)",
            new { symbol },
            unitOfWork.Transaction);
    }

    public async Task CreateHoldingAsync(Holding holding, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            @"INSERT INTO tracker.fin_holdings (id, symbol, name, currency)
              VALUES (@Id, @Symbol, @Name, @Currency)",
            new { holding.Id, holding.Symbol, holding.Name, holding.Currency },
            unitOfWork.Transaction);
    }

    public async Task UpdateHoldingAsync(Holding holding, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            @"UPDATE tracker.fin_holdings
              SET symbol   = @Symbol,
                  name     = @Name,
                  currency = @Currency
              WHERE id = @Id",
            new { holding.Id, holding.Symbol, holding.Name, holding.Currency },
            unitOfWork.Transaction);
    }

    public async Task UpdateHoldingPriceAsync(Guid id, decimal price, DateTime asOf, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            @"UPDATE tracker.fin_holdings
              SET last_price    = @price,
                  last_price_at = @asOf
              WHERE id = @id",
            new { id, price, asOf },
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
                  (id, name, principal, currency, annual_rate, compounding, opened_on, matures_on, note)
              VALUES (@Id, @Name, @Principal, @Currency, @AnnualRate, @Compounding, @OpenedOn, @MaturesOn, @Note)",
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
        deposit.Note
    };

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
