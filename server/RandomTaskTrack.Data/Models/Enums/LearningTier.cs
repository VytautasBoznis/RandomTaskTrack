namespace RandomTaskTrack.Data.Models.Enums;

/// <summary>
/// How much a path is allowed to compete for time. Matches ck_learn_goals_tier.
/// Four fixed rungs rather than a free sort order: the point is to be forced to
/// say that the language course is below the promotion, not to let five things
/// all be first.
/// </summary>
public enum LearningTier
{
    Primary = 1,
    Secondary = 2,
    Tertiary = 3,
    NiceToHave = 4
}
