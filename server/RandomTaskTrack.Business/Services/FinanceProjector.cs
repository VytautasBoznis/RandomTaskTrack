using Microsoft.Extensions.Options;
using RandomTaskTrack.Data.Dtos.Finance;
using RandomTaskTrack.Data.Models.ConfigurationOptions;
using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Models.Finance;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Finance;
using RandomTaskTrack.Interfaces.Services;

namespace RandomTaskTrack.Business.Services;

/// <inheritdoc cref="IFinanceProjector"/>
/// <remarks>
/// The whole scope's arithmetic lives here, in one place, because it is the one
/// place it can be silently wrong. Two rules hold it together:
///
/// <b>Cash and assets never double-count.</b> The ledger is cash only. Deposits
/// and holdings are valued separately, and their past cash movements are
/// already inside the cash balance — buying a share three years ago is *why*
/// the cash is lower now. So the only future cash an asset produces is a
/// deposit maturing and a dividend landing.
///
/// <b>The present is actual, the future is projected.</b> Months up to and
/// including this one take their income and expenses from the ledger; only
/// later months apply the flow definitions. That is also what stops this month
/// being counted twice — the rent already paid is in the cash balance, and the
/// projection does not add it again.
///
/// <b>Balances are derived, never stored.</b> An account's balance is its
/// entries plus the money its deposits have moved: principal out while a
/// deposit is open, principal and interest back in once it has matured. Both
/// halves are computed here rather than written as rows, so nothing has to run
/// on a maturity date and deleting a deposit undoes both.
///
/// <b>A debt's payments are a flow; its transfers are a deposit.</b> That split
/// looks arbitrary and is not. The monthly payment behaves exactly like rent —
/// months up to today take it from the ledger, later months project it — which
/// is the only way it can avoid double-counting a mortgage payment the user
/// already logged. The one-off transfers around it (the downpayment, the
/// disbursement, a lump off the principal) behave exactly like a deposit
/// opening: derived on their date, never logged. The balance owed is neither.
/// It is amortised from the terms, unconditionally, because what you owe the
/// bank does not depend on whether you remembered to write the payment down.
/// </remarks>
public class FinanceProjector : IFinanceProjector
{
    private readonly IFinanceRepository _repository;
    private readonly IClock _clock;
    private readonly FinanceOptions _options;

    public FinanceProjector(
        IFinanceRepository repository,
        IClock clock,
        IOptions<FinanceOptions> options)
    {
        _repository = repository;
        _clock = clock;
        _options = options.Value;
    }

    public async Task<FinanceOverviewDto> BuildOverviewAsync(IUnitOfWork unitOfWork)
    {
        FinanceData data = await LoadAsync(unitOfWork);
        DateOnly today = _clock.Today;

        List<PositionDto> positions = BuildPositions(data);
        List<AccountDto> accounts = BuildAccounts(data, today, positions);
        List<ScheduledDebt> schedules = BuildSchedules(data);
        List<DebtDto> debts = BuildDebts(schedules, today, data);

        // Summed from the account cards rather than from the ledger directly,
        // so the tile and the cards under it can never disagree by a cent.
        decimal cash = accounts.Sum(a => a.BalanceBase);
        decimal deposits = StillHeld(data.Deposits, today).Sum(d => ToBase(ValueAt(d, today), d.Currency, data));
        decimal stocks = positions.Sum(p => p.MarketValueBase ?? 0m);

        // Through the same two helpers the projection uses, not summed off the
        // cards. Summing the cards was wrong in a way that only showed up ten
        // months before a purchase: DebtDto.AssetValueBase is what the thing is
        // worth, which a debt carries from the day it is entered, while owning
        // it starts on StartsOn. Adding those up counted a flat that had not
        // been bought yet and put 220k on net worth that the chart, filtering
        // properly, did not have. One code path now, so they cannot disagree.
        decimal assets = AssetsOn(schedules, today, data);
        decimal owed = OwedOn(schedules, today, data);

        return new FinanceOverviewDto
        {
            Today = today,
            BaseCurrency = _options.BaseCurrency,
            CashBase = Round(cash),
            DepositsBase = Round(deposits),
            StocksBase = Round(stocks),
            AssetsBase = Round(assets),
            DebtsBase = Round(owed),

            // Summed from the rounded parts, not rounded from the raw sum.
            // Otherwise the total is a cent off what the numbers beside it add
            // up to, and a total that visibly does not add up is a total nobody
            // trusts.
            NetWorthBase = Round(cash) + Round(deposits) + Round(stocks) + Round(assets) - Round(owed),
            MonthlyIncomeBase = Round(MonthlyRate(data, FinanceFlowKind.Income)),

            // The debt payments belong in here whether or not anyone also wrote
            // them down as a flow. "A typical month" that leaves out the
            // mortgage is the one number on the page that could talk somebody
            // into a second one.
            MonthlyExpenseBase = Round(MonthlyRate(data, FinanceFlowKind.Expense) + MonthlyDebtPayments(debts)),

            // A holding with no price contributes nothing, which would quietly
            // understate the total. The UI says so rather than showing a number
            // that is wrong by an unknown amount.
            HasUnpricedHoldings = positions.Any(p => p.Quantity > 0 && p.LastPrice is null),

            Accounts = accounts,
            Flows = data.Flows,
            Positions = positions,
            Deposits = data.Deposits,
            Debts = debts,
            Dividends = data.Dividends,
            Targets = data.Targets,
            Currencies = data.Currencies
        };
    }

