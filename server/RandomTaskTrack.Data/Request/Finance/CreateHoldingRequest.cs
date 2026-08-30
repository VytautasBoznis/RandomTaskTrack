using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Finance;

public class CreateHoldingRequest : AuthenticatedRequest
{
    /// <summary>The account the shares are held in.</summary>
    public Guid AccountId { get; set; }

    /// <summary>In the price source vocabulary — Yahoo wants "AAPL", "ASML.AS".</summary>
    public string Symbol { get; set; } = "";

    public string? Name { get; set; }
    public string Currency { get; set; } = "";
}
