using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Finance;

public class CreateEntryRequest : AuthenticatedRequest
{
    /// <summary>The recurring definition this is an instance of, when it is one.</summary>
    public Guid? FlowId { get; set; }

    /// <summary>Which account the money moved in or out of.</summary>
    public Guid AccountId { get; set; }

    public FinanceFlowKind Kind { get; set; }
    public string Name { get; set; } = "";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "";
    public DateOnly OccurredOn { get; set; }
    public string? Category { get; set; }
    public string? Note { get; set; }
}