    public async Task<List<ProjectionPointDto>> ProjectAsync(
        int historyMonths,
        int months,
        decimal stockGrowthPct,
        IUnitOfWork unitOfWork)
    {
        FinanceData data = await LoadAsync(unitOfWork);
        DateOnly today = _clock.Today;
        var thisMonth = new DateOnly(today.Year, today.Month, 1);
        DateOnly historyFrom = thisMonth.AddMonths(-historyMonths);

        List<MonthlyTotal> actuals = await _repository.GetMonthlyTotalsAsync(historyFrom, unitOfWork);

        var points = new List<ProjectionPointDto>();

        // ── Behind today: what the ledger says really happened ────────────────
        // No balances. Valuing holdings in the past needs historical prices this
        // app does not store, and a plausible-looking line drawn from nothing is
        // worse than an absent one.
        for (int i = 0; i < historyMonths; i++)
        {
            DateOnly month = historyFrom.AddMonths(i);

            decimal income = actuals.Where(a => a.Month == month).Sum(a => ToBase(a.Income, a.Currency, data));
            decimal expenses = actuals.Where(a => a.Month == month).Sum(a => ToBase(a.Expenses, a.Currency, data));

            points.Add(new ProjectionPointDto
            {
                Month = month,
                IsActual = true,
                Income = Round(income),
                Expenses = Round(expenses),
                Net = Round(income - expenses)
            });
        }

        // ── Today: the anchor every projected month builds on ─────────────────
        // Actual income and expenses month-to-date, and real balances. Cash here
        // already reflects everything paid so far this month, which is exactly
        // why the loop below starts applying flows at the *next* month.
        List<PositionDto> positions = BuildPositions(data);
        List<ScheduledDebt> schedules = BuildSchedules(data);

        decimal cash = BuildAccounts(data, today, positions).Sum(a => a.BalanceBase);
        decimal stocksNow = positions.Sum(p => p.MarketValueBase ?? 0m);

        decimal monthToDateIncome = actuals.Where(a => a.Month == thisMonth).Sum(a => ToBase(a.Income, a.Currency, data));
        decimal monthToDateExpenses = actuals.Where(a => a.Month == thisMonth).Sum(a => ToBase(a.Expenses, a.Currency, data));
        decimal depositsNow = StillHeld(data.Deposits, today).Sum(d => ToBase(ValueAt(d, today), d.Currency, data));
        decimal assetsNow = AssetsOn(schedules, today, data);
        decimal owedNow = OwedOn(schedules, today, data);

        points.Add(new ProjectionPointDto
        {
            Month = thisMonth,
            IsActual = true,
            Income = Round(monthToDateIncome),
            Expenses = Round(monthToDateExpenses),
            Net = Round(monthToDateIncome - monthToDateExpenses),
            Cash = Round(cash),
            Deposits = Round(depositsNow),
            Stocks = Round(stocksNow),
            Assets = Round(assetsNow),
            Debts = Round(owedNow),
            NetWorth = Round(cash) + Round(depositsNow) + Round(stocksNow) + Round(assetsNow) - Round(owedNow)
        });

        // ── Ahead of today: the flow definitions ──────────────────────────────
        decimal runningCash = cash;

        for (int i = 1; i <= months; i++)
        {
            DateOnly month = thisMonth.AddMonths(i);
            DateOnly monthEnd = month.AddMonths(1).AddDays(-1);

            decimal income = 0m;
            decimal expenses = 0m;

            foreach (FinanceFlow flow in data.Flows.Where(f => f.IsActive))
            {
                int hits = OccurrencesInMonth(flow.Cadence, flow.StartsOn, flow.EndsOn, flow.DayOfMonth, flow.MonthOfYear, month);

                if (hits == 0)
                {
                    continue;
                }

                decimal amount = ToBase(flow.Amount, flow.Currency, data) * hits;

                if (flow.Kind == FinanceFlowKind.Income)
                {
                    income += amount;
                }
                else
                {
                    expenses += amount;
                }
            }

            // Expected dividends are income you have not logged yet. One that
            // actually landed is a ledger entry like any other, which is why
            // these only ever apply to future months.
            foreach (Dividend dividend in data.Dividends.Where(d => d.IsActive))
            {
                int hits = OccurrencesInMonth(dividend.Cadence, dividend.StartsOn, dividend.EndsOn, dividend.DayOfMonth, dividend.MonthOfYear, month);

                if (hits > 0)
                {
                    income += ToBase(dividend.Amount, dividend.Currency, data) * hits;
                }
            }

            // A debt payment is an expense like the rent it usually replaces,
            // and it stops of its own accord — the schedule simply runs out the
            // month the balance clears. That is what makes "my payments end in
            // 2047" a thing the chart can show rather than a date to remember.
            foreach (ScheduledDebt scheduled in schedules)
            {
                DebtMonth? due = scheduled.Schedule.FirstOrDefault(m => m.Month == month);

                if (due is not null)
                {
                    expenses += ToBase(due.Payment, scheduled.Debt.Currency, data);
                }
            }

            // The first bucket reaches back to the day after today rather than
            // to its own first, because the anchor above only counts deposits
            // that have already opened or matured. Without the overlap, one
            // maturing on the 28th when today is the 15th falls into the gap
            // between the two and leaves the projection for good.
            DateOnly windowFrom = i == 1 ? today.AddDays(1) : month;

            // A deposit maturing is the one moment its money crosses back into
            // cash. Only future maturities count: a deposit that matured before
            // today is already in the balance.
            decimal maturing = data.Deposits
                .Where(d => d.MaturesOn.HasValue && d.MaturesOn.Value >= windowFrom && d.MaturesOn.Value <= monthEnd)
                .Sum(d => ToBase(ValueAt(d, d.MaturesOn!.Value), d.Currency, data));

            // And a deposit opening is the mirror of it: the principal leaves
            // the source account on the day it opens. Only deposits that name
            // a source move themselves — ones opened before accounts existed
            // had their transfer logged by hand, and taking it out again here
            // would charge for it twice.
            decimal locking = data.Deposits
                .Where(d => d.SourceAccountId.HasValue && d.OpenedOn >= windowFrom && d.OpenedOn <= monthEnd)
                .Sum(d => ToBase(d.Principal, d.Currency, data));

            // The one-off transfers around a debt, on the same footing as a
            // deposit opening and inside the same window. The borrowing lands
            // only when an account is named — for a mortgage the bank pays the
            // seller and the money never passes through here, which is why the
            // downpayment can leave without anything arriving.
            decimal borrowed = data.Debts
                .Where(d => d.DisbursesToAccountId.HasValue && d.StartsOn >= windowFrom && d.StartsOn <= monthEnd)
                .Sum(d => ToBase(d.Principal, d.Currency, data));

            decimal down = data.Debts
                .Where(d => d.DownPaymentAccountId.HasValue && d.StartsOn >= windowFrom && d.StartsOn <= monthEnd)
                .Sum(d => ToBase(d.DownPayment ?? 0m, d.Currency, data));

            // A planned lump off the principal is cash leaving on the day it is
            // dated. Its effect on the balance is already in the schedule; this
            // is only the other half of it.
            decimal lumps = data.DebtPayments
                .Where(p => p.AccountId.HasValue && p.PaidOn >= windowFrom && p.PaidOn <= monthEnd)
                .Sum(p => ToBase(p.Amount, DebtCurrency(p, data), data));

            runningCash += income - expenses + maturing - locking + borrowed - down - lumps;

            decimal deposits = StillHeld(data.Deposits, monthEnd)
                .Sum(d => ToBase(ValueAt(d, monthEnd), d.Currency, data));

            // Growth is applied to the portfolio as a whole from today, rather
            // than per holding, because the assumption is one number the user
            // typed — pretending it is per-stock would be false precision.
            decimal stocks = stocksNow * (decimal)Math.Pow(1 + (double)stockGrowthPct / 100.0, i / 12.0);

            // Property is held flat for the same reason, in reverse: nobody
            // typed an assumption for it, so inventing one would be the false
            // precision. It appears whole the month its debt starts.
            decimal assets = AssetsOn(schedules, monthEnd, data);
            decimal owed = OwedOn(schedules, monthEnd, data);

            points.Add(new ProjectionPointDto
            {
                Month = month,
                IsActual = false,
                Income = Round(income),
                Expenses = Round(expenses),
                Net = Round(income - expenses),
                Cash = Round(runningCash),
                Deposits = Round(deposits),
                Stocks = Round(stocks),
                Assets = Round(assets),
                Debts = Round(owed),
                NetWorth = Round(runningCash) + Round(deposits) + Round(stocks) + Round(assets) - Round(owed)
            });
        }

        return points;
    }

