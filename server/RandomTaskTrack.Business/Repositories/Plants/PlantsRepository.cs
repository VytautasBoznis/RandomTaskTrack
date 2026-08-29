using Dapper;
using RandomTaskTrack.Data.Dtos.Plants;
using RandomTaskTrack.Data.Dtos.Tasks;
using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Models.Plants;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Plants;

namespace RandomTaskTrack.Business.Repositories.Plants;

public class PlantsRepository : IPlantsRepository
{
    private const string SelectColumns = @"
        id, kind, name, location, species, latin_name, acquired_on, notes, description,
        profile::text AS profile, researched_at, research_model, created_at, updated_at";

    /// <summary>Everything but the bytes. See PlantPhotoDto for why.</summary>
    private const string SelectPhotoMeta = @"
        id, plant_id, media_type, stage, note, taken_on, created_at";

    /// <summary>
    /// The link is a jsonb field rather than a column (see the plants
    /// migration), so this filter cannot use an index. It does not need to:
    /// both queries are already narrowed to pending rows, and pending tasks
    /// only ever exist out to the materializer's 21-day horizon.
    /// </summary>
    private const string ByPlantId = "data ->> 'plantId' = ANY(@plantIds)";

    public async Task<List<Plant>> GetAllAsync(IUnitOfWork unitOfWork)
    {
        var rows = await unitOfWork.Connection.QueryAsync<Plant>(
            $@"SELECT {SelectColumns}
               FROM tracker.plant_plants
               ORDER BY lower(name)",
            transaction: unitOfWork.Transaction);

        return rows.ToList();
    }

    public async Task<Plant?> GetByIdAsync(Guid id, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<Plant>(
            $"SELECT {SelectColumns} FROM tracker.plant_plants WHERE id = @id",
            new { id },
            unitOfWork.Transaction);
    }

