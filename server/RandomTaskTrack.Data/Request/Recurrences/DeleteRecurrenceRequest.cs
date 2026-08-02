using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Recurrences;

public class DeleteRecurrenceRequest : AuthenticatedRequest
{
    public Guid Id { get; set; }

    /// <summary>Also remove pending future instances. Completed history is
    /// never deleted.</summary>
    public bool DeleteFutureTasks { get; set; } = true;
}
