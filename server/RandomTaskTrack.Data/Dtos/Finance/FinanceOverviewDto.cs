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
    /// Every ledger entry ever, netted and converted. This is the derived
    /// "what is in the account right now" the whole projection starts from.
    /// </summary>
    public decimal CashBase { get; set; }

    /// <summary>Deposits at their value today, principal plus interest accrued so far.</summary>
    public decimal DepositsBase { get; set; }

    /// <summary>Positions at the last pulled price. Excludes holdings never priced.</summary>
    public decimal StocksBase { get; set; }

    public decimal NetWorthBase { get; set; }

    /// <summary>What the active flows say a typical month costs and earns.</summary>
    public decimal MonthlyIncomeBase { get; set; }

    public decimal MonthlyExpenseBase { get; set; }

    /// <summary>True when a holding has no price yet, so the UI can say the total is short.</summary>
    public bool HasUnpricedHoldings { get; set; }

    public List<FinanceFlow> Flows { get; set; } = new();
    public List<PositionDto> Positions { get; set; } = new();
    public List<Deposit> Deposits { get; set; } = new();
    public List<Dividend> Dividends { get; set; } = new();
    public List<FinanceTarget> Targets { get; set; } = new();
    public List<Currency> Currencies { get; set; } = new();
}
