namespace RandomTaskTrack.Data.Models.Plants;

/// <summary>
/// One line of the suggested care schedule, and the shape the UI sends back to
/// turn it into a recurrence.
/// </summary>
public class PlantCareTask
{
    public string Title { get; set; } = "";

    /// <summary>Days between doings. Everything a plant needs is an interval.</summary>
    public int IntervalDays { get; set; }

    /// <summary>Seasonal caveats — "half as often from November". Carried onto
    /// the recurrence, so it lands on every task the schedule spawns.</summary>
    public string Notes { get; set; } = "";
}
