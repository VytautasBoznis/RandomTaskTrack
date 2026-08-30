using RandomTaskTrack.Data.Models.Enums;

namespace RandomTaskTrack.Data.Models.Finance;

/// <summary>
/// A pot the money sits in. There is deliberately no balance here: the balance
/// is still derived from the ledger, and <see cref="Dtos.Finance.AccountDto"/>
/// is what carries the computed figure.
/// </summary>
public class FinanceAccount
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public AccountKind Kind { get; set; }

    /// <summary>
    /// The currency the balance is quoted in. Entries against the account may
    /// be in any currency; the balance converts them.
    /// </summary>
    public string Currency { get; set; } = "";

    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
