using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Finance;

public class CreateAccountRequest : AuthenticatedRequest
{
    public string Name { get; set; } = "";

    /// <summary>Cash for a bank account, Stock for a brokerage or pension.</summary>
    public AccountKind Kind { get; set; }

    /// <summary>The currency the balance is quoted in.</summary>
    public string Currency { get; set; } = "";

    public string? Note { get; set; }
}
