namespace RandomTaskTrack.Data.Models.ConfigurationOptions;

public class SchedulerOptions
{
    /// <summary>How far ahead recurring task instances are materialized.</summary>
    public int MaterializeHorizonDays { get; set; } = 21;

    /// <summary>How often the background materializer wakes up.</summary>
    public int MaterializeIntervalMinutes { get; set; } = 60;

    /// <summary>IANA id. Due dates are calendar dates, so "today" must be
    /// resolved in the user's zone, not the container's UTC.</summary>
    public string TimeZone { get; set; } = "Europe/Vilnius";
}
