using RandomTaskTrack.Data.Models.Tasks;
using RandomTaskTrack.Interfaces.Base;

namespace RandomTaskTrack.Interfaces.Services;

public interface IRecurrenceMaterializer
{
    /// <summary>Materializes instances for every active recurrence up to the
    /// configured horizon. Idempotent.</summary>
    Task<int> MaterializeAllAsync(IUnitOfWork unitOfWork, CancellationToken cancellationToken);

    /// <summary>Materializes a single recurrence. Used right after create/update
    /// so tasks appear immediately rather than on the next background sweep.</summary>
    Task<int> MaterializeOneAsync(TaskRecurrence recurrence, IUnitOfWork unitOfWork, CancellationToken cancellationToken);

    /// <summary>
    /// Next occurrence after a completion, for FromCompletion recurrences.
    /// Returns null for FromSchedule (those are handled by the horizon sweep)
    /// or when the recurrence has ended.
    /// </summary>
    DateOnly? GetNextDueAfterCompletion(TaskRecurrence recurrence, DateOnly completedOn);
}
