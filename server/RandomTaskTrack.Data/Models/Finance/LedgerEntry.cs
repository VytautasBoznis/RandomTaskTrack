using RandomTaskTrack.Data.Models.Enums;

namespace RandomTaskTrack.Data.Models.Finance;

/// <summary>
/// One movement of cash that really happened. The ledger is cash only —
/// deposits and holdings are assets valued separately, and their past cash
/// movements are already reflected in the balance these entries produce.
/// </summary>
public class LedgerEntry
{
    public Guid Id { get; set; }

    /// <summary>
    /// The recurring definition this was an instance of, when it was one.
    /// Nulled rather than deleted with the flow: the money still moved.
    /// </summary>
    public Guid? FlowId { get; set; }

    public FinanceFlowKind Kind { get; set; }
    public string Name { get; set; } = "";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "";
    public DateOnly OccurredOn { get; set; }
    public string? Category { get; set; }
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }
}
