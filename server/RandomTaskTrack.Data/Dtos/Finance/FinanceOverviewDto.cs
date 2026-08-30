using RandomTaskTrack.Data.Models.Finance;

namespace RandomTaskTrack.Data.Dtos.Finance;

/// <summary>
/// Everything the Finance tab renders, in one round trip — the same bargain
/// <see cref="Tasks.DashboardDto"/> makes with the dashboard.
/// </summary>
public class FinanceOverviewDto
{
    public DateOnly Today { get; set; }
    public string BaseCurrency { get; set; } = "";

    /// <summary>
    /// What is in the accounts right now: every ledger entry ever, netted and
    /// converted, plus the money the deposits have moved between them. Equal to
    /// the sum of <see cref="AccountDto.BalanceBase"/>, and the figure the whole
    /// projection starts from.
    /// </summary>
    public decimal CashBase { get; set; }

    /// <summary>Deposits at their value today, principal plus interest accrued so far.</summary>
    public decimal DepositsBase { get; set; }

    /// <summary>Positions at the last pulled price. Excludes holdings never priced.</summary>
    public decimal StocksBase { get; set; }

    /// <summary>
    /// What the debts bought, held flat. Only counts debts that have started —
    /// a mortgage you sign next year has not got you a flat yet.
    /// </summary>
    public decimal AssetsBase { get; set; }

    /// <summary>What is still owed across every debt, amortised to today.</summary>
    public decimal DebtsBase { get; set; }

    /// <summary>Cash + deposits + holdings + assets − debts.</summary>
    public decimal NetWorthBase { get; set; }

    /// <summary>
    /// What the active flows say a typical month costs and earns. The expense
    /// side includes the payment on every debt still running, because a
    /// mortgage payment is an expense whether or not it is also a flow.
    /// </summary>
    public decimal MonthlyIncomeBase { get; set; }

    public decimal MonthlyExpenseBase { get; set; }

    /// <summary>True when a holding has no price yet, so the UI can say the total is short.</summary>
    public bool HasUnpricedHoldings { get; set; }

    public List<AccountDto> Accounts { get; set; } = new();
    public List<FinanceFlow> Flows { get; set; } = new();
    public List<PositionDto> Positions { get; set; } = new();
    public List<Deposit> Deposits { get; set; } = new();
    public List<DebtDto> Debts { get; set; } = new();
    public List<Dividend> Dividends { get; set; } = new();
    public List<FinanceTarget> Targets { get; set; } = new();
    public List<Currency> Currencies { get; set; } = new();
}