    public async Task CreateAsync(Plant plant, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            @"INSERT INTO tracker.plant_plants
                  (id, kind, name, location, species, latin_name, acquired_on, notes,
                   description, profile, researched_at, research_model)
              VALUES
                  (@Id, @Kind, @Name, @Location, @Species, @LatinName, @AcquiredOn, @Notes,
                   @Description, @Profile::jsonb, @ResearchedAt, @ResearchModel)",
            plant,
            unitOfWork.Transaction);
    }

    public async Task UpdateAsync(Plant plant, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            @"UPDATE tracker.plant_plants
              SET kind        = @Kind,
                  name        = @Name,
                  location    = @Location,
                  species     = @Species,
                  latin_name  = @LatinName,
                  acquired_on = @AcquiredOn,
                  notes       = @Notes,
                  updated_at  = now()
              WHERE id = @Id",
            plant,
            unitOfWork.Transaction);
    }

    public async Task SaveProfileAsync(Plant plant, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            @"UPDATE tracker.plant_plants
              SET description    = @Description,
                  profile        = @Profile::jsonb,
                  species        = @Species,
                  latin_name     = @LatinName,
                  researched_at  = @ResearchedAt,
                  research_model = @ResearchModel,
                  updated_at     = now()
              WHERE id = @Id",
            plant,
            unitOfWork.Transaction);
    }

    public async Task<bool> DeleteAsync(Guid id, IUnitOfWork unitOfWork)
    {
        int affected = await unitOfWork.Connection.ExecuteAsync(
            "DELETE FROM tracker.plant_plants WHERE id = @id",
            new { id },
            unitOfWork.Transaction);

        return affected > 0;
    }

    public async Task<List<PlantPhotoDto>> GetPhotosAsync(IEnumerable<Guid> plantIds, IUnitOfWork unitOfWork)
    {
        Guid[] ids = plantIds.ToArray();

        if (ids.Length == 0)
        {
            return new List<PlantPhotoDto>();
        }

        var rows = await unitOfWork.Connection.QueryAsync<PlantPhotoDto>(
            $@"SELECT {SelectPhotoMeta}
               FROM tracker.plant_photos
               WHERE plant_id = ANY(@ids)
               ORDER BY taken_on DESC, created_at DESC",
            new { ids },
            unitOfWork.Transaction);

        return rows.ToList();
    }

    public async Task<PlantPhoto?> GetPhotoAsync(Guid photoId, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<PlantPhoto>(
            $"SELECT {SelectPhotoMeta}, image FROM tracker.plant_photos WHERE id = @photoId",
            new { photoId },
            unitOfWork.Transaction);
    }

    public async Task<PlantPhoto?> GetLatestPhotoAsync(Guid plantId, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<PlantPhoto>(
            $@"SELECT {SelectPhotoMeta}, image
               FROM tracker.plant_photos
               WHERE plant_id = @plantId
               ORDER BY taken_on DESC, created_at DESC
               LIMIT 1",
            new { plantId },
            unitOfWork.Transaction);
    }

    public async Task AddPhotoAsync(PlantPhoto photo, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            @"INSERT INTO tracker.plant_photos
                  (id, plant_id, image, media_type, stage, note, taken_on)
              VALUES
                  (@Id, @PlantId, @Image, @MediaType, @Stage, @Note, @TakenOn)",
            photo,
            unitOfWork.Transaction);
    }

    public async Task SavePhotoReadAsync(Guid photoId, string stage, string note, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            @"UPDATE tracker.plant_photos
              SET stage = @stage, note = @note
              WHERE id = @photoId",
            new { photoId, stage, note },
            unitOfWork.Transaction);
    }

    public async Task<bool> DeletePhotoAsync(Guid photoId, IUnitOfWork unitOfWork)
    {
        int affected = await unitOfWork.Connection.ExecuteAsync(
            "DELETE FROM tracker.plant_photos WHERE id = @photoId",
            new { photoId },
            unitOfWork.Transaction);

        return affected > 0;
    }

    public async Task<List<TaskListItemDto>> GetPendingTasksAsync(IEnumerable<Guid> plantIds, IUnitOfWork unitOfWork)
    {
        string[] ids = ToTextIds(plantIds);

        if (ids.Length == 0)
        {
            return new List<TaskListItemDto>();
        }

        var rows = await unitOfWork.Connection.QueryAsync<TaskListItemDto>(
            $@"SELECT t.id, t.domain_id, d.code AS domain_code, d.name AS domain_name,
                      t.recurrence_id, t.title, t.notes, t.data::text AS data,
                      t.due_on, t.due_time, t.status, t.completed_at
               FROM tracker.task_tasks t
               INNER JOIN tracker.task_domains d ON d.id = t.domain_id
               WHERE t.status = @pending
                 AND t.{ByPlantId}
               ORDER BY t.due_on, t.title",
            new { plantIds = ids, pending = (int)TaskItemStatus.Pending },
            unitOfWork.Transaction);

        return rows.ToList();
    }

    public async Task<List<RecurrenceListItemDto>> GetCareRecurrencesAsync(IEnumerable<Guid> plantIds, IUnitOfWork unitOfWork)
    {
        string[] ids = ToTextIds(plantIds);

        if (ids.Length == 0)
        {
            return new List<RecurrenceListItemDto>();
        }

        var rows = await unitOfWork.Connection.QueryAsync<RecurrenceListItemDto>(
            $@"SELECT r.id, r.domain_id, d.code AS domain_code, r.title, r.notes,
                      r.data::text AS data, r.rule_type, r.interval_days, r.days_of_week,
                      r.day_of_month, r.anchor_mode, r.time_of_day, r.starts_on,
                      r.ends_on, r.is_active, r.last_due_on
               FROM tracker.task_recurrences r
               INNER JOIN tracker.task_domains d ON d.id = r.domain_id
               WHERE r.{ByPlantId}
               ORDER BY r.title",
            new { plantIds = ids },
            unitOfWork.Transaction);

        return rows.ToList();
    }

    public async Task<int> DeletePendingTasksAsync(Guid plantId, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.ExecuteAsync(
            $@"DELETE FROM tracker.task_tasks
               WHERE status = @pending
                 AND {ByPlantId}",
            new { plantIds = new[] { plantId.ToString() }, pending = (int)TaskItemStatus.Pending },
            unitOfWork.Transaction);
    }

    /// <summary>
    /// jsonb ->> gives text, so the ids go over as text too. Comparing them as
    /// uuid would fail the whole query the first time any other scope writes a
    /// non-uuid `plantId` into a payload.
    /// </summary>
    private static string[] ToTextIds(IEnumerable<Guid> plantIds) =>
        plantIds.Select(id => id.ToString()).ToArray();
}
