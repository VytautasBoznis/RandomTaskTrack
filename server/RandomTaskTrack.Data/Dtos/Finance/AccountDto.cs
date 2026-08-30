using RandomTaskTrack.Data.Models.Enums;

namespace RandomTaskTrack.Data.Dtos.Finance;

/// <summary>
/// An account with everything sitting in it folded in. Nothing here is stored:
/// the balance is the ledger plus the deposits that moved money in or out of
/// it, and the holdings figure is the positions assigned to it at their last
/// pulled price. Correcting an entry corrects the balance for free, which is
/// the whole reason there is no balance column.
/// </summary>
public class AccountDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public AccountKind Kind { get; set; }
    public string Currency { get; set; } = "";
    public string? Note { get; set; }

    /// <summary>
    /// The balance in the account's own currency — what the bank app would say.
    /// </summary>
    public decimal Balance { get; set; }

    /// <summary>The same balance in the base currency, for the totals.</summary>
    public decimal BalanceBase { get; set; }

    /// <summary>Market value of the positions held here, in base. Zero for a bank account.</summary>
    public decimal HoldingsBase { get; set; }

    /// <summary>Balance plus holdings: what the account is worth altogether.</summary>
    public decimal ValueBase { get; set; }

    /// <summary>
    /// Money on its way back: deposits that will land here, at what they will
    /// be worth on the day they mature. Not part of <see cref="ValueBase"/> —
    /// it is still in the deposit, not in the account.
    /// </summary>
    public decimal MaturingBase { get; set; }

    /// <summary>The soonest of those maturity dates, so a card can say when.</summary>
    public DateOnly? NextMaturityOn { get; set; }
}
