using Microsoft.Extensions.Options;
using RandomTaskTrack.Data.Models.ConfigurationOptions;
using RandomTaskTrack.Interfaces.Services;

namespace RandomTaskTrack.Business.Services;

public class Clock : IClock
{
    private readonly TimeZoneInfo _timeZone;

    public Clock(IOptions<SchedulerOptions> options)
    {
        string id = options?.Value?.TimeZone ?? "UTC";

        try
        {
            _timeZone = TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            // A bad timezone id should not take the app down — everything still
            // works on UTC, dates just roll over at the wrong local hour.
            Serilog.Log.Warning("Unknown timezone '{TimeZone}', falling back to UTC.", id);
            _timeZone = TimeZoneInfo.Utc;
        }
    }

    public DateTime UtcNow => DateTime.UtcNow;

    public TimeZoneInfo TimeZone => _timeZone;

    /// <summary>
    /// "Today" in the configured zone. Due dates are calendar dates, so this
    /// must not be UTC-derived — a task due Monday should stop being "today" at
    /// local midnight, not at 02:00 or 03:00 local.
    /// </summary>
    public DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZone));
}