    // ── Accounts ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Every account with what is sitting in it. Nothing here is read from a
    /// column: the balance is the account's entries plus what its deposits and
    /// debts have moved, which is why "set the balance to 4,180" is an
    /// adjustment entry and not an UPDATE.
    /// </summary>
    private List<AccountDto> BuildAccounts(FinanceData data, DateOnly today, List<PositionDto> positions)
    {
        var accounts = new List<AccountDto>();

        foreach (FinanceAccount account in data.Accounts)
        {
            decimal balance = data.Cash
                                  .Where(c => c.AccountId == account.Id)
                                  .Sum(c => ToBase(c.Amount, c.Currency, data))
                              + DepositMovement(account.Id, today, data)
                              + DebtMovement(account.Id, today, data);

            decimal holdings = positions
                .Where(p => p.AccountId == account.Id)
                .Sum(p => p.MarketValueBase ?? 0m);

            // Still in the deposit, not in the account — so it is reported
            // beside the balance rather than added to it.
            List<Deposit> incoming = data.Deposits
                .Where(d => d.TargetAccountId == account.Id && d.MaturesOn.HasValue && d.MaturesOn.Value > today)
                .ToList();

            accounts.Add(new AccountDto
            {
                Id = account.Id,
                Name = account.Name,
                Kind = account.Kind,
                Currency = account.Currency,
                Note = account.Note,
                Balance = Round(FromBase(balance, account.Currency, data)),
                BalanceBase = Round(balance),
                HoldingsBase = Round(holdings),
                ValueBase = Round(balance) + Round(holdings),
                MaturingBase = Round(incoming.Sum(d => ToBase(ValueAt(d, d.MaturesOn!.Value), d.Currency, data))),
                NextMaturityOn = incoming.Count == 0 ? null : incoming.Min(d => d.MaturesOn!.Value)
            });
        }

        return accounts;
    }

