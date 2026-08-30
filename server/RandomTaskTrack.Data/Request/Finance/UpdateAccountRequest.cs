using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Finance;

public class UpdateAccountRequest : AuthenticatedRequest
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public AccountKind? Kind { get; set; }
    public string? Currency { get; set; }
    public string? Note { get; set; }
}
