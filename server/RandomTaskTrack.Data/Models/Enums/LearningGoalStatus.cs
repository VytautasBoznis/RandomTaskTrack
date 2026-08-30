namespace RandomTaskTrack.Data.Models.Enums;

/// <summary>
/// Matches ck_learn_goals_status. Parked rather than deleted, because a path
/// put down for a year is worth being able to pick back up with its plan and
/// its finished steps intact.
/// </summary>
public enum LearningGoalStatus
{
    Active = 1,
    Achieved = 2,
    Parked = 3
}
