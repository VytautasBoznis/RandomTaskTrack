namespace RandomTaskTrack.Data.Models.Enums;

/// <summary>
/// Decides where the next occurrence is measured from when a task is completed
/// late. FromSchedule keeps the original cadence; FromCompletion resets the
/// clock to when the work actually happened.
/// </summary>
public enum RecurrenceAnchorMode : int
{
    FromSchedule = 1,
    FromCompletion = 2
}
