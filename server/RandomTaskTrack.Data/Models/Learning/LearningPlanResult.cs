namespace RandomTaskTrack.Data.Models.Learning;

public class LearningPlanResult
{
    public LearningPlan Plan { get; set; } = new();

    /// <summary>Which model drafted it.</summary>
    public string? Model { get; set; }
}
