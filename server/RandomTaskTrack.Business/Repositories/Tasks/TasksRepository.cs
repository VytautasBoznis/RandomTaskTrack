using System.Text;
using Dapper;
using RandomTaskTrack.Data.Dtos.Tasks;
using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Models.Tasks;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Tasks;

namespace RandomTaskTrack.Business.Repositories.Tasks;

public class TasksRepository : ITasksRepository
{
    // Joined once here so every dashboard bucket returns the same shape and the
    // UI never has to resolve a domain id against a second call.
    private const string SelectListItem = @"
        SELECT t.id,
               t.domain_id,
               d.code   AS domain_code,
               d.name   AS domain_name,
               t.recurrence_id,
               t.title,
               t.notes,
               t.data::text AS data,
               t.due_on,
               t.due_time,
               t.status,
               t.completed_at
        FROM tracker.task_tasks t
        INNER JOIN tracker.task_domains d ON d.id = t.domain_id";

    public async Task<TaskListItemDto?> GetByIdAsync(Guid id, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<TaskListItemDto>(
            $"{SelectListItem} WHERE t.id = @id",
            new { id },
            unitOfWork.Transaction);
    }

    public async Task<TaskItem?> GetRawByIdAsync(Guid id, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<TaskItem>(
            @"SELECT id, domain_id, recurrence_id, title, notes, data::text AS data,
                     due_on, due_time, status, completed_at, created_at, updated_at
              FROM tracker.task_tasks
              WHERE id = @id",
            new { id },
            unitOfWork.Transaction);
    }

    public async Task<List<TaskListItemDto>> QueryAsync(
        int? domainId,
        DateOnly? fromDate,
        DateOnly? toDate,
        TaskItemStatus? status,
        string? search,
        int page,
        int pageSize,
        IUnitOfWork unitOfWork)
    {
        var sql = new StringBuilder(SelectListItem);
        sql.Append(BuildWhere(domainId, fromDate, toDate, status, search));
        sql.Append(" ORDER BY t.due_on, t.due_time NULLS LAST, t.title LIMIT @limit OFFSET @offset");

        var rows = await unitOfWork.Connection.QueryAsync<TaskListItemDto>(
            sql.ToString(),
            BuildParameters(domainId, fromDate, toDate, status, search, pageSize, (page - 1) * pageSize),
            unitOfWork.Transaction);

        return rows.ToList();
    }

    public async Task<int> CountAsync(
        int? domainId,
        DateOnly? fromDate,
        DateOnly? toDate,
        TaskItemStatus? status,
        string? search,
        IUnitOfWork unitOfWork)
    {
        var sql = new StringBuilder(
            @"SELECT count(*)
              FROM tracker.task_tasks t
              INNER JOIN tracker.task_domains d ON d.id = t.domain_id");
        sql.Append(BuildWhere(domainId, fromDate, toDate, status, search));

        return await unitOfWork.Connection.ExecuteScalarAsync<int>(
            sql.ToString(),
            BuildParameters(domainId, fromDate, toDate, status, search, null, null),
            unitOfWork.Transaction);
    }

    public async Task CreateAsync(TaskItem task, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            @"INSERT INTO tracker.task_tasks
                  (id, domain_id, recurrence_id, title, notes, data, due_on, due_time, status)
              VALUES
                  (@Id, @DomainId, @RecurrenceId, @Title, @Notes, @Data::jsonb, @DueOn, @DueTime, @Status)",
            new
            {
                task.Id,
                task.DomainId,
                task.RecurrenceId,
                task.Title,
                task.Notes,
                task.Data,
                task.DueOn,
                task.DueTime,
                Status = (int)task.Status
            },
            unitOfWork.Transaction);
    }

