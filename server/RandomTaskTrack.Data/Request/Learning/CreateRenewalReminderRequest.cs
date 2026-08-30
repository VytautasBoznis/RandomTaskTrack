using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Learning;

/// <summary>
/// Puts a dated renewal reminder on the board. Only meaningful for a credential
/// that actually expires; asking for one on a permanent credential is rejected
/// rather than quietly scheduling nothing.
/// </summary>
public class CreateRenewalReminderRequest : AuthenticatedRequest
{
    public Guid Id { get; set; }

    /// <summary>
    /// Defaults to the day the renewal window opens, from the looked-up
    /// WindowOpensDays — or to 60 days before expiry when nothing better is
    /// known, which is long enough to book an exam.
    /// </summary>
    public DateOnly? DueOn { get; set; }
}
