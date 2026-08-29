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

        decimal cash = data.Cash.Sum(c => ToBase(c.Amount, c.Currency, data));
        decimal deposits = StillHeld(data.Deposits, today).Sum(d => ToBase(ValueAt(d, today), d.Currency, data));
        decimal stocks = positions.Sum(p => p.MarketValueBase ?? 0m);

        return new FinanceOverviewDto
        {
            Today = today,
            BaseCurrency = _options.BaseCurrency,
            CashBase = Round(cash),
            DepositsBase = Round(deposits),
            StocksBase = Round(stocks),

            // Summed from the rounded parts, not rounded from the raw sum.
            // Otherwise the total is a cent off what the three numbers beside it
            // add up to, and a total that visibly does not add up is a total
            // nobody trusts.
            NetWorthBase = Round(cash) + Round(deposits) + Round(stocks),
            MonthlyIncomeBase = Round(MonthlyRate(data, FinanceFlowKind.Income)),
            MonthlyExpenseBase = Round(MonthlyRate(data, FinanceFlowKind.Expense)),

            // A holding with no price contributes nothing, which would quietly
            // understate the total. The UI says so rather than showing a number
            // that is wrong by an unknown amount.
            HasUnpricedHoldings = positions.Any(p => p.Quantity > 0 && p.LastPrice is null),

            Flows = data.Flows,
            Positions = positions,
            Deposits = data.Deposits,
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

        decimal cash = data.Cash.Sum(c => ToBase(c.Amount, c.Currency, data));
        decimal stocksNow = positions.Sum(p => p.MarketValueBase ?? 0m);

        decimal monthToDateIncome = actuals.Where(a => a.Month == thisMonth).Sum(a => ToBase(a.Income, a.Currency, data));
        decimal monthToDateExpenses = actuals.Where(a => a.Month == thisMonth).Sum(a => ToBase(a.Expenses, a.Currency, data));
        decimal depositsNow = StillHeld(data.Deposits, today).Sum(d => ToBase(ValueAt(d, today), d.Currency, data));

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
            NetWorth = Round(cash) + Round(depositsNow) + Round(stocksNow)
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

            // A deposit maturing is the one moment its money crosses back into
            // cash. Only future maturities count: a deposit that matured before
            // today is already in the balance.
            decimal maturing = data.Deposits
                .Where(d => d.MaturesOn.HasValue && d.MaturesOn.Value >= month && d.MaturesOn.Value <= monthEnd)
                .Sum(d => ToBase(ValueAt(d, d.MaturesOn!.Value), d.Currency, data));

            runningCash += income - expenses + maturing;

            decimal deposits = StillHeld(data.Deposits, monthEnd)
                .Sum(d => ToBase(ValueAt(d, monthEnd), d.Currency, data));

            // Growth is applied to the portfolio as a whole from today, rather
            // than per holding, because the assumption is one number the user
            // typed — pretending it is per-stock would be false precision.
            decimal stocks = stocksNow * (decimal)Math.Pow(1 + (double)stockGrowthPct / 100.0, i / 12.0);

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
                NetWorth = Round(runningCash) + Round(deposits) + Round(stocks)
            });
        }

        return points;
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

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    // ── Loading ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Everything, in one pass. Both entry points need all of it, and loading
    /// per-holding trades would be N+1 on a page that always shows every
    /// holding.
    /// </summary>
    private async Task<FinanceData> LoadAsync(IUnitOfWork unitOfWork) => new()
    {
        Currencies = await _repository.GetCurrenciesAsync(unitOfWork),
        Flows = await _repository.GetFlowsAsync(true, unitOfWork),
        Cash = await _repository.GetCashByCurrencyAsync(unitOfWork),
        Holdings = await _repository.GetHoldingsAsync(unitOfWork),
        Trades = await _repository.GetTradesAsync(unitOfWork),
        Dividends = await _repository.GetDividendsAsync(true, unitOfWork),
        Deposits = await _repository.GetDepositsAsync(unitOfWork),
        Targets = await _repository.GetTargetsAsync(unitOfWork)
    };

    private class FinanceData
    {
        public List<Currency> Currencies { get; init; } = new();
        public List<FinanceFlow> Flows { get; init; } = new();
        public List<CurrencyAmount> Cash { get; init; } = new();
        public List<Holding> Holdings { get; init; } = new();
        public List<Trade> Trades { get; init; } = new();
        public List<Dividend> Dividends { get; init; } = new();
        public List<Deposit> Deposits { get; init; } = new();
        public List<FinanceTarget> Targets { get; init; } = new();
    }
}
