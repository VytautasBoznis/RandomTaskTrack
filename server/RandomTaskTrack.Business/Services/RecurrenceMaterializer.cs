using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RandomTaskTrack.Data.Models.ConfigurationOptions;
using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Models.Tasks;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Recurrences;
using RandomTaskTrack.Interfaces.Repositories.Tasks;
using RandomTaskTrack.Interfaces.Services;

namespace RandomTaskTrack.Business.Services;

/// <summary>
/// Turns recurrence rules into dated task rows.
///
/// Materializing ahead of time (rather than computing the schedule on read)
/// keeps the dashboard a plain indexed query, and means a task can be edited or
/// annotated individually without special-casing "this one is virtual".
/// </summary>
public class RecurrenceMaterializer : IRecurrenceMaterializer
{
    private readonly IRecurrencesRepository _recurrencesRepository;
    private readonly ITasksRepository _tasksRepository;
    private readonly IClock _clock;
    private readonly SchedulerOptions _options;
    private readonly ILogger<RecurrenceMaterializer> _logger;

    public RecurrenceMaterializer(
        IRecurrencesRepository recurrencesRepository,
        ITasksRepository tasksRepository,
        IClock clock,
        IOptions<SchedulerOptions> options,
        ILogger<RecurrenceMaterializer> logger)
    {
        _recurrencesRepository = recurrencesRepository;
        _tasksRepository = tasksRepository;
        _clock = clock;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<int> MaterializeAllAsync(IUnitOfWork unitOfWork, CancellationToken cancellationToken)
    {
        DateOnly horizon = _clock.Today.AddDays(_options.MaterializeHorizonDays);
        List<TaskRecurrence> recurrences = await _recurrencesRepository.GetActiveAsync(horizon, unitOfWork);

        int created = 0;

        foreach (TaskRecurrence recurrence in recurrences)
        {
            cancellationToken.ThrowIfCancellationRequested();
            created += await MaterializeOneAsync(recurrence, unitOfWork, cancellationToken);
        }

        if (created > 0)
        {
            _logger.LogInformation("Materialized {Count} task(s) across {Recurrences} recurrence(s)", created, recurrences.Count);
        }

        return created;
    }

    public async Task<int> MaterializeOneAsync(TaskRecurrence recurrence, IUnitOfWork unitOfWork, CancellationToken cancellationToken)
    {
        DateOnly today = _clock.Today;
        DateOnly horizon = today.AddDays(_options.MaterializeHorizonDays);

        // A FromCompletion recurrence only ever has one open instance at a
        // time — the next one is spawned by CompleteTaskOperation, not here.
        // Materializing a whole horizon of them would defeat the point.
        if (recurrence.AnchorMode == RecurrenceAnchorMode.FromCompletion)
        {
            return await MaterializeNextForCompletionAnchoredAsync(recurrence, today, unitOfWork);
        }

        // Resume from the watermark, but never earlier than today — a
        // recurrence created months ago should not backfill history on first
        // run.
        DateOnly cursor = recurrence.LastDueOn.HasValue
            ? recurrence.LastDueOn.Value.AddDays(1)
            : recurrence.StartsOn;

        if (cursor < today)
        {
            cursor = today;
        }

        int created = 0;
        DateOnly? lastWritten = null;

        while (cursor <= horizon)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (recurrence.EndsOn.HasValue && cursor > recurrence.EndsOn.Value)
            {
                break;
            }

            if (Matches(recurrence, cursor))
            {
                if (await CreateInstanceAsync(recurrence, cursor, unitOfWork))
                {
                    created++;
                }

                lastWritten = cursor;
            }

            cursor = cursor.AddDays(1);
        }

        // Watermark to the horizon, not to the last match: the range up to the
        // horizon has been fully considered, so re-scanning it next run is
        // wasted work.
        DateOnly watermark = recurrence.EndsOn.HasValue && recurrence.EndsOn.Value < horizon
            ? recurrence.EndsOn.Value
            : horizon;

        if (lastWritten.HasValue || !recurrence.LastDueOn.HasValue || recurrence.LastDueOn.Value < watermark)
        {
            await _recurrencesRepository.UpdateLastDueOnAsync(recurrence.Id, watermark, unitOfWork);
        }

        return created;
    }

