using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Finance;

public class CreateHoldingRequest : AuthenticatedRequest
{
    /// <summary>In the price source vocabulary — Yahoo wants "AAPL", "ASML.AS".</summary>
    public string Symbol { get; set; } = "";

    public string? Name { get; set; }
    public string Currency { get; set; } = "";
}
