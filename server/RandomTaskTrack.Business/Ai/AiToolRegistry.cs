using System.Text.Json;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Data.Models.Ai;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Models.Tasks;
using RandomTaskTrack.Interfaces.Ai;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Domains;
using RandomTaskTrack.Interfaces.Repositories.Recurrences;
using RandomTaskTrack.Interfaces.Repositories.Tasks;
using RandomTaskTrack.Interfaces.Services;

namespace RandomTaskTrack.Business.Ai;

/// <summary>
/// The AI's entire surface onto the database. Every definition here is plain
/// JSON Schema so it can be handed to any provider unchanged, and every handler
/// runs on the caller's UnitOfWork so a chat turn is atomic.
/// </summary>
public class AiToolRegistry : IAiToolRegistry
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false
    };

    private readonly ITasksRepository _tasksRepository;
    private readonly IRecurrencesRepository _recurrencesRepository;
    private readonly ICompletionsRepository _completionsRepository;
    private readonly IDomainsRepository _domainsRepository;
    private readonly IRecurrenceMaterializer _materializer;
    private readonly IClock _clock;
    private readonly ILogger<AiToolRegistry> _logger;

    public AiToolRegistry(
        ITasksRepository tasksRepository,
        IRecurrencesRepository recurrencesRepository,
        ICompletionsRepository completionsRepository,
        IDomainsRepository domainsRepository,
        IRecurrenceMaterializer materializer,
        IClock clock,
        ILogger<AiToolRegistry> logger)
    {
        _tasksRepository = tasksRepository;
        _recurrencesRepository = recurrencesRepository;
        _completionsRepository = completionsRepository;
        _domainsRepository = domainsRepository;
        _materializer = materializer;
        _clock = clock;
        _logger = logger;
    }

    public List<AiToolDefinition> GetDefinitions() =>
    [
        new()
        {
            Name = AiToolNames.ListDomains,
            Description = "List the available trackers (domains) with their ids and codes. Call this before creating anything so you use a real domain_id.",
            InputSchema = """
                {"type":"object","properties":{},"additionalProperties":false}
                """
        },
        new()
        {
            Name = AiToolNames.QueryTasks,
            Description = "Search existing tasks. Use this before creating tasks to avoid duplicating something already scheduled.",
            InputSchema = """
                {
                  "type":"object",
                  "properties":{
                    "domain_id":{"type":"integer","description":"Restrict to one tracker."},
                    "from_date":{"type":"string","description":"Inclusive lower bound, YYYY-MM-DD."},
                    "to_date":{"type":"string","description":"Inclusive upper bound, YYYY-MM-DD."},
                    "status":{"type":"string","enum":["pending","done","skipped"]},
                    "search":{"type":"string","description":"Case-insensitive substring of the title."},
                    "limit":{"type":"integer","description":"Max rows, default 50."}
                  },
                  "additionalProperties":false
                }
                """
        },
        new()
        {
            Name = AiToolNames.CreateTask,
            Description = "Create a single dated task. For anything that repeats, use create_recurrence instead of calling this in a loop.",
            InputSchema = """
                {
                  "type":"object",
                  "properties":{
                    "domain_id":{"type":"integer"},
                    "title":{"type":"string"},
                    "notes":{"type":"string"},
                    "due_on":{"type":"string","description":"YYYY-MM-DD."},
                    "due_time":{"type":"string","description":"HH:MM, optional."},
                    "data":{"type":"object","description":"Domain-specific payload, e.g. {\"sets\":5,\"reps\":5,\"weight_kg\":80} for a lift, or {\"water_ml\":250} for a plant."}
                  },
                  "required":["domain_id","title","due_on"],
                  "additionalProperties":false
                }
                """
        },
        new()
        {
            Name = AiToolNames.UpdateTask,
            Description = "Change an existing task's title, notes, payload, due date or tracker. Only the fields you pass are changed.",
            InputSchema = """
                {
                  "type":"object",
                  "properties":{
                    "id":{"type":"string","description":"Task id (uuid)."},
                    "title":{"type":"string"},
                    "notes":{"type":"string"},
                    "due_on":{"type":"string","description":"YYYY-MM-DD."},
                    "due_time":{"type":"string","description":"HH:MM."},
                    "domain_id":{"type":"integer"},
                    "data":{"type":"object"}
                  },
                  "required":["id"],
                  "additionalProperties":false
                }
                """
        },
        new()
        {
            Name = AiToolNames.DeleteTask,
            Description = "Delete a task and its completion history. Destructive and irreversible.",
            RequiresConfirmation = true,
            InputSchema = """
                {
                  "type":"object",
                  "properties":{"id":{"type":"string","description":"Task id (uuid)."}},
                  "required":["id"],
                  "additionalProperties":false
                }
                """
        },
        new()
        {
            Name = AiToolNames.CreateRecurrence,
            Description = "Create a repeating task. Instances are generated automatically. Prefer this over many create_task calls.",
            InputSchema = """
                {
                  "type":"object",
                  "properties":{
                    "domain_id":{"type":"integer"},
                    "title":{"type":"string"},
                    "notes":{"type":"string"},
                    "data":{"type":"object","description":"Payload copied onto every generated instance."},
                    "rule_type":{"type":"string","enum":["interval_days","days_of_week","day_of_month"]},
                    "interval_days":{"type":"integer","description":"Required when rule_type is interval_days."},
                    "days_of_week":{"type":"array","items":{"type":"integer"},"description":"Required when rule_type is days_of_week. 0=Sunday .. 6=Saturday."},
                    "day_of_month":{"type":"integer","description":"Required when rule_type is day_of_month. 1-31, clamped to short months."},
                    "anchor_mode":{"type":"string","enum":["from_schedule","from_completion"],"description":"from_schedule keeps a fixed cadence regardless of when it was done. from_completion restarts the interval from the actual completion date - use it for chores where the gap matters more than the calendar."},
                    "time_of_day":{"type":"string","description":"HH:MM, optional."},
                    "starts_on":{"type":"string","description":"YYYY-MM-DD, defaults to today."},
                    "ends_on":{"type":"string","description":"YYYY-MM-DD, optional."}
                  },
                  "required":["domain_id","title","rule_type"],
                  "additionalProperties":false
                }
                """
        },
        new()
        {
            Name = AiToolNames.UpdateRecurrence,
            Description = "Change a recurrence. Only the fields you pass are changed. Set is_active false to pause it without deleting history.",
            InputSchema = """
                {
                  "type":"object",
                  "properties":{
                    "id":{"type":"string"},
                    "title":{"type":"string"},
                    "notes":{"type":"string"},
                    "data":{"type":"object"},
                    "rule_type":{"type":"string","enum":["interval_days","days_of_week","day_of_month"]},
                    "interval_days":{"type":"integer"},
                    "days_of_week":{"type":"array","items":{"type":"integer"}},
                    "day_of_month":{"type":"integer"},
                    "anchor_mode":{"type":"string","enum":["from_schedule","from_completion"]},
                    "time_of_day":{"type":"string"},
                    "ends_on":{"type":"string"},
                    "is_active":{"type":"boolean"}
                  },
                  "required":["id"],
                  "additionalProperties":false
                }
                """
        },
        new()
        {
            Name = AiToolNames.DeleteRecurrence,
            Description = "Delete a recurrence and its pending future instances. Completed history is kept. Destructive.",
            RequiresConfirmation = true,
            InputSchema = """
                {
                  "type":"object",
                  "properties":{"id":{"type":"string"}},
                  "required":["id"],
                  "additionalProperties":false
                }
                """
        },
        new()
        {
            Name = AiToolNames.QueryCompletionLog,
            Description = "Read what actually happened: planned vs actual values per completion. This is the real data - use it before giving progress feedback or adjusting a plan, and never invent numbers that are not in here.",
            InputSchema = """
                {
                  "type":"object",
                  "properties":{
                    "domain_id":{"type":"integer"},
                    "title_contains":{"type":"string","description":"Substring of the task title, e.g. 'squat'."},
                    "from_date":{"type":"string","description":"YYYY-MM-DD."},
                    "to_date":{"type":"string","description":"YYYY-MM-DD."},
                    "limit":{"type":"integer","description":"Max rows, default 100."}
                  },
                  "additionalProperties":false
                }
                """
        }
    ];

    public async Task<AiToolResult> ExecuteAsync(AiToolCall call, IUnitOfWork unitOfWork, CancellationToken cancellationToken)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(string.IsNullOrWhiteSpace(call.JsonInput) ? "{}" : call.JsonInput);
            JsonElement input = document.RootElement;

            string payload = call.Name switch
            {
                AiToolNames.ListDomains => await ListDomainsAsync(unitOfWork),
                AiToolNames.QueryTasks => await QueryTasksAsync(input, unitOfWork),
                AiToolNames.CreateTask => await CreateTaskAsync(input, unitOfWork),
                AiToolNames.UpdateTask => await UpdateTaskAsync(input, unitOfWork),
                AiToolNames.DeleteTask => await DeleteTaskAsync(input, unitOfWork),
                AiToolNames.CreateRecurrence => await CreateRecurrenceAsync(input, unitOfWork, cancellationToken),
                AiToolNames.UpdateRecurrence => await UpdateRecurrenceAsync(input, unitOfWork, cancellationToken),
                AiToolNames.DeleteRecurrence => await DeleteRecurrenceAsync(input, unitOfWork),
                AiToolNames.QueryCompletionLog => await QueryCompletionLogAsync(input, unitOfWork),
                _ => throw new InvalidOperationException($"Unknown tool '{call.Name}'.")
            };

            return new AiToolResult { ToolCallId = call.Id, Content = payload };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Hand the failure back to the model rather than aborting the turn:
            // it can usually correct a bad id or a malformed date itself.
            _logger.LogWarning(ex, "AI tool {Tool} failed", call.Name);

            return new AiToolResult
            {
                ToolCallId = call.Id,
                Content = $"Error: {ex.Message}",
                IsError = true
            };
        }
    }

    // ── Handlers ─────────────────────────────────────────────────────────────

    private async Task<string> ListDomainsAsync(IUnitOfWork unitOfWork)
    {
        List<TaskDomain> domains = await _domainsRepository.GetAllAsync(false, unitOfWork);

        return Serialize(domains.Select(d => new { id = d.Id, code = d.Code, name = d.Name }));
    }

    private async Task<string> QueryTasksAsync(JsonElement input, IUnitOfWork unitOfWork)
    {
        var tasks = await _tasksRepository.QueryAsync(
            GetInt(input, "domain_id"),
            GetDate(input, "from_date"),
            GetDate(input, "to_date"),
            ParseStatus(GetString(input, "status")),
            GetString(input, "search"),
            1,
            GetInt(input, "limit") ?? 50,
            unitOfWork);

        return Serialize(tasks.Select(t => new
        {
            id = t.Id,
            domain_id = t.DomainId,
            domain = t.DomainCode,
            title = t.Title,
            notes = t.Notes,
            due_on = t.DueOn.ToString("yyyy-MM-dd"),
            status = t.Status.ToString().ToLowerInvariant(),
            recurrence_id = t.RecurrenceId,
            data = RawJson(t.Data)
        }));
    }

    private async Task<string> CreateTaskAsync(JsonElement input, IUnitOfWork unitOfWork)
    {
        int domainId = GetRequiredInt(input, "domain_id");
        await EnsureDomainExistsAsync(domainId, unitOfWork);

        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            DomainId = domainId,
            Title = GetRequiredString(input, "title"),
            Notes = GetString(input, "notes"),
            Data = GetObjectJson(input, "data"),
            DueOn = GetDate(input, "due_on") ?? throw new InvalidOperationException("due_on is required (YYYY-MM-DD)."),
            DueTime = GetTime(input, "due_time"),
            Status = TaskItemStatus.Pending
        };

        await _tasksRepository.CreateAsync(task, unitOfWork);

        return Serialize(new { created = true, id = task.Id, due_on = task.DueOn.ToString("yyyy-MM-dd") });
    }

    private async Task<string> UpdateTaskAsync(JsonElement input, IUnitOfWork unitOfWork)
    {
        Guid id = GetRequiredGuid(input, "id");
        TaskItem existing = await _tasksRepository.GetRawByIdAsync(id, unitOfWork)
                            ?? throw new InvalidOperationException($"No task with id {id}.");

        existing.Title = GetString(input, "title") ?? existing.Title;
        existing.Notes = GetString(input, "notes") ?? existing.Notes;
        existing.DueOn = GetDate(input, "due_on") ?? existing.DueOn;
        existing.DueTime = GetTime(input, "due_time") ?? existing.DueTime;

        int? domainId = GetInt(input, "domain_id");
        if (domainId.HasValue)
        {
            await EnsureDomainExistsAsync(domainId.Value, unitOfWork);
            existing.DomainId = domainId.Value;
        }

        if (input.TryGetProperty("data", out JsonElement data) && data.ValueKind == JsonValueKind.Object)
        {
            existing.Data = data.GetRawText();
        }

        await _tasksRepository.UpdateAsync(existing, unitOfWork);

        return Serialize(new { updated = true, id = existing.Id });
    }

    private async Task<string> DeleteTaskAsync(JsonElement input, IUnitOfWork unitOfWork)
    {
        Guid id = GetRequiredGuid(input, "id");
        bool deleted = await _tasksRepository.DeleteAsync(id, unitOfWork);

        return Serialize(new { deleted, id });
    }

    private async Task<string> CreateRecurrenceAsync(JsonElement input, IUnitOfWork unitOfWork, CancellationToken cancellationToken)
    {
        int domainId = GetRequiredInt(input, "domain_id");
        await EnsureDomainExistsAsync(domainId, unitOfWork);

        RecurrenceRuleType ruleType = ParseRuleType(GetRequiredString(input, "rule_type"));

        var recurrence = new TaskRecurrence
        {
            Id = Guid.NewGuid(),
            DomainId = domainId,
            Title = GetRequiredString(input, "title"),
            Notes = GetString(input, "notes"),
            Data = GetObjectJson(input, "data"),
            RuleType = ruleType,
            IntervalDays = GetInt(input, "interval_days"),
            DaysOfWeek = GetIntArray(input, "days_of_week"),
            DayOfMonth = GetInt(input, "day_of_month"),
            AnchorMode = ParseAnchorMode(GetString(input, "anchor_mode")),
            TimeOfDay = GetTime(input, "time_of_day"),
            StartsOn = GetDate(input, "starts_on") ?? _clock.Today,
            EndsOn = GetDate(input, "ends_on"),
            IsActive = true
        };

        ValidateRecurrenceShape(recurrence);

        await _recurrencesRepository.CreateAsync(recurrence, unitOfWork);
        int materialized = await _materializer.MaterializeOneAsync(recurrence, unitOfWork, cancellationToken);

        return Serialize(new
        {
            created = true,
            id = recurrence.Id,
            materialized_tasks = materialized
        });
    }

    private async Task<string> UpdateRecurrenceAsync(JsonElement input, IUnitOfWork unitOfWork, CancellationToken cancellationToken)
    {
        Guid id = GetRequiredGuid(input, "id");
        TaskRecurrence existing = await _recurrencesRepository.GetRawByIdAsync(id, unitOfWork)
                                  ?? throw new InvalidOperationException($"No recurrence with id {id}.");

        existing.Title = GetString(input, "title") ?? existing.Title;
        existing.Notes = GetString(input, "notes") ?? existing.Notes;
        existing.IntervalDays = GetInt(input, "interval_days") ?? existing.IntervalDays;
        existing.DaysOfWeek = GetIntArray(input, "days_of_week") ?? existing.DaysOfWeek;
        existing.DayOfMonth = GetInt(input, "day_of_month") ?? existing.DayOfMonth;
        existing.TimeOfDay = GetTime(input, "time_of_day") ?? existing.TimeOfDay;
        existing.EndsOn = GetDate(input, "ends_on") ?? existing.EndsOn;

        string? ruleType = GetString(input, "rule_type");
        if (ruleType != null) existing.RuleType = ParseRuleType(ruleType);

        string? anchorMode = GetString(input, "anchor_mode");
        if (anchorMode != null) existing.AnchorMode = ParseAnchorMode(anchorMode);

        if (input.TryGetProperty("is_active", out JsonElement active) &&
            active.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            existing.IsActive = active.GetBoolean();
        }

        if (input.TryGetProperty("data", out JsonElement data) && data.ValueKind == JsonValueKind.Object)
        {
            existing.Data = data.GetRawText();
        }

        ValidateRecurrenceShape(existing);

        await _recurrencesRepository.UpdateAsync(existing, unitOfWork);
        int materialized = await _materializer.MaterializeOneAsync(existing, unitOfWork, cancellationToken);

        return Serialize(new { updated = true, id = existing.Id, materialized_tasks = materialized });
    }

    private async Task<string> DeleteRecurrenceAsync(JsonElement input, IUnitOfWork unitOfWork)
    {
        Guid id = GetRequiredGuid(input, "id");

        int removed = await _tasksRepository.DeletePendingByRecurrenceAsync(id, _clock.Today, unitOfWork);
        bool deleted = await _recurrencesRepository.DeleteAsync(id, unitOfWork);

        return Serialize(new { deleted, id, removed_pending_tasks = removed });
    }

    private async Task<string> QueryCompletionLogAsync(JsonElement input, IUnitOfWork unitOfWork)
    {
        var entries = await _completionsRepository.QueryAsync(
            GetInt(input, "domain_id"),
            GetString(input, "title_contains"),
            GetDate(input, "from_date"),
            GetDate(input, "to_date"),
            GetInt(input, "limit") ?? 100,
            unitOfWork);

        return Serialize(entries.Select(e => new
        {
            id = e.Id,
            task_id = e.TaskId,
            domain = e.DomainCode,
            title = e.Title,
            status = e.Status.ToString().ToLowerInvariant(),
            due_on = e.DueOn.ToString("yyyy-MM-dd"),
            completed_at = e.CompletedAt,
            planned = RawJson(e.PlannedData),
            actual = RawJson(e.ActualData),
            note = e.Note
        }));
    }

    // ── Parsing helpers ──────────────────────────────────────────────────────
    // Everything the model sends is untrusted text. These convert or throw with
    // a message the model can act on, which is why the errors are phrased for
    // the model rather than for a log.

    private async Task EnsureDomainExistsAsync(int domainId, IUnitOfWork unitOfWork)
    {
        TaskDomain? domain = await _domainsRepository.GetByIdAsync(domainId, unitOfWork);

        if (domain is null)
        {
            throw new InvalidOperationException($"No domain with id {domainId}. Call {AiToolNames.ListDomains} first.");
        }
    }

    private static void ValidateRecurrenceShape(TaskRecurrence recurrence)
    {
        switch (recurrence.RuleType)
        {
            case RecurrenceRuleType.IntervalDays when recurrence.IntervalDays is null or <= 0:
                throw new InvalidOperationException("interval_days must be a positive integer when rule_type is interval_days.");
            case RecurrenceRuleType.DaysOfWeek when recurrence.DaysOfWeek is null || recurrence.DaysOfWeek.Length == 0:
                throw new InvalidOperationException("days_of_week must contain at least one day (0=Sunday..6=Saturday) when rule_type is days_of_week.");
            case RecurrenceRuleType.DaysOfWeek when recurrence.DaysOfWeek.Any(d => d is < 0 or > 6):
                throw new InvalidOperationException("days_of_week entries must be between 0 (Sunday) and 6 (Saturday).");
            case RecurrenceRuleType.DayOfMonth when recurrence.DayOfMonth is null or < 1 or > 31:
                throw new InvalidOperationException("day_of_month must be between 1 and 31 when rule_type is day_of_month.");
        }
    }

    private static RecurrenceRuleType ParseRuleType(string value) => value.ToLowerInvariant() switch
    {
        "interval_days" => RecurrenceRuleType.IntervalDays,
        "days_of_week" => RecurrenceRuleType.DaysOfWeek,
        "day_of_month" => RecurrenceRuleType.DayOfMonth,
        _ => throw new InvalidOperationException($"Unknown rule_type '{value}'. Use interval_days, days_of_week or day_of_month.")
    };

    private static RecurrenceAnchorMode ParseAnchorMode(string? value) => value?.ToLowerInvariant() switch
    {
        null or "from_schedule" => RecurrenceAnchorMode.FromSchedule,
        "from_completion" => RecurrenceAnchorMode.FromCompletion,
        _ => throw new InvalidOperationException($"Unknown anchor_mode '{value}'. Use from_schedule or from_completion.")
    };

    private static TaskItemStatus? ParseStatus(string? value) => value?.ToLowerInvariant() switch
    {
        null => null,
        "pending" => TaskItemStatus.Pending,
        "done" => TaskItemStatus.Done,
        "skipped" => TaskItemStatus.Skipped,
        _ => throw new InvalidOperationException($"Unknown status '{value}'. Use pending, done or skipped.")
    };

    private static string Serialize(object value) => JsonSerializer.Serialize(value, SerializerOptions);

    /// <summary>Splices stored jsonb back in as JSON rather than as an escaped
    /// string, so the model sees {"reps":5} not "{\"reps\":5}".</summary>
    private static JsonElement RawJson(string json)
    {
        try
        {
            return JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json).RootElement.Clone();
        }
        catch (JsonException)
        {
            return JsonDocument.Parse("{}").RootElement.Clone();
        }
    }

    private static string GetObjectJson(JsonElement input, string name) =>
        input.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.Object
            ? value.GetRawText()
            : "{}";

    private static string? GetString(JsonElement input, string name) =>
        input.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string GetRequiredString(JsonElement input, string name) =>
        GetString(input, name) ?? throw new InvalidOperationException($"'{name}' is required.");

    private static int? GetInt(JsonElement input, string name)
    {
        if (!input.TryGetProperty(name, out JsonElement value))
        {
            return null;
        }

        // Models occasionally send numbers as strings; accept both rather than
        // bouncing the call over formatting.
        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt32(out int number) => number,
            JsonValueKind.String when int.TryParse(value.GetString(), out int parsed) => parsed,
            _ => null
        };
    }

    private static int GetRequiredInt(JsonElement input, string name) =>
        GetInt(input, name) ?? throw new InvalidOperationException($"'{name}' is required and must be an integer.");

    private static Guid GetRequiredGuid(JsonElement input, string name)
    {
        string raw = GetRequiredString(input, name);

        return Guid.TryParse(raw, out Guid id)
            ? id
            : throw new InvalidOperationException($"'{name}' must be a uuid, got '{raw}'.");
    }

    private static DateOnly? GetDate(JsonElement input, string name)
    {
        string? raw = GetString(input, name);

        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return DateOnly.TryParse(raw, out DateOnly date)
            ? date
            : throw new InvalidOperationException($"'{name}' must be a date in YYYY-MM-DD format, got '{raw}'.");
    }

    private static TimeOnly? GetTime(JsonElement input, string name)
    {
        string? raw = GetString(input, name);

        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return TimeOnly.TryParse(raw, out TimeOnly time)
            ? time
            : throw new InvalidOperationException($"'{name}' must be a time in HH:MM format, got '{raw}'.");
    }

    private static int[]? GetIntArray(JsonElement input, string name)
    {
        if (!input.TryGetProperty(name, out JsonElement value) || value.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        return value.EnumerateArray()
            .Where(e => e.ValueKind == JsonValueKind.Number)
            .Select(e => e.GetInt32())
            .ToArray();
    }
}
