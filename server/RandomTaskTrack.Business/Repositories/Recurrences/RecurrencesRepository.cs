using Dapper;
using RandomTaskTrack.Data.Dtos.Tasks;
using RandomTaskTrack.Data.Models.Tasks;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Recurrences;

namespace RandomTaskTrack.Business.Repositories.Recurrences;

public class RecurrencesRepository : IRecurrencesRepository
{
    private const string SelectRaw = @"
        SELECT id, domain_id, title, notes, data::text AS data,
               rule_type, interval_days, days_of_week, day_of_month, anchor_mode,
               time_of_day, starts_on, ends_on, is_active, last_due_on,
               created_at, updated_at
        FROM tracker.task_recurrences";

    private const string SelectListItem = @"
        SELECT r.id, r.domain_id, d.code AS domain_code, r.title, r.notes,
               r.data::text AS data, r.rule_type, r.interval_days, r.days_of_week,
               r.day_of_month, r.anchor_mode, r.time_of_day, r.starts_on,
               r.ends_on, r.is_active, r.last_due_on
        FROM tracker.task_recurrences r
        INNER JOIN tracker.task_domains d ON d.id = r.domain_id";

    public async Task<List<RecurrenceListItemDto>> QueryAsync(int? domainId, bool includeInactive, IUnitOfWork unitOfWork)
    {
        var rows = await unitOfWork.Connection.QueryAsync<RecurrenceListItemDto>(
            $@"{SelectListItem}
               WHERE (@includeInactive OR r.is_active)
                 AND (@domainId::int IS NULL OR r.domain_id = @domainId)
               ORDER BY d.sort_order, r.title",
            new { domainId, includeInactive },
            unitOfWork.Transaction);

        return rows.ToList();
    }

    public async Task<RecurrenceListItemDto?> GetByIdAsync(Guid id, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<RecurrenceListItemDto>(
            $"{SelectListItem} WHERE r.id = @id",
            new { id },
            unitOfWork.Transaction);
    }

    public async Task<TaskRecurrence?> GetRawByIdAsync(Guid id, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<TaskRecurrence>(
            $"{SelectRaw} WHERE id = @id",
            new { id },
            unitOfWork.Transaction);
    }

    public async Task<List<TaskRecurrence>> GetActiveAsync(DateOnly onOrBefore, IUnitOfWork unitOfWork)
    {
        var rows = await unitOfWork.Connection.QueryAsync<TaskRecurrence>(
            $@"{SelectRaw}
               WHERE is_active
                 AND starts_on <= @onOrBefore
                 AND (ends_on IS NULL OR ends_on >= @onOrBefore - 1)
               ORDER BY created_at",
            new { onOrBefore },
            unitOfWork.Transaction);

        return rows.ToList();
    }

    public async Task CreateAsync(TaskRecurrence recurrence, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            @"INSERT INTO tracker.task_recurrences
                  (id, domain_id, title, notes, data, rule_type, interval_days,
                   days_of_week, day_of_month, anchor_mode, time_of_day,
                   starts_on, ends_on, is_active)
              VALUES
                  (@Id, @DomainId, @Title, @Notes, @Data::jsonb, @RuleType, @IntervalDays,
                   @DaysOfWeek, @DayOfMonth, @AnchorMode, @TimeOfDay,
                   @StartsOn, @EndsOn, @IsActive)",
            new
            {
                recurrence.Id,
                recurrence.DomainId,
                recurrence.Title,
                recurrence.Notes,
                recurrence.Data,
                RuleType = (int)recurrence.RuleType,
                recurrence.IntervalDays,
                recurrence.DaysOfWeek,
                recurrence.DayOfMonth,
                AnchorMode = (int)recurrence.AnchorMode,
                recurrence.TimeOfDay,
                recurrence.StartsOn,
                recurrence.EndsOn,
                recurrence.IsActive
            },
            unitOfWork.Transaction);
    }

    public async Task UpdateAsync(TaskRecurrence recurrence, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            @"UPDATE tracker.task_recurrences
              SET title         = @Title,
                  notes         = @Notes,
                  data          = @Data::jsonb,
                  rule_type     = @RuleType,
                  interval_days = @IntervalDays,
                  days_of_week  = @DaysOfWeek,
                  day_of_month  = @DayOfMonth,
                  anchor_mode   = @AnchorMode,
                  time_of_day   = @TimeOfDay,
                  ends_on       = @EndsOn,
                  is_active     = @IsActive,
                  updated_at    = now()
              WHERE id = @Id",
            new
            {
                recurrence.Id,
                recurrence.Title,
                recurrence.Notes,
                recurrence.Data,
                RuleType = (int)recurrence.RuleType,
                recurrence.IntervalDays,
                recurrence.DaysOfWeek,
                recurrence.DayOfMonth,
                AnchorMode = (int)recurrence.AnchorMode,
                recurrence.TimeOfDay,
                recurrence.EndsOn,
                recurrence.IsActive
            },
            unitOfWork.Transaction);
    }

    public async Task UpdateLastDueOnAsync(Guid id, DateOnly lastDueOn, IUnitOfWork unitOfWork)
    {
        // GREATEST guards against an out-of-order materializer run walking the
        // watermark backwards.
        await unitOfWork.Connection.ExecuteAsync(
            @"UPDATE tracker.task_recurrences
              SET last_due_on = GREATEST(COALESCE(last_due_on, @lastDueOn), @lastDueOn),
                  updated_at  = now()
              WHERE id = @id",
            new { id, lastDueOn },
            unitOfWork.Transaction);
    }

    public async Task<bool> DeleteAsync(Guid id, IUnitOfWork unitOfWork)
    {
        int affected = await unitOfWork.Connection.ExecuteAsync(
            "DELETE FROM tracker.task_recurrences WHERE id = @id",
            new { id },
            unitOfWork.Transaction);

        return affected > 0;
    }
}
