using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Learning;

/// <summary>
/// Puts a step on the board as a dated task, so it turns up on Today next to
/// everything else and lands in the completion log like everything else.
///
/// One-off and dated, never a recurrence: a step is done once. Studying that
/// repeats is a recurrence the user makes on the Recurring tab, which is the
/// right place for "an hour of Spanish every weekday".
/// </summary>
public class CreateLearningStepTaskRequest : AuthenticatedRequest
{
    public Guid Id { get; set; }

    /// <summary>Defaults to the step's target date, or today when it has none.</summary>
    public DateOnly? DueOn { get; set; }
}
