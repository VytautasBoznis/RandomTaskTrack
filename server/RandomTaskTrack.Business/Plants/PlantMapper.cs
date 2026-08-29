using System.Text.Json;
using RandomTaskTrack.Data.Dtos.Plants;
using RandomTaskTrack.Data.Dtos.Tasks;
using RandomTaskTrack.Data.Models.Ai;
using RandomTaskTrack.Data.Models.Plants;

namespace RandomTaskTrack.Business.Plants;

/// <summary>
/// The one place that knows two encodings: the care profile as jsonb, and the
/// {"plantId": "…"} payload that ties a task back to a plant.
/// </summary>
internal static class PlantMapper
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    /// <summary>The key care tasks carry. Also read by the UI, which groups by it.</summary>
    public const string PlantIdKey = "plantId";

    /// <summary>
    /// The care line a schedule came from, unprefixed — "Water", not
    /// "Water — the big one". It is what lets both the dedupe here and the UI
    /// tell an already-scheduled suggestion from a new one without either of
    /// them having to reproduce the title format.
    /// </summary>
    public const string CareTitleKey = "careTitle";

    public static string Serialize(PlantProfile profile) => JsonSerializer.Serialize(profile, Options);

    /// <summary>The task/recurrence payload for a plant. Copied onto every
    /// instance the schedule spawns, which is what makes the link stick.</summary>
    public static string PayloadFor(Guid plantId, string careTitle) =>
        JsonSerializer.Serialize(
            new Dictionary<string, string>
            {
                [PlantIdKey] = plantId.ToString(),
                [CareTitleKey] = careTitle
            },
            Options);

    public static PlantDto ToDto(
        Plant plant,
        List<TaskListItemDto> tasks,
        List<RecurrenceListItemDto> recurrences,
        List<PlantPhotoDto> photos) => new()
    {
        Id = plant.Id,
        Kind = plant.Kind,
        Name = plant.Name,
        Location = plant.Location,
        Species = plant.Species,
        LatinName = plant.LatinName,
        AcquiredOn = plant.AcquiredOn,
        Notes = plant.Notes,
        Description = plant.Description,

        // ResearchedAt rather than the blob is the test: a lookup that came back
        // empty-handed still wrote a row, and an empty profile card is worse
        // than none at all.
        Profile = plant.ResearchedAt.HasValue ? Deserialize(plant.Profile) : null,

        ResearchedAt = plant.ResearchedAt,
        ResearchModel = plant.ResearchModel,
        Tasks = tasks,
        Recurrences = recurrences,
        Photos = photos,
        CreatedAt = plant.CreatedAt,
        UpdatedAt = plant.UpdatedAt
    };

    /// <summary>
    /// A stored photo as something the model can look at. Base64 on the way out
    /// only — the bytes are stored as bytes, not as text.
    /// </summary>
    public static AiImage ToAiImage(PlantPhoto photo) => new()
    {
        Base64 = Convert.ToBase64String(photo.Image),
        MediaType = photo.MediaType
    };

    /// <summary>
    /// Which plant a task payload names, or null for the ones that name none.
    /// </summary>
    public static Guid? PlantIdOf(string? data) =>
        Guid.TryParse(Read(data, PlantIdKey), out Guid id) ? id : null;

    /// <summary>Which care line a schedule came from, if it came from one.</summary>
    public static string? CareTitleOf(string? data) => Read(data, CareTitleKey);

    /// <summary>
    /// Tolerant on purpose: `data` is free-form jsonb that anything — the chat
    /// agent included — can write into, so anything unexpected is "not a plant
    /// task" rather than an error.
    /// </summary>
    private static string? Read(string? data, string key)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            return null;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(data);

            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !document.RootElement.TryGetProperty(key, out JsonElement value) ||
                value.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            return value.GetString();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static PlantProfile? Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<PlantProfile>(json, Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
