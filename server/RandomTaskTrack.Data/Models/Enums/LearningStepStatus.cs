namespace RandomTaskTrack.Data.Models.Enums;

/// <summary>
/// Matches ck_learn_steps_status. Dropped is kept rather than deleted so a
/// re-draft does not keep suggesting the thing that was already decided against.
/// </summary>
public enum LearningStepStatus
{
    Planned = 1,
    Doing = 2,
    Done = 3,
    Dropped = 4
}
