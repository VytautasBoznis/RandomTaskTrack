namespace RandomTaskTrack.Interfaces.Services;

/// <summary>
/// Due dates are calendar dates, so "today" has to be resolved in the user's
/// timezone rather than the container's UTC — otherwise everything rolls over
/// at the wrong hour. Injected so it can be frozen in tests.
/// </summary>
public interface IClock
{
    DateTime UtcNow { get; }
    DateOnly Today { get; }
    TimeZoneInfo TimeZone { get; }
}
