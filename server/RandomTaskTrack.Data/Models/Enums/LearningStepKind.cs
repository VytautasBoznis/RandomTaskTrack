namespace RandomTaskTrack.Data.Models.Enums;

/// <summary>
/// What kind of thing a step is. Matches ck_learn_steps_kind.
///
/// This is what lets one table carry an exam, a Udemy course and a university
/// assignment without three tables: the kind decides the badge on the row and
/// which fields the form shows, and nothing else branches on it.
/// </summary>
public enum LearningStepKind
{
    /// <summary>Read, watch, practise. No artefact at the end.</summary>
    Study = 1,

    /// <summary>An exam to sit.</summary>
    Certification = 2,

    /// <summary>Something built, to have built it.</summary>
    Project = 3,

    /// <summary>A named course on a named platform.</summary>
    Course = 4,

    /// <summary>Coursework with a due date and a mark.</summary>
    Assignment = 5,

    /// <summary>A licence with an issuing authority — the FPV A2, a driving test.</summary>
    Licence = 6,

    /// <summary>A marker on the path that is not itself work.</summary>
    Milestone = 7
}