    /// <summary>
    /// What the deposits have done to one account's balance by a given date:
    /// principal out of the source from the day it opened, principal plus
    /// interest into the target from the day it matured. A deposit with neither
    /// account set moves nothing — it predates accounts, and its transfer is
    /// already a hand-logged entry.
    /// </summary>
    private static decimal DepositMovement(Guid accountId, DateOnly on, FinanceData data)
    {
        decimal out_ = data.Deposits
            .Where(d => d.SourceAccountId == accountId && d.OpenedOn <= on)
            .Sum(d => ToBase(d.Principal, d.Currency, data));

        decimal back = data.Deposits
            .Where(d => d.TargetAccountId == accountId && d.MaturesOn.HasValue && d.MaturesOn.Value <= on)
            .Sum(d => ToBase(ValueAt(d, d.MaturesOn!.Value), d.Currency, data));

        return back - out_;
    }

    // ── Positions ────────────────────────────────────────────────────────────

    /// <summary>
    /// Folds the trades into each holding. Quantity and cost basis are summed
    /// here and nowhere else, so correcting a mistyped trade corrects the
    /// position, the market value and the net worth for free.
    /// </summary>
    private List<PositionDto> BuildPositions(FinanceData data)
    {
        var positions = new List<PositionDto>();

        foreach (Holding holding in data.Holdings)
        {
            List<Trade> trades = data.Trades.Where(t => t.HoldingId == holding.Id).ToList();

            decimal bought = trades.Where(t => t.Side == TradeSide.Buy).Sum(t => t.Quantity);
            decimal sold = trades.Where(t => t.Side == TradeSide.Sell).Sum(t => t.Quantity);
            decimal quantity = bought - sold;

            // Average cost, not FIFO. This is a personal tracker, not a tax
            // return, and average cost is the number that answers "am I up on
            // this" without needing a lot-matching engine nobody asked for.
            decimal buyCost = trades.Where(t => t.Side == TradeSide.Buy).Sum(t => t.Quantity * t.Price + t.Fee);
            decimal averageCost = bought > 0 ? buyCost / bought : 0m;

            decimal? marketValue = holding.LastPrice.HasValue ? quantity * holding.LastPrice.Value : null;

            positions.Add(new PositionDto
            {
                Id = holding.Id,
                AccountId = holding.AccountId,
                Symbol = holding.Symbol,
                Name = holding.Name,
                Currency = holding.Currency,
                LastPrice = holding.LastPrice,
                LastPriceAt = holding.LastPriceAt,
                Quantity = quantity,
                CostBasis = Round(averageCost * quantity),
                MarketValue = marketValue.HasValue ? Round(marketValue.Value) : null,
                MarketValueBase = marketValue.HasValue ? Round(ToBase(marketValue.Value, holding.Currency, data)) : null,
                Trades = trades
            });
        }

        return positions;
    }

