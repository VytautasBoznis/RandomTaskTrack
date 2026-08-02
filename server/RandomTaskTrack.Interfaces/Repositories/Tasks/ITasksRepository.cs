using RandomTaskTrack.Data.Dtos.Tasks;
using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Models.Tasks;
using RandomTaskTrack.Interfaces.Base;

namespace RandomTaskTrack.Interfaces.Repositories.Tasks;

public interface ITasksRepository
{
    Task<TaskListItemDto?> GetByIdAsync(Guid id, IUnitOfWork unitOfWork);

    Task<TaskItem?> GetRawByIdAsync(Guid id, IUnitOfWork unitOfWork);

    Task<List<TaskListItemDto>> QueryAsync(
        int? domainId,
        DateOnly? fromDate,
        DateOnly? toDate,
        TaskItemStatus? status,
        string? search,
        int page,
        int pageSize,
        IUnitOfWork unitOfWork);

    Task<int> CountAsync(
        int? domainId,
        DateOnly? fromDate,
        DateOnly? toDate,
        TaskItemStatus? status,
        string? search,
        IUnitOfWork unitOfWork);

    Task CreateAsync(TaskItem task, IUnitOfWork unitOfWork);

    /// <summary>
    /// Inserts a materialized recurrence instance, relying on
    /// ux_task_tasks_recurrence_due to no-op on a duplicate. Returns true when
    /// a row was actually written — this is what makes the materializer safe to
    /// run repeatedly and concurrently.
    /// </summary>
    Task<bool> TryCreateFromRecurrenceAsync(TaskItem task, IUnitOfWork unitOfWork);

    Task UpdateAsync(TaskItem task, IUnitOfWork unitOfWork);

    Task MarkCompletedAsync(Guid id, TaskItemStatus status, DateTime completedAt, IUnitOfWork unitOfWork);

    Task<bool> DeleteAsync(Guid id, IUnitOfWork unitOfWork);

    /// <summary>Deletes only pending instances on or after the given date.
    /// Completed history is never touched.</summary>
    Task<int> DeletePendingByRecurrenceAsync(Guid recurrenceId, DateOnly fromDate, IUnitOfWork unitOfWork);

    // ── Dashboard buckets ────────────────────────────────────────────────────
    Task<List<TaskListItemDto>> GetOverdueAsync(DateOnly today, IUnitOfWork unitOfWork);
    Task<List<TaskListItemDto>> GetDueOnAsync(DateOnly date, IUnitOfWork unitOfWork);
    Task<List<TaskListItemDto>> GetUpcomingAsync(DateOnly fromExclusive, DateOnly toInclusive, IUnitOfWork unitOfWork);
    Task<List<TaskListItemDto>> GetCompletedOnAsync(DateOnly date, IUnitOfWork unitOfWork);
    Task<List<DomainStreakDto>> GetDomainStreaksAsync(DateOnly today, IUnitOfWork unitOfWork);
}
