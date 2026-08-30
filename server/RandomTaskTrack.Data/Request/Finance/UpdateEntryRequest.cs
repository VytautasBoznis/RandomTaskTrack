using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Finance;

public class UpdateEntryRequest : AuthenticatedRequest
{
    public Guid Id { get; set; }

    /// <summary>Moves the entry to another account, and both balances with it.</summary>
    public Guid? AccountId { get; set; }

    public string? Name { get; set; }
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }
    public DateOnly? OccurredOn { get; set; }
    public string? Category { get; set; }
    public string? Note { get; set; }
}
