namespace RandomTaskTrack.Data.Models.Enums;

/// <summary>
/// How often a flow or a dividend repeats. Deliberately not
/// <see cref="RecurrenceRuleType"/>: that one is task-shaped (days-of-week
/// arrays, an anchor mode defined in terms of completion) and money is not.
/// </summary>
public enum FinanceCadence : int
{
    Weekly = 1,
    Monthly = 2,
    Quarterly = 3,
    Yearly = 4
}
