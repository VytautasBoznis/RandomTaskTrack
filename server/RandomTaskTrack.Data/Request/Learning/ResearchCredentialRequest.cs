using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Learning;

/// <summary>
/// Looks up how this credential renews. Reports failure as failure — the button
/// does exactly one thing, so silently doing nothing would be a lie.
/// </summary>
public class ResearchCredentialRequest : AuthenticatedRequest
{
    public Guid Id { get; set; }
}