    // ── Deposits ─────────────────────────────────────────────────────────────

    /// <summary>
    /// What a deposit is worth on a given date. Contractual, not assumed — this
    /// is the one asset in the scope whose future value is not a guess.
    ///
    /// ACT/365: elapsed days over 365, the convention retail deposits are quoted
    /// on. A matured deposit is worth nothing here because its money has moved
    /// to cash; the maturity value is added there in the same month.
    /// </summary>
    /// <summary>
    /// The deposits that are still assets on a given date. A deposit maturing
    /// *within* the month has already had its value moved into cash, so leaving
    /// it in this list too would count the same money twice — which is exactly
    /// what the first cut of this did, inflating net worth by a whole deposit
    /// for one month.
    /// </summary>
    private static IEnumerable<Deposit> StillHeld(IEnumerable<Deposit> deposits, DateOnly on) =>
        deposits.Where(d => !d.MaturesOn.HasValue || d.MaturesOn.Value > on);

    private static decimal ValueAt(Deposit deposit, DateOnly on)
    {
        if (on < deposit.OpenedOn)
        {
            return 0m;
        }

        if (deposit.MaturesOn.HasValue && on > deposit.MaturesOn.Value)
        {
            return 0m;
        }

        double years = (on.ToDateTime(TimeOnly.MinValue) - deposit.OpenedOn.ToDateTime(TimeOnly.MinValue)).TotalDays / 365.0;
        double rate = (double)deposit.AnnualRate / 100.0;

        double factor = deposit.Compounding switch
        {
            DepositCompounding.Simple => 1 + rate * years,
            DepositCompounding.Monthly => Math.Pow(1 + rate / 12.0, years * 12.0),
            _ => Math.Pow(1 + rate, years)
        };

        return deposit.Principal * (decimal)factor;
    }

    // ── Debts ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 50 years. A payment that does not cover the month's interest never
    /// amortises, and without a stop the loop below would run until the decimal
    /// overflowed. The validator refuses that shape on the way in; this is what
    /// stops a row that predates the validator, or one edited around it, from
    /// taking the whole overview down with it.
    /// </summary>
    private const int ScheduleCapMonths = 600;

    /// <summary>One month of a debt's life, in the debt's own currency.</summary>
    /// <param name="Payment">
    /// What is actually taken this month, which is the contractual payment
    /// every month but the last — that one is a stub for whatever is left.
    /// </param>
    /// <param name="Closing">Owed at the end of the month. Zero on the payoff month.</param>
    private sealed record DebtMonth(DateOnly Month, decimal Interest, decimal Payment, decimal Closing);

    /// <summary>A debt with its schedule already run, so nothing amortises twice.</summary>
    private sealed record ScheduledDebt(Debt Debt, List<DebtMonth> Schedule, List<DebtPayment> Payments);

    private static List<ScheduledDebt> BuildSchedules(FinanceData data) =>
        data.Debts
            .Select(debt =>
            {
                List<DebtPayment> lumps = data.DebtPayments
                    .Where(p => p.DebtId == debt.Id)
                    .OrderBy(p => p.PaidOn)
                    .ToList();

                return new ScheduledDebt(debt, Amortise(debt, lumps), lumps);
            })
            .ToList();

    /// <summary>
    /// The whole life of a debt, month by month. The one piece of arithmetic in
    /// this scope that runs forward rather than being evaluated at a date,
    /// because each month's interest depends on what the month before left
    /// behind — there is no closed form once a lump sum lands in the middle of
    /// it.
    ///
    /// Within a month: interest accrues on the opening balance, the payment
    /// covers that and takes the remainder off the principal, then any lump
    /// sums dated in the month come off what is left. Paying the chunk last is
    /// the conservative order — it charges a full month's interest on money that
    /// might have gone on the 2nd — and it keeps a chunk from ever making a
    /// month's interest negative.
    ///
    /// A zero rate is not a special case: the interest term falls out and every
    /// payment goes entirely on the principal, which is exactly what an
    /// interest-free instalment plan does.
    /// </summary>
    private static List<DebtMonth> Amortise(Debt debt, List<DebtPayment> lumps)
    {
        var schedule = new List<DebtMonth>();
        var first = new DateOnly(debt.StartsOn.Year, debt.StartsOn.Month, 1);
        DateOnly? lastAllowed = debt.EndsOn.HasValue
            ? new DateOnly(debt.EndsOn.Value.Year, debt.EndsOn.Value.Month, 1)
            : null;

        decimal balance = debt.Principal;
        decimal monthlyRate = debt.AnnualRate / 100m / 12m;

        for (int i = 0; i < ScheduleCapMonths && balance > 0m; i++)
        {
            DateOnly month = first.AddMonths(i);

            // ends_on caps the schedule whether or not the balance has cleared.
            // Whatever is standing when it does is the balloon — a lease
            // residual, the lump at the end of an interest-only deal — and it
            // is reported rather than silently paid or silently paid forever.
            if (lastAllowed.HasValue && month > lastAllowed.Value)
            {
                break;
            }

            decimal interest = Round(balance * monthlyRate);

            // The last payment is a stub. Taking the full amount would overpay
            // the bank by up to a payment and leave the balance negative, which
            // then reads as an asset.
            decimal payment = Math.Min(debt.Payment, balance + interest);

            balance = balance + interest - payment;

            decimal lump = lumps
                .Where(p => p.PaidOn.Year == month.Year && p.PaidOn.Month == month.Month)
                .Sum(p => p.Amount);

            // Overpaying the balance to death clears it; it does not go
            // negative and turn into money the bank owes you.
            balance = Math.Max(0m, balance - lump);

            schedule.Add(new DebtMonth(month, interest, payment, Round(balance)));
        }

        return schedule;
    }

