using System.Text.Json;
using RandomTaskTrack.Data.Dtos.Learning;
using RandomTaskTrack.Data.Dtos.Tasks;
using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Models.Learning;

namespace RandomTaskTrack.Business.Learning;

/// <summary>
/// The one place that knows three encodings: the drafted plan and the renewal
/// rules as jsonb, and the {"learnStepId": …} / {"credentialId": …} payloads
/// that tie a task back to what put it on the board.
/// </summary>
internal static class LearningMapper
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    /// <summary>The key a step's task carries. Also read by the UI.</summary>
    public const string StepIdKey = "learnStepId";

    /// <summary>The key a renewal reminder carries.</summary>
    public const string CredentialIdKey = "credentialId";

    /// <summary>
    /// The goal, alongside the step. Not needed to find anything — the step id
    /// is enough — but it makes a task on the board self-describing, which is
    /// what lets the chat agent see which path a task belongs to without a
    /// second lookup.
    /// </summary>
    public const string GoalIdKey = "learnGoalId";

    /// <summary>
    /// How long before expiry a renewal is treated as actionable when the
    /// lookup did not say. Long enough to book and sit an exam without
    /// rearranging a month; short enough not to nag for a year.
    /// </summary>
    public const int DefaultRenewalWindowDays = 60;

    public static string Serialize(LearningPlan plan) => JsonSerializer.Serialize(plan, Options);

    public static string Serialize(CredentialRenewal renewal) => JsonSerializer.Serialize(renewal, Options);

    /// <summary>
    /// The stored renewal rules, or null when nothing has been looked up and
    /// when what is stored no longer parses. Public because the reminder is
    /// dated from the window this carries.
    /// </summary>
    public static CredentialRenewal? DeserializeRenewal(string json) => Deserialize<CredentialRenewal>(json);

    public static string PayloadForStep(Guid stepId, Guid goalId) =>
        JsonSerializer.Serialize(
            new Dictionary<string, string>
            {
                [StepIdKey] = stepId.ToString(),
                [GoalIdKey] = goalId.ToString()
            },
            Options);

    public static string PayloadForCredential(Guid credentialId) =>
        JsonSerializer.Serialize(
            new Dictionary<string, string> { [CredentialIdKey] = credentialId.ToString() },
            Options);

    /// <summary>Which step a task payload names, or null for the ones that name none.</summary>
    public static Guid? StepIdOf(string? data) =>
        Guid.TryParse(Read(data, StepIdKey), out Guid id) ? id : null;

    public static Guid? CredentialIdOf(string? data) =>
        Guid.TryParse(Read(data, CredentialIdKey), out Guid id) ? id : null;

    /// <summary>Which path a task payload names. What lets the dashboard group
    /// a path's tasks into one row without going back for the step.</summary>
    public static Guid? GoalIdOf(string? data) =>
        Guid.TryParse(Read(data, GoalIdKey), out Guid id) ? id : null;

    public static LearningGoalDto ToDto(LearningGoal goal, List<LearningStepDto> steps, DateOnly today) => new()
    {
        Id = goal.Id,
        Title = goal.Title,
        Tier = goal.Tier,
        Status = goal.Status,
        Why = goal.Why,
        Benefits = goal.Benefits,
        TargetOn = goal.TargetOn,
        Context = goal.Context,

        // ResearchedAt rather than the blob is the test: a draft that came back
        // half-empty still wrote a row, and an empty plan panel is worse than none.
        Plan = goal.ResearchedAt.HasValue ? Deserialize<LearningPlan>(goal.Plan) : null,

        ResearchedAt = goal.ResearchedAt,
        ResearchModel = goal.ResearchModel,
        Notes = goal.Notes,
        Steps = steps,
        DaysUntilTarget = goal.TargetOn.HasValue ? goal.TargetOn.Value.DayNumber - today.DayNumber : null,
        CreatedAt = goal.CreatedAt,
        UpdatedAt = goal.UpdatedAt
    };

    public static LearningStepDto ToDto(LearningStep step, TaskListItemDto? task) => new()
    {
        Id = step.Id,
        GoalId = step.GoalId,
        Title = step.Title,
        Kind = step.Kind,
        Status = step.Status,
        TargetOn = step.TargetOn,
        Notes = step.Notes,
        Outcome = step.Outcome,
        Provider = step.Provider,
        Url = step.Url,
        Cost = step.Cost,
        Hours = step.Hours,
        SortOrder = step.SortOrder,
        Task = task,
        CreatedAt = step.CreatedAt,
        UpdatedAt = step.UpdatedAt
    };

    public static LearningCredentialDto ToDto(LearningCredential credential, TaskListItemDto? task, DateOnly today)
    {
        CredentialRenewal? renewal = credential.ResearchedAt.HasValue
            ? Deserialize<CredentialRenewal>(credential.Renewal)
            : null;

        return new LearningCredentialDto
        {
            Id = credential.Id,
            GoalId = credential.GoalId,
            Name = credential.Name,
            Issuer = credential.Issuer,
            Code = credential.Code,
            EarnedOn = credential.EarnedOn,
            RenewalKind = credential.RenewalKind,
            ExpiresOn = credential.ExpiresOn,
            CredentialId = credential.CredentialId,
            Url = credential.Url,
            Renewal = renewal,
            ResearchedAt = credential.ResearchedAt,
            ResearchModel = credential.ResearchModel,
            Notes = credential.Notes,

            // Only ever a countdown for something that actually counts down.
            // A permanent credential and an unchecked one both answer null, and
            // the UI renders them as what they are rather than as "expires in".
            DaysUntilExpiry = credential.ExpiresOn.HasValue
                ? credential.ExpiresOn.Value.DayNumber - today.DayNumber
                : null,

            IsRenewable = IsRenewable(credential, renewal, today),
            Task = task,
            CreatedAt = credential.CreatedAt,
            UpdatedAt = credential.UpdatedAt
        };
    }

    /// <summary>
    /// Whether the renewal window is open. Driven by what the lookup found
    /// rather than by a fixed number of days, because the windows genuinely
    /// differ — Microsoft opens six months out, others a few weeks. Stays true
    /// once it has lapsed: something that has expired is the most actionable
    /// state there is.
    /// </summary>
    public static bool IsRenewable(LearningCredential credential, CredentialRenewal? renewal, DateOnly today)
    {
        if (credential.RenewalKind != CredentialRenewalKind.Expires || credential.ExpiresOn is null)
        {
            return false;
        }

        int window = renewal?.WindowOpensDays > 0 ? renewal.WindowOpensDays : DefaultRenewalWindowDays;

        return today >= credential.ExpiresOn.Value.AddDays(-window);
    }

    /// <summary>
    /// Tolerant on purpose: `data` is free-form jsonb that anything — the chat
    /// agent included — can write into, so anything unexpected is "not a
    /// learning task" rather than an error.
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

    private static T? Deserialize<T>(string json) where T : class
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json, Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
