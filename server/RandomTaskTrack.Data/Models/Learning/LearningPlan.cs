namespace RandomTaskTrack.Data.Models.Learning;

/// <summary>
/// What the draft came back with. Stored whole as learn_goals.plan and handed
/// to the UI as-is: it is read, not computed with, and a prompt that learns to
/// return one more field should not be a migration.
///
/// Every list element carries the fields <see cref="LearningStep"/> needs, so
/// "add this to my path" is one press rather than a form to retype.
/// </summary>
public class LearningPlan
{
    /// <summary>Two or three sentences: the shape of the route, and how long it is.</summary>
    public string Summary { get; set; } = "";

    /// <summary>
    /// What "prepared" actually means, concretely enough to be tested against.
    /// This is the field that answers "what level of the language do I want" —
    /// a goal that cannot say when it is finished never finishes.
    /// </summary>
    public string TargetDefinition { get; set; } = "";

    /// <summary>What the draft assumed about the starting point. Wrong assumptions
    /// are the usual reason a plan is useless, so they are said out loud.</summary>
    public string AssumedLevel { get; set; } = "";

    /// <summary>The study hours a week the rest of the plan is paced against.</summary>
    public int WeeklyHours { get; set; }

    /// <summary>What has to be true before the path can start — "linear algebra to
    /// A-level standard" is where "get the maths up to par" lands.</summary>
    public List<string> Prerequisites { get; set; } = new();

    public List<LearningPhase> Phases { get; set; } = new();
    public List<LearningCertificationSuggestion> Certifications { get; set; } = new();
    public List<LearningResource> Resources { get; set; } = new();
    public List<LearningProject> Projects { get; set; } = new();

    /// <summary>Experience to go and collect that is not a project — a home lab,
    /// a CTF season, shadowing someone.</summary>
    public List<string> HandsOn { get; set; } = new();

    /// <summary>What usually derails this path. Read once, at the start.</summary>
    public List<string> Risks { get; set; } = new();
}

/// <summary>A block of the route, in order.</summary>
public class LearningPhase
{
    public string Title { get; set; } = "";

    /// <summary>Rough length. Weeks rather than dates: a path that slips slips whole.</summary>
    public int Weeks { get; set; }

    public string Focus { get; set; } = "";

    /// <summary>What you can do at the end of it that you could not at the start.</summary>
    public string Outcome { get; set; } = "";
}

/// <summary>An exam worth sitting on the way. Suggested, not committed.</summary>
public class LearningCertificationSuggestion
{
    public string Name { get; set; } = "";
    public string Issuer { get; set; } = "";

    /// <summary>"AZ-305", "SC-200". Empty when the credential has no exam code.</summary>
    public string Code { get; set; } = "";

    /// <summary>Where it sits in the sequence. 1 is first.</summary>
    public int Order { get; set; }

    /// <summary>Text, like everywhere else in this scope — "€165", "$300 + retake".</summary>
    public string TypicalCost { get; set; } = "";

    public int PrepHours { get; set; }

    /// <summary>Why this one and not the neighbouring one.</summary>
    public string Why { get; set; } = "";

    /// <summary>How long it lasts once earned, as the issuer states it. A heads-up
    /// only — what is actually tracked is learn_credentials once it is passed.</summary>
    public string Validity { get; set; } = "";
}

/// <summary>
/// A course, book, lab or app.
///
/// <see cref="Provider"/> plus an exact <see cref="Title"/> is the handle,
/// because that pair still finds the thing in a year. <see cref="Url"/> is
/// best-effort and the UI treats it as such: a model-supplied course link is a
/// dead end often enough that nothing should depend on it.
/// </summary>
public class LearningResource
{
    public string Title { get; set; } = "";

    /// <summary>course, book, lab, app, video, community, docs.</summary>
    public string Kind { get; set; } = "";

    public string Provider { get; set; } = "";
    public string Url { get; set; } = "";
    public string Cost { get; set; } = "";
    public string Why { get; set; } = "";

    /// <summary>Which phase it belongs to, 1-based. 0 when it spans the whole path.</summary>
    public int Phase { get; set; }
}

/// <summary>Something to build, pitched at a level.</summary>
public class LearningProject
{
    public string Title { get; set; } = "";

    /// <summary>beginner, intermediate, advanced.</summary>
    public string Level { get; set; } = "";

    /// <summary>What you actually build.</summary>
    public string Build { get; set; } = "";

    /// <summary>What having built it demonstrates — to an interviewer, or to yourself.</summary>
    public string Proves { get; set; } = "";
}