    /// <summary>
    /// What is still owed on a date, in the debt's own currency. Nothing before
    /// the debt starts — a mortgage you sign next year is not money you owe
    /// today — and the closing balance of the month it falls in after that.
    ///
    /// Monthly granularity, deliberately: the projection buckets by month, and a
    /// balance accurate to the day inside a chart accurate to the month would be
    /// precision with nowhere to go.
    /// </summary>
    private static decimal OwedOn(ScheduledDebt scheduled, DateOnly on)
    {
        var month = new DateOnly(on.Year, on.Month, 1);

        if (scheduled.Schedule.Count == 0 || month < scheduled.Schedule[0].Month)
        {
            return 0m;
        }

        DebtMonth? current = scheduled.Schedule.LastOrDefault(m => m.Month <= month);

        return current?.Closing ?? 0m;
    }

    private static decimal OwedOn(List<ScheduledDebt> schedules, DateOnly on, FinanceData data) =>
        schedules.Sum(s => ToBase(OwedOn(s, on), s.Debt.Currency, data));

    /// <summary>
    /// What the debts have bought by a date, held flat. A debt that has not
    /// started has not bought anything yet, which is what keeps the asset and
    /// the borrowing appearing in the same month instead of the flat arriving
    /// early and inflating net worth in between.
    ///
    /// The asset outlives the debt on purpose: the flat is still yours after the
    /// mortgage is paid off.
    /// </summary>
    private static decimal AssetsOn(List<ScheduledDebt> schedules, DateOnly on, FinanceData data) =>
        schedules
            .Where(s => s.Debt.AssetValue.HasValue && s.Debt.StartsOn <= on)
            .Sum(s => ToBase(s.Debt.AssetValue!.Value, s.Debt.Currency, data));

    /// <summary>
    /// What the debts have done to one account's balance by a given date. The
    /// mirror of <see cref="DepositMovement"/>, and it plays by the same rule:
    /// only the halves that name an account move anything, so a debt taken out
    /// before it was tracked here — whose cash moved years ago and is already
    /// inside the balance — moves nothing and cannot take it out twice.
    ///
    /// The monthly payments are deliberately not here. They are projected as
    /// expenses instead, because a payment already logged in the ledger would
    /// otherwise come out of the balance a second time.
    /// </summary>
    private static decimal DebtMovement(Guid accountId, DateOnly on, FinanceData data)
    {
        decimal borrowed = data.Debts
            .Where(d => d.DisbursesToAccountId == accountId && d.StartsOn <= on)
            .Sum(d => ToBase(d.Principal, d.Currency, data));

        decimal down = data.Debts
            .Where(d => d.DownPaymentAccountId == accountId && d.StartsOn <= on)
            .Sum(d => ToBase(d.DownPayment ?? 0m, d.Currency, data));

        decimal lumps = data.DebtPayments
            .Where(p => p.AccountId == accountId && p.PaidOn <= on)
            .Sum(p => ToBase(p.Amount, DebtCurrency(p, data), data));

        return borrowed - down - lumps;
    }

    /// <summary>
    /// A lump sum has no currency of its own — it is paid in whatever the debt
    /// is denominated in, and giving it a second column would only let the two
    /// disagree.
    /// </summary>
    private static string DebtCurrency(DebtPayment payment, FinanceData data) =>
        data.Debts.FirstOrDefault(d => d.Id == payment.DebtId)?.Currency ?? "";

