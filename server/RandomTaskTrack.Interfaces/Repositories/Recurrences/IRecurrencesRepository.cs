using RandomTaskTrack.Data.Dtos.Tasks;
using RandomTaskTrack.Data.Models.Tasks;
using RandomTaskTrack.Interfaces.Base;

namespace RandomTaskTrack.Interfaces.Repositories.Recurrences;

public interface IRecurrencesRepository
{
    Task<List<RecurrenceListItemDto>> QueryAsync(int? domainId, bool includeInactive, IUnitOfWork unitOfWork);
    Task<RecurrenceListItemDto?> GetByIdAsync(Guid id, IUnitOfWork unitOfWork);
    Task<TaskRecurrence?> GetRawByIdAsync(Guid id, IUnitOfWork unitOfWork);

    /// <summary>Active, in-window recurrences — the materializer's work list.</summary>
    Task<List<TaskRecurrence>> GetActiveAsync(DateOnly onOrBefore, IUnitOfWork unitOfWork);

    Task CreateAsync(TaskRecurrence recurrence, IUnitOfWork unitOfWork);
    Task UpdateAsync(TaskRecurrence recurrence, IUnitOfWork unitOfWork);
    Task UpdateLastDueOnAsync(Guid id, DateOnly lastDueOn, IUnitOfWork unitOfWork);
    Task<bool> DeleteAsync(Guid id, IUnitOfWork unitOfWork);
}
