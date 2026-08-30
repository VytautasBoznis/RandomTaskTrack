namespace RandomTaskTrack.Data.Models.Learning;

/// <summary>What the draft is asked. Everything the goal already knows about itself.</summary>
public class LearningPlanQuestion
{
    public string Title { get; set; } = "";

    /// <summary>The motivation, verbatim. A path drafted for "I want the promotion"
    /// is not the path drafted for "I want to stop being bored".</summary>
    public string Why { get; set; } = "";

    public string Benefits { get; set; } = "";

    /// <summary>The "prepared by" date, when there is one. What paces the phases.</summary>
    public DateOnly? TargetOn { get; set; }

    /// <summary>Today, so "18 months" can be turned into phases that fit the target.</summary>
    public DateOnly Today { get; set; }

    /// <summary>Current level, hours a week, constraints.</summary>
    public string Context { get; set; } = "";

    /// <summary>
    /// What is already held. Passed so the draft does not spend a phase on a
    /// certification that is already on the wall — the single most useless thing
    /// a plan can do.
    /// </summary>
    public List<string> HeldCredentials { get; set; } = new();
}