    /// <summary>
    /// The debts with their schedules read off. Everything derived is computed
    /// once here rather than per card, which is what keeps the tile, the list
    /// and the chart quoting the same figure.
    /// </summary>
    private static List<DebtDto> BuildDebts(List<ScheduledDebt> schedules, DateOnly today, FinanceData data)
    {
        var thisMonth = new DateOnly(today.Year, today.Month, 1);
        var debts = new List<DebtDto>();

        foreach (ScheduledDebt scheduled in schedules)
        {
            Debt debt = scheduled.Debt;
            List<DebtMonth> schedule = scheduled.Schedule;

            decimal outstanding = OwedOn(scheduled, today);

            // The first month it reads zero. Null when it never does, which is
            // a payment that does not cover the interest — the card says so
            // rather than showing a date 50 years out that only means "capped".
            DateOnly? paidOff = schedule.FirstOrDefault(m => m.Closing == 0m)?.Month;

            // Only a balance still standing on the contractual last month is a
            // balloon. One left by the 50-year cap is a debt that never clears,
            // and calling that a balloon would dress up a broken row as a
            // feature of the deal.
            DebtMonth? last = schedule.Count == 0 ? null : schedule[^1];

            decimal balloon = last is not null
                              && last.Closing > 0m
                              && debt.EndsOn.HasValue
                              && last.Month == new DateOnly(debt.EndsOn.Value.Year, debt.EndsOn.Value.Month, 1)
                ? last.Closing
                : 0m;

            decimal interestAhead = schedule.Where(m => m.Month > thisMonth).Sum(m => m.Interest);

            debts.Add(new DebtDto
            {
                Id = debt.Id,
                Name = debt.Name,
                Principal = debt.Principal,
                Currency = debt.Currency,
                AnnualRate = debt.AnnualRate,
                Payment = debt.Payment,
                StartsOn = debt.StartsOn,
                EndsOn = debt.EndsOn,
                AssetValue = debt.AssetValue,
                DownPayment = debt.DownPayment,
                DownPaymentAccountId = debt.DownPaymentAccountId,
                DisbursesToAccountId = debt.DisbursesToAccountId,
                Note = debt.Note,

                Outstanding = Round(outstanding),
                OutstandingBase = Round(ToBase(outstanding, debt.Currency, data)),
                AssetValueBase = debt.AssetValue.HasValue
                    ? Round(ToBase(debt.AssetValue.Value, debt.Currency, data))
                    : null,
                PaymentBase = Round(ToBase(debt.Payment, debt.Currency, data)),
                PaidOffOn = paidOff,
                BalloonBase = Round(ToBase(balloon, debt.Currency, data)),
                InterestRemainingBase = Round(ToBase(interestAhead, debt.Currency, data)),
                Payments = scheduled.Payments.OrderByDescending(p => p.PaidOn).ToList()
            });
        }

        return debts;
    }

    /// <summary>
    /// What the debts add to a typical month. Only the ones still being paid:
    /// a cleared debt costs nothing, and one that starts next year is not part
    /// of this month either.
    /// </summary>
    private static decimal MonthlyDebtPayments(List<DebtDto> debts) =>
        debts.Where(d => d.OutstandingBase > 0m).Sum(d => d.PaymentBase);

    // ── Cadence ──────────────────────────────────────────────────────────────

    /// <summary>
    /// How many times a cadence fires inside one calendar month. Weekly can be
    /// four or five, which is the whole reason this counts rather than assuming
    /// one: a weekly expense is not 4× a month, and calling it that drifts by
    /// almost a month's worth over a year.
    ///
    /// Every cadence anchors on <paramref name="startsOn"/>.
    /// <paramref name="dayOfMonth"/> and <paramref name="monthOfYear"/> are
    /// optional overrides for when the calendar matters more than the anchor.
    /// Quarterly counts from the anchor month and ignores monthOfYear, which
    /// would not mean anything for it.
    /// </summary>
    private static int OccurrencesInMonth(
        FinanceCadence cadence,
        DateOnly startsOn,
        DateOnly? endsOn,
        int? dayOfMonth,
        int? monthOfYear,
        DateOnly month)
    {
        DateOnly monthEnd = month.AddMonths(1).AddDays(-1);

        if (endsOn.HasValue && endsOn.Value < month)
        {
            return 0;
        }

        if (startsOn > monthEnd)
        {
            return 0;
        }

        if (cadence == FinanceCadence.Weekly)
        {
            // Step to the first occurrence on or after the month starts rather
            // than walking every week since startsOn, which could be years.
            DateOnly first = startsOn >= month
                ? startsOn
                : month.AddDays((7 - (month.DayNumber - startsOn.DayNumber) % 7) % 7);

            DateOnly last = endsOn.HasValue && endsOn.Value < monthEnd ? endsOn.Value : monthEnd;

            return first > last ? 0 : (last.DayNumber - first.DayNumber) / 7 + 1;
        }

        int monthsFromStart = (month.Year - startsOn.Year) * 12 + month.Month - startsOn.Month;

        bool fires = cadence switch
        {
            FinanceCadence.Monthly => true,
            FinanceCadence.Quarterly => monthsFromStart % 3 == 0,
            _ => month.Month == (monthOfYear ?? startsOn.Month)
        };

        if (!fires || monthsFromStart < 0)
        {
            return 0;
        }

        // Clamped, so "the 31st" is the 30th in November rather than skipped.
        int day = Math.Min(dayOfMonth ?? startsOn.Day, DateTime.DaysInMonth(month.Year, month.Month));
        var occurrence = new DateOnly(month.Year, month.Month, day);

        if (occurrence < startsOn || (endsOn.HasValue && occurrence > endsOn.Value))
        {
            return 0;
        }

        return 1;
    }

