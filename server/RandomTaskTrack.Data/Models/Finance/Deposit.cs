using RandomTaskTrack.Data.Models.Enums;

namespace RandomTaskTrack.Data.Models.Finance;

public class Deposit
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Principal { get; set; }
    public string Currency { get; set; } = "";

    /// <summary>
    /// A percentage as written on the bank's page: 4.25 means 4.25%, not
    /// 0.0425. One less place to drop a factor of 100.
    /// </summary>
    public decimal AnnualRate { get; set; }

    public DepositCompounding Compounding { get; set; }
    public DateOnly OpenedOn { get; set; }

    /// <summary>
    /// Null is an open-ended savings account: it keeps accruing and never
    /// returns to cash on its own.
    /// </summary>
    public DateOnly? MaturesOn { get; set; }

    /// <summary>
    /// The account the principal came out of, subtracted from its balance for
    /// as long as the deposit is open. Null for deposits opened before accounts
    /// existed, whose transfer was logged as an entry by hand — attaching an
    /// account to one of those would subtract the same money twice.
    /// </summary>
    public Guid? SourceAccountId { get; set; }

    /// <summary>
    /// Where principal plus interest lands once <see cref="MaturesOn"/> has
    /// passed. Defaults to the source on write, so money never leaves an
    /// account with nowhere to come back to.
    /// </summary>
    public Guid? TargetAccountId { get; set; }

    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
