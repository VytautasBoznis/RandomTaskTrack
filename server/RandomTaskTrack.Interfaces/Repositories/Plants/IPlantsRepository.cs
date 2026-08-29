using RandomTaskTrack.Data.Dtos.Plants;
using RandomTaskTrack.Data.Dtos.Tasks;
using RandomTaskTrack.Data.Models.Plants;
using RandomTaskTrack.Interfaces.Base;

namespace RandomTaskTrack.Interfaces.Repositories.Plants;

public interface IPlantsRepository
{
    Task<List<Plant>> GetAllAsync(IUnitOfWork unitOfWork);
    Task<Plant?> GetByIdAsync(Guid id, IUnitOfWork unitOfWork);
    Task CreateAsync(Plant plant, IUnitOfWork unitOfWork);
    Task UpdateAsync(Plant plant, IUnitOfWork unitOfWork);

    /// <summary>
    /// Writes back a finished lookup only. Split from UpdateAsync because the
    /// two have different owners: the user owns the name and their notes, the
    /// lookup owns the profile, and neither should overwrite the other.
    /// </summary>
    Task SaveProfileAsync(Plant plant, IUnitOfWork unitOfWork);

    Task<bool> DeleteAsync(Guid id, IUnitOfWork unitOfWork);

    // ── Photos, which are also the stage history ─────────────────────────────

    /// <summary>Metadata only, newest first. Never selects the image column —
    /// the tab lists every photo of every plant.</summary>
    Task<List<PlantPhotoDto>> GetPhotosAsync(IEnumerable<Guid> plantIds, IUnitOfWork unitOfWork);

    /// <summary>With the bytes. For serving one, and for showing one to the AI.</summary>
    Task<PlantPhoto?> GetPhotoAsync(Guid photoId, IUnitOfWork unitOfWork);

    /// <summary>The plant as it looks now — what a re-lookup should be shown.</summary>
    Task<PlantPhoto?> GetLatestPhotoAsync(Guid plantId, IUnitOfWork unitOfWork);

    Task AddPhotoAsync(PlantPhoto photo, IUnitOfWork unitOfWork);

    /// <summary>Writes back what the AI made of a photo, after the fact.</summary>
    Task SavePhotoReadAsync(Guid photoId, string stage, string note, IUnitOfWork unitOfWork);

    Task<bool> DeletePhotoAsync(Guid photoId, IUnitOfWork unitOfWork);

    // ── The task-engine side, joined on data->>'plantId' ─────────────────────

    /// <summary>Pending tasks for the given plants, soonest first.</summary>
    Task<List<TaskListItemDto>> GetPendingTasksAsync(IEnumerable<Guid> plantIds, IUnitOfWork unitOfWork);

    /// <summary>Care schedules for the given plants, paused ones included —
    /// hiding a paused schedule would leave no way to bring it back.</summary>
    Task<List<RecurrenceListItemDto>> GetCareRecurrencesAsync(IEnumerable<Guid> plantIds, IUnitOfWork unitOfWork);

    /// <summary>
    /// Every pending task for the plant, overdue ones included — unlike
    /// DeletePendingByRecurrenceAsync, which keeps them. The plant is being
    /// deleted, so "water it, 3 days late" is noise rather than history.
    /// Completed rows are never touched.
    /// </summary>
    Task<int> DeletePendingTasksAsync(Guid plantId, IUnitOfWork unitOfWork);
}