    public DateOnly? GetNextDueAfterCompletion(TaskRecurrence recurrence, DateOnly completedOn)
    {
        if (!recurrence.IsActive || recurrence.AnchorMode != RecurrenceAnchorMode.FromCompletion)
        {
            return null;
        }

        DateOnly? next = recurrence.RuleType switch
        {
            // The whole point of FromCompletion: the clock restarts when the
            // work actually happened. Clean the bathroom on day 9 of a 7-day
            // cycle and the next one is day 16, not day 14.
            RecurrenceRuleType.IntervalDays => completedOn.AddDays(recurrence.IntervalDays ?? 1),
            RecurrenceRuleType.DaysOfWeek => NextMatchingDayOfWeek(recurrence, completedOn),
            RecurrenceRuleType.DayOfMonth => NextMatchingDayOfMonth(recurrence, completedOn),
            _ => null
        };

        if (next.HasValue && recurrence.EndsOn.HasValue && next.Value > recurrence.EndsOn.Value)
        {
            return null;
        }

        return next;
    }

    private async Task<int> MaterializeNextForCompletionAnchoredAsync(TaskRecurrence recurrence, DateOnly today, IUnitOfWork unitOfWork)
    {
        // Seed the very first instance only. Everything after it is chained off
        // an actual completion.
        if (recurrence.LastDueOn.HasValue)
        {
            return 0;
        }

        DateOnly first = recurrence.StartsOn < today ? today : recurrence.StartsOn;

        if (recurrence.EndsOn.HasValue && first > recurrence.EndsOn.Value)
        {
            return 0;
        }

        bool created = await CreateInstanceAsync(recurrence, first, unitOfWork);
        await _recurrencesRepository.UpdateLastDueOnAsync(recurrence.Id, first, unitOfWork);

        return created ? 1 : 0;
    }

    private async Task<bool> CreateInstanceAsync(TaskRecurrence recurrence, DateOnly dueOn, IUnitOfWork unitOfWork)
    {
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            DomainId = recurrence.DomainId,
            RecurrenceId = recurrence.Id,
            Title = recurrence.Title,
            Notes = recurrence.Notes,
            Data = recurrence.Data,
            DueOn = dueOn,
            DueTime = recurrence.TimeOfDay,
            Status = TaskItemStatus.Pending
        };

        return await _tasksRepository.TryCreateFromRecurrenceAsync(task, unitOfWork);
    }

    private static bool Matches(TaskRecurrence recurrence, DateOnly date)
    {
        if (date < recurrence.StartsOn)
        {
            return false;
        }

        return recurrence.RuleType switch
        {
            RecurrenceRuleType.IntervalDays =>
                recurrence.IntervalDays is > 0 &&
                (date.DayNumber - recurrence.StartsOn.DayNumber) % recurrence.IntervalDays.Value == 0,

            RecurrenceRuleType.DaysOfWeek =>
                recurrence.DaysOfWeek?.Contains((int)date.DayOfWeek) == true,

            // Clamp to the month's length so a "31st" rule still fires in
            // February rather than silently skipping short months.
            RecurrenceRuleType.DayOfMonth =>
                recurrence.DayOfMonth.HasValue &&
                date.Day == Math.Min(recurrence.DayOfMonth.Value, DateTime.DaysInMonth(date.Year, date.Month)),

            _ => false
        };
    }

    private static DateOnly? NextMatchingDayOfWeek(TaskRecurrence recurrence, DateOnly after)
    {
        if (recurrence.DaysOfWeek is null || recurrence.DaysOfWeek.Length == 0)
        {
            return null;
        }

        for (int offset = 1; offset <= 7; offset++)
        {
            DateOnly candidate = after.AddDays(offset);

            if (recurrence.DaysOfWeek.Contains((int)candidate.DayOfWeek))
            {
                return candidate;
            }
        }

        return null;
    }

    private static DateOnly? NextMatchingDayOfMonth(TaskRecurrence recurrence, DateOnly after)
    {
        if (!recurrence.DayOfMonth.HasValue)
        {
            return null;
        }

        DateOnly cursor = after.AddDays(1);

        // At most two months of scanning: the target day always exists within
        // that window once clamped to the month length.
        for (int i = 0; i < 62; i++)
        {
            int clamped = Math.Min(recurrence.DayOfMonth.Value, DateTime.DaysInMonth(cursor.Year, cursor.Month));

            if (cursor.Day == clamped)
            {
                return cursor;
            }

            cursor = cursor.AddDays(1);
        }

        return null;
    }
}