    /// <summary>
    /// What the active flows of one kind come to in a typical month, for the
    /// overview tiles. Quarterly and yearly are spread rather than counted, so
    /// the figure reads as a rate and not as "zero this month".
    /// </summary>
    private static decimal MonthlyRate(FinanceData data, FinanceFlowKind kind) =>
        data.Flows
            .Where(f => f.IsActive && f.Kind == kind)
            .Sum(f => ToBase(f.Amount, f.Currency, data) * f.Cadence switch
            {
                // 52 weeks over 12 months, not 4 — the difference is a month a year.
                FinanceCadence.Weekly => 52m / 12m,
                FinanceCadence.Monthly => 1m,
                FinanceCadence.Quarterly => 1m / 3m,
                _ => 1m / 12m
            });

    // ── Currency ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Converts to the base currency. rate_to_base is units per base unit, so
    /// USD at 1.08 means $108 is €100 — divide, do not multiply.
    ///
    /// An unknown currency is passed through unconverted rather than dropped.
    /// The rate table is hand-maintained, and a missing row should make one
    /// number slightly wrong rather than silently zero out a holding.
    /// </summary>
    private static decimal ToBase(decimal amount, string currency, FinanceData data)
    {
        Currency? rate = data.Currencies.FirstOrDefault(c => string.Equals(c.Code, currency, StringComparison.OrdinalIgnoreCase));

        return rate is null || rate.RateToBase == 0 ? amount : amount / rate.RateToBase;
    }

    /// <summary>
    /// Back the other way, for an account quoted in something other than the
    /// base. Multiply, since rate_to_base is units per base unit.
    /// </summary>
    private static decimal FromBase(decimal amount, string currency, FinanceData data)
    {
        Currency? rate = data.Currencies.FirstOrDefault(c => string.Equals(c.Code, currency, StringComparison.OrdinalIgnoreCase));

        return rate is null ? amount : amount * rate.RateToBase;
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    // ── Loading ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Everything, in one pass. Both entry points need all of it, and loading
    /// per-holding trades would be N+1 on a page that always shows every
    /// holding.
    /// </summary>
    private async Task<FinanceData> LoadAsync(IUnitOfWork unitOfWork) => new()
    {
        Accounts = await _repository.GetAccountsAsync(unitOfWork),
        Currencies = await _repository.GetCurrenciesAsync(unitOfWork),
        Flows = await _repository.GetFlowsAsync(true, unitOfWork),
        Cash = await _repository.GetCashByAccountAsync(unitOfWork),
        Holdings = await _repository.GetHoldingsAsync(unitOfWork),
        Trades = await _repository.GetTradesAsync(unitOfWork),
        Dividends = await _repository.GetDividendsAsync(true, unitOfWork),
        Deposits = await _repository.GetDepositsAsync(unitOfWork),
        Debts = await _repository.GetDebtsAsync(unitOfWork),
        DebtPayments = await _repository.GetDebtPaymentsAsync(unitOfWork),
        Targets = await _repository.GetTargetsAsync(unitOfWork)
    };

    private class FinanceData
    {
        public List<FinanceAccount> Accounts { get; init; } = new();
        public List<Currency> Currencies { get; init; } = new();
        public List<FinanceFlow> Flows { get; init; } = new();
        public List<AccountCash> Cash { get; init; } = new();
        public List<Holding> Holdings { get; init; } = new();
        public List<Trade> Trades { get; init; } = new();
        public List<Dividend> Dividends { get; init; } = new();
        public List<Deposit> Deposits { get; init; } = new();
        public List<Debt> Debts { get; init; } = new();
        public List<DebtPayment> DebtPayments { get; init; } = new();
        public List<FinanceTarget> Targets { get; init; } = new();
    }
}