    public async Task<bool> TryCreateFromRecurrenceAsync(TaskItem task, IUnitOfWork unitOfWork)
    {
        // ux_task_tasks_recurrence_due makes the conflict clause a no-op when
        // this instance already exists, which is what lets the materializer run
        // on a timer, on demand, and concurrently without guarding.
        int affected = await unitOfWork.Connection.ExecuteAsync(
            @"INSERT INTO tracker.task_tasks
                  (id, domain_id, recurrence_id, title, notes, data, due_on, due_time, status)
              VALUES
                  (@Id, @DomainId, @RecurrenceId, @Title, @Notes, @Data::jsonb, @DueOn, @DueTime, @Status)
              ON CONFLICT (recurrence_id, due_on) WHERE recurrence_id IS NOT NULL DO NOTHING",
            new
            {
                task.Id,
                task.DomainId,
                task.RecurrenceId,
                task.Title,
                task.Notes,
                task.Data,
                task.DueOn,
                task.DueTime,
                Status = (int)task.Status
            },
            unitOfWork.Transaction);

        return affected > 0;
    }

    public async Task UpdateAsync(TaskItem task, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            @"UPDATE tracker.task_tasks
              SET domain_id = @DomainId,
                  title     = @Title,
                  notes     = @Notes,
                  data      = @Data::jsonb,
                  due_on    = @DueOn,
                  due_time  = @DueTime,
                  updated_at = now()
              WHERE id = @Id",
            new
            {
                task.Id,
                task.DomainId,
                task.Title,
                task.Notes,
                task.Data,
                task.DueOn,
                task.DueTime
            },
            unitOfWork.Transaction);
    }

    public async Task MarkCompletedAsync(Guid id, TaskItemStatus status, DateTime completedAt, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            @"UPDATE tracker.task_tasks
              SET status = @status, completed_at = @completedAt, updated_at = now()
              WHERE id = @id",
            new { id, status = (int)status, completedAt },
            unitOfWork.Transaction);
    }

    public async Task<bool> DeleteAsync(Guid id, IUnitOfWork unitOfWork)
    {
        int affected = await unitOfWork.Connection.ExecuteAsync(
            "DELETE FROM tracker.task_tasks WHERE id = @id",
            new { id },
            unitOfWork.Transaction);

        return affected > 0;
    }

    public async Task<int> DeletePendingByRecurrenceAsync(Guid recurrenceId, DateOnly fromDate, IUnitOfWork unitOfWork)
    {
        // Only pending instances. Anything already done or skipped is history
        // and stays put even when the recurrence itself is deleted.
        return await unitOfWork.Connection.ExecuteAsync(
            @"DELETE FROM tracker.task_tasks
              WHERE recurrence_id = @recurrenceId
                AND due_on >= @fromDate
                AND status = @pending",
            new { recurrenceId, fromDate, pending = (int)TaskItemStatus.Pending },
            unitOfWork.Transaction);
    }

    // ── Dashboard buckets ────────────────────────────────────────────────────

    public async Task<List<TaskListItemDto>> GetOverdueAsync(DateOnly today, IUnitOfWork unitOfWork)
    {
        var rows = await unitOfWork.Connection.QueryAsync<TaskListItemDto>(
            $@"{SelectListItem}
               WHERE t.status = @pending AND t.due_on < @today
               ORDER BY t.due_on, t.due_time NULLS LAST",
            new { today, pending = (int)TaskItemStatus.Pending },
            unitOfWork.Transaction);

        return rows.ToList();
    }

    public async Task<List<TaskListItemDto>> GetDueOnAsync(DateOnly date, IUnitOfWork unitOfWork)
    {
        var rows = await unitOfWork.Connection.QueryAsync<TaskListItemDto>(
            $@"{SelectListItem}
               WHERE t.status = @pending AND t.due_on = @date
               ORDER BY t.due_time NULLS LAST, d.sort_order, t.title",
            new { date, pending = (int)TaskItemStatus.Pending },
            unitOfWork.Transaction);

        return rows.ToList();
    }

    public async Task<List<TaskListItemDto>> GetUpcomingAsync(DateOnly fromExclusive, DateOnly toInclusive, IUnitOfWork unitOfWork)
    {
        var rows = await unitOfWork.Connection.QueryAsync<TaskListItemDto>(
            $@"{SelectListItem}
               WHERE t.status = @pending AND t.due_on > @fromExclusive AND t.due_on <= @toInclusive
               ORDER BY t.due_on, t.due_time NULLS LAST, d.sort_order",
            new { fromExclusive, toInclusive, pending = (int)TaskItemStatus.Pending },
            unitOfWork.Transaction);

        return rows.ToList();
    }

    public async Task<List<TaskListItemDto>> GetCompletedOnAsync(DateOnly date, IUnitOfWork unitOfWork)
    {
        var rows = await unitOfWork.Connection.QueryAsync<TaskListItemDto>(
            $@"{SelectListItem}
               WHERE t.status <> @pending AND t.due_on = @date
               ORDER BY t.completed_at DESC NULLS LAST",
            new { date, pending = (int)TaskItemStatus.Pending },
            unitOfWork.Transaction);

        return rows.ToList();
    }

    public async Task<List<DomainStreakDto>> GetDomainStreaksAsync(DateOnly today, IUnitOfWork unitOfWork)
    {
        // Two independent aggregates (completion log vs. open tasks) joined onto
        // the domain list, so a domain with no activity still shows up as a row
        // of zeroes rather than vanishing.
        var rows = await unitOfWork.Connection.QueryAsync<DomainStreakDto>(
            @"WITH recent AS (
                  SELECT domain_id,
                         count(*) FILTER (WHERE status = @done)    AS completed_last7_days,
                         count(*) FILTER (WHERE status = @skipped) AS skipped_last7_days,
                         max(completed_at)                          AS last_completed_at
                  FROM tracker.task_completions
                  WHERE due_on > @today - 7
                  GROUP BY domain_id
              ),
              overdue AS (
                  SELECT domain_id, count(*) AS pending_overdue
                  FROM tracker.task_tasks
                  WHERE status = @pending AND due_on < @today
                  GROUP BY domain_id
              )
              SELECT d.id                                    AS domain_id,
                     d.code                                  AS domain_code,
                     d.name                                  AS domain_name,
                     COALESCE(r.completed_last7_days, 0)::int AS completed_last7_days,
                     COALESCE(r.skipped_last7_days, 0)::int   AS skipped_last7_days,
                     COALESCE(o.pending_overdue, 0)::int      AS pending_overdue,
                     r.last_completed_at
              FROM tracker.task_domains d
              LEFT JOIN recent  r ON r.domain_id = d.id
              LEFT JOIN overdue o ON o.domain_id = d.id
              WHERE d.is_active
              ORDER BY d.sort_order, d.name",
            new
            {
                today,
                done = (int)TaskItemStatus.Done,
                skipped = (int)TaskItemStatus.Skipped,
                pending = (int)TaskItemStatus.Pending
            },
            unitOfWork.Transaction);

        return rows.ToList();
    }

    // ── Filter helpers ───────────────────────────────────────────────────────

    private static string BuildWhere(int? domainId, DateOnly? fromDate, DateOnly? toDate, TaskItemStatus? status, string? search)
    {
        var clauses = new List<string>();

        if (domainId.HasValue) clauses.Add("t.domain_id = @domainId");
        if (fromDate.HasValue) clauses.Add("t.due_on >= @fromDate");
        if (toDate.HasValue) clauses.Add("t.due_on <= @toDate");
        if (status.HasValue) clauses.Add("t.status = @status");
        if (!string.IsNullOrWhiteSpace(search)) clauses.Add("t.title ILIKE @search");

        return clauses.Count == 0 ? string.Empty : " WHERE " + string.Join(" AND ", clauses);
    }

    private static object BuildParameters(
        int? domainId,
        DateOnly? fromDate,
        DateOnly? toDate,
        TaskItemStatus? status,
        string? search,
        int? limit,
        int? offset)
    {
        return new
        {
            domainId,
            fromDate,
            toDate,
            status = status.HasValue ? (int)status.Value : (int?)null,
            search = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%",
            limit,
            offset
        };
    }
}
