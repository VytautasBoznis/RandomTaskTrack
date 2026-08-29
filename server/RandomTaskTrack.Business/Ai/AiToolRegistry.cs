using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Data.Dtos.Finance;
using RandomTaskTrack.Data.Models.Ai;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Models.Finance;
using RandomTaskTrack.Data.Models.Tasks;
using RandomTaskTrack.Interfaces.Ai;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Domains;
using RandomTaskTrack.Interfaces.Repositories.Finance;
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
    private readonly IFinanceRepository _financeRepository;
    private readonly IRecurrenceMaterializer _materializer;
    private readonly IFinanceProjector _projector;
    private readonly IClock _clock;
    private readonly ILogger<AiToolRegistry> _logger;

    public AiToolRegistry(
        ITasksRepository tasksRepository,
        IRecurrencesRepository recurrencesRepository,
        ICompletionsRepository completionsRepository,
        IDomainsRepository domainsRepository,
        IFinanceRepository financeRepository,
        IRecurrenceMaterializer materializer,
        IFinanceProjector projector,
        IClock clock,
        ILogger<AiToolRegistry> logger)
    {
        _tasksRepository = tasksRepository;
        _recurrencesRepository = recurrencesRepository;
        _completionsRepository = completionsRepository;
        _domainsRepository = domainsRepository;
        _financeRepository = financeRepository;
        _materializer = materializer;
        _projector = projector;
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
        },

        // ── Finance ──────────────────────────────────────────────────────────
        new()
        {
            Name = AiToolNames.QueryFinance,
            Description = "The money picture right now: cash, deposits, holdings, net worth, the recurring flows, expected dividends and targets. Every figure is already converted to the base currency. Call this before saying anything about money.",
            InputSchema = """
                {"type":"object","properties":{},"additionalProperties":false}
                """
        },
        new()
        {
            Name = AiToolNames.ProjectFinances,
            Description = "Project cash, deposits, holdings and net worth forward, month by month. Use this for any 'when will I have X' or 'can I afford Y' question instead of doing the arithmetic yourself.",
            InputSchema = """
                {
                  "type":"object",
                  "properties":{
                    "months":{"type":"integer","description":"Months to project forward, 1-600. Default 60."},
                    "history_months":{"type":"integer","description":"Months of actual ledger history to include behind today. Default 12."},
                    "stock_growth_pct":{"type":"number","description":"Assumed annual return on holdings, as a percentage. Defaults to 0, which holds them at the last pulled price. Say which figure you used."}
                  },
                  "additionalProperties":false
                }
                """
        },
        new()
        {
            Name = AiToolNames.CreateFlow,
            Description = "Create a recurring income or expense. This is a definition of what is supposed to happen, not a record that it did — use log_entry for money that actually moved.",
            InputSchema = """
                {
                  "type":"object",
                  "properties":{
                    "kind":{"type":"string","enum":["income","expense"]},
                    "name":{"type":"string"},
                    "amount":{"type":"number","description":"Positive. kind carries the direction."},
                    "currency":{"type":"string","description":"ISO code, e.g. EUR. Must already exist in the currency table."},
                    "cadence":{"type":"string","enum":["weekly","monthly","quarterly","yearly"]},
                    "day_of_month":{"type":"integer","description":"1-31. Optional; defaults to the day of starts_on. Clamped in short months."},
                    "month_of_year":{"type":"integer","description":"1-12, for yearly only. Optional; defaults to the month of starts_on."},
                    "starts_on":{"type":"string","description":"YYYY-MM-DD. Every cadence is anchored on this."},
                    "ends_on":{"type":"string","description":"YYYY-MM-DD, optional."},
                    "category":{"type":"string","description":"Free text, for grouping."}
                  },
                  "required":["kind","name","amount","currency","cadence","starts_on"],
                  "additionalProperties":false
                }
                """
        },
        new()
        {
            Name = AiToolNames.UpdateFlow,
            Description = "Change a recurring flow. Only the fields you pass are changed. Set is_active false to pause it without losing it. Kind cannot be changed - delete and recreate instead.",
            InputSchema = """
                {
                  "type":"object",
                  "properties":{
                    "id":{"type":"string","description":"Flow id (uuid)."},
                    "name":{"type":"string"},
                    "amount":{"type":"number"},
                    "currency":{"type":"string"},
                    "cadence":{"type":"string","enum":["weekly","monthly","quarterly","yearly"]},
                    "day_of_month":{"type":"integer"},
                    "month_of_year":{"type":"integer"},
                    "starts_on":{"type":"string"},
                    "ends_on":{"type":"string"},
                    "category":{"type":"string"},
                    "is_active":{"type":"boolean"}
                  },
                  "required":["id"],
                  "additionalProperties":false
                }
                """
        },
        new()
        {
            Name = AiToolNames.DeleteFlow,
            Description = "Delete a recurring flow. Ledger entries that came from it are kept and simply lose the link. Destructive.",
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
            Name = AiToolNames.LogEntry,
            Description = "Record money that actually moved. This is what current cash is derived from, so log real income and real expenses here rather than adjusting a balance.",
            InputSchema = """
                {
                  "type":"object",
                  "properties":{
                    "kind":{"type":"string","enum":["income","expense"]},
                    "name":{"type":"string"},
                    "amount":{"type":"number","description":"Positive."},
                    "currency":{"type":"string"},
                    "occurred_on":{"type":"string","description":"YYYY-MM-DD."},
                    "flow_id":{"type":"string","description":"The recurring flow this was an instance of, if it was one."},
                    "category":{"type":"string"},
                    "note":{"type":"string"}
                  },
                  "required":["kind","name","amount","currency","occurred_on"],
                  "additionalProperties":false
                }
                """
        },
        new()
        {
            Name = AiToolNames.QueryEntries,
            Description = "Read the ledger - what actually happened, as opposed to what the flows say should. Use this before commenting on spending.",
            InputSchema = """
                {
                  "type":"object",
                  "properties":{
                    "from_date":{"type":"string","description":"YYYY-MM-DD."},
                    "to_date":{"type":"string","description":"YYYY-MM-DD."},
                    "kind":{"type":"string","enum":["income","expense"]},
                    "search":{"type":"string","description":"Substring of the name or category."},
                    "limit":{"type":"integer","description":"Max rows, default 100."}
                  },
                  "additionalProperties":false
                }
                """
        },
        new()
        {
            Name = AiToolNames.CreateHolding,
            Description = "Start tracking a stock. The symbol must be in the price source vocabulary: AAPL for Nasdaq, ASML.AS for Amsterdam. Adding the holding does not add any shares - follow it with log_trade.",
            InputSchema = """
                {
                  "type":"object",
                  "properties":{
                    "symbol":{"type":"string"},
                    "name":{"type":"string"},
                    "currency":{"type":"string","description":"The currency the stock is quoted in, e.g. USD."}
                  },
                  "required":["symbol","currency"],
                  "additionalProperties":false
                }
                """
        },
        new()
        {
            Name = AiToolNames.LogTrade,
            Description = "Record a buy or a sell. The position is the sum of trades, so this is also how a mistake is corrected - add the missing trade rather than adjusting a total.",
            InputSchema = """
                {
                  "type":"object",
                  "properties":{
                    "holding_id":{"type":"string","description":"From query_finance."},
                    "side":{"type":"string","enum":["buy","sell"]},
                    "quantity":{"type":"number","description":"Positive. Fractional shares are allowed."},
                    "price":{"type":"number","description":"Per share, in the holding currency."},
                    "fee":{"type":"number","description":"Commission etc. Optional, defaults to 0."},
                    "traded_on":{"type":"string","description":"YYYY-MM-DD."},
                    "note":{"type":"string"}
                  },
                  "required":["holding_id","side","quantity","price","traded_on"],
                  "additionalProperties":false
                }
                """
        },
        new()
        {
            Name = AiToolNames.CreateDividend,
            Description = "Record a dividend you expect to be paid, recurring. This is an expectation that feeds the projection; a dividend that actually landed is a log_entry income instead.",
            InputSchema = """
                {
                  "type":"object",
                  "properties":{
                    "name":{"type":"string"},
                    "amount":{"type":"number","description":"Per payment, not per year."},
                    "currency":{"type":"string"},
                    "cadence":{"type":"string","enum":["weekly","monthly","quarterly","yearly"]},
                    "holding_id":{"type":"string","description":"Optional - which position pays it."},
                    "day_of_month":{"type":"integer"},
                    "month_of_year":{"type":"integer"},
                    "starts_on":{"type":"string","description":"YYYY-MM-DD."},
                    "ends_on":{"type":"string"}
                  },
                  "required":["name","amount","currency","cadence","starts_on"],
                  "additionalProperties":false
                }
                """
        },
        new()
        {
            Name = AiToolNames.CreateDeposit,
            Description = "Record money parked at a known interest rate. Unlike a stock the growth is contractual, so the projection values these exactly.",
            InputSchema = """
                {
                  "type":"object",
                  "properties":{
                    "name":{"type":"string"},
                    "principal":{"type":"number"},
                    "currency":{"type":"string"},
                    "annual_rate":{"type":"number","description":"A percentage as the bank writes it: 4.25 means 4.25%, not 0.0425."},
                    "compounding":{"type":"string","enum":["simple","monthly","annual"],"description":"Defaults to annual."},
                    "opened_on":{"type":"string","description":"YYYY-MM-DD."},
                    "matures_on":{"type":"string","description":"YYYY-MM-DD. Omit for an open-ended savings account."},
                    "note":{"type":"string"}
                  },
                  "required":["name","principal","currency","annual_rate","opened_on"],
                  "additionalProperties":false
                }
                """
        },
        new()
        {
            Name = AiToolNames.CreateTarget,
            Description = "Put a mark on the projection graph. An amount alone draws a goal line, a date alone marks a milestone, both together is a point to hit.",
            InputSchema = """
                {
                  "type":"object",
                  "properties":{
                    "label":{"type":"string"},
                    "target_on":{"type":"string","description":"YYYY-MM-DD."},
                    "amount":{"type":"number","description":"In the base currency."},
                    "note":{"type":"string"}
                  },
                  "required":["label"],
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
                AiToolNames.QueryFinance => await QueryFinanceAsync(unitOfWork),
                AiToolNames.ProjectFinances => await ProjectFinancesAsync(input, unitOfWork),
                AiToolNames.CreateFlow => await CreateFlowAsync(input, unitOfWork),
                AiToolNames.UpdateFlow => await UpdateFlowAsync(input, unitOfWork),
                AiToolNames.DeleteFlow => await DeleteFlowAsync(input, unitOfWork),
                AiToolNames.LogEntry => await LogEntryAsync(input, unitOfWork),
                AiToolNames.QueryEntries => await QueryEntriesAsync(input, unitOfWork),
                AiToolNames.CreateHolding => await CreateHoldingAsync(input, unitOfWork),
                AiToolNames.LogTrade => await LogTradeAsync(input, unitOfWork),
                AiToolNames.CreateDividend => await CreateDividendAsync(input, unitOfWork),
                AiToolNames.CreateDeposit => await CreateDepositAsync(input, unitOfWork),
                AiToolNames.CreateTarget => await CreateTargetAsync(input, unitOfWork),
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

    // ── Finance handlers ─────────────────────────────────────────────────────

    private async Task<string> QueryFinanceAsync(IUnitOfWork unitOfWork)
    {
        FinanceOverviewDto overview = await _projector.BuildOverviewAsync(unitOfWork);

        return Serialize(new
        {
            today = overview.Today.ToString("yyyy-MM-dd"),
            base_currency = overview.BaseCurrency,
            cash = overview.CashBase,
            deposits = overview.DepositsBase,
            stocks = overview.StocksBase,
            net_worth = overview.NetWorthBase,
            monthly_income = overview.MonthlyIncomeBase,
            monthly_expenses = overview.MonthlyExpenseBase,

            // Named so the model reports the caveat rather than the total alone.
            some_holdings_have_no_price = overview.HasUnpricedHoldings,

            flows = overview.Flows.Select(f => new
            {
                id = f.Id,
                kind = f.Kind.ToString().ToLowerInvariant(),
                name = f.Name,
                amount = f.Amount,
                currency = f.Currency,
                cadence = f.Cadence.ToString().ToLowerInvariant(),
                day_of_month = f.DayOfMonth,
                month_of_year = f.MonthOfYear,
                starts_on = f.StartsOn.ToString("yyyy-MM-dd"),
                ends_on = f.EndsOn?.ToString("yyyy-MM-dd"),
                category = f.Category,
                is_active = f.IsActive
            }),
            positions = overview.Positions.Select(p => new
            {
                id = p.Id,
                symbol = p.Symbol,
                name = p.Name,
                currency = p.Currency,
                quantity = p.Quantity,
                last_price = p.LastPrice,
                cost_basis = p.CostBasis,
                market_value = p.MarketValue,
                market_value_base = p.MarketValueBase
            }),
            deposit_accounts = overview.Deposits.Select(d => new
            {
                id = d.Id,
                name = d.Name,
                principal = d.Principal,
                currency = d.Currency,
                annual_rate_pct = d.AnnualRate,
                compounding = d.Compounding.ToString().ToLowerInvariant(),
                opened_on = d.OpenedOn.ToString("yyyy-MM-dd"),
                matures_on = d.MaturesOn?.ToString("yyyy-MM-dd")
            }),
            dividends = overview.Dividends.Select(d => new
            {
                id = d.Id,
                name = d.Name,
                amount = d.Amount,
                currency = d.Currency,
                cadence = d.Cadence.ToString().ToLowerInvariant(),
                holding_id = d.HoldingId,
                is_active = d.IsActive
            }),
            targets = overview.Targets.Select(t => new
            {
                id = t.Id,
                label = t.Label,
                target_on = t.TargetOn?.ToString("yyyy-MM-dd"),
                amount = t.Amount
            })
        });
    }

    private async Task<string> ProjectFinancesAsync(JsonElement input, IUnitOfWork unitOfWork)
    {
        int months = GetInt(input, "months") ?? 60;
        int historyMonths = GetInt(input, "history_months") ?? 12;
        decimal growth = GetDecimal(input, "stock_growth_pct") ?? 0m;

        if (months is < 1 or > 600)
        {
            throw new InvalidOperationException("months must be between 1 and 600.");
        }

        List<ProjectionPointDto> points = await _projector.ProjectAsync(historyMonths, months, growth, unitOfWork);

        return Serialize(new
        {
            stock_growth_pct_used = growth,
            points = points.Select(p => new
            {
                month = p.Month.ToString("yyyy-MM"),
                is_actual = p.IsActual,
                income = p.Income,
                expenses = p.Expenses,
                net = p.Net,
                cash = p.Cash,
                deposits = p.Deposits,
                stocks = p.Stocks,
                net_worth = p.NetWorth
            })
        });
    }

    private async Task<string> CreateFlowAsync(JsonElement input, IUnitOfWork unitOfWork)
    {
        var flow = new FinanceFlow
        {
            Id = Guid.NewGuid(),
            Kind = ParseFlowKind(GetRequiredString(input, "kind")),
            Name = GetRequiredString(input, "name"),
            Amount = GetRequiredDecimal(input, "amount"),
            Currency = await ResolveCurrencyAsync(GetRequiredString(input, "currency"), unitOfWork),
            Cadence = ParseCadence(GetRequiredString(input, "cadence")),
            DayOfMonth = GetInt(input, "day_of_month"),
            MonthOfYear = GetInt(input, "month_of_year"),
            StartsOn = GetDate(input, "starts_on") ?? throw new InvalidOperationException("starts_on is required (YYYY-MM-DD)."),
            EndsOn = GetDate(input, "ends_on"),
            Category = GetString(input, "category"),
            IsActive = true
        };

        if (flow.Amount <= 0)
        {
            throw new InvalidOperationException("amount must be positive; kind decides income or expense.");
        }

        await _financeRepository.CreateFlowAsync(flow, unitOfWork);

        return Serialize(new { created = true, id = flow.Id, name = flow.Name });
    }

    private async Task<string> UpdateFlowAsync(JsonElement input, IUnitOfWork unitOfWork)
    {
        Guid id = GetRequiredGuid(input, "id");
        FinanceFlow flow = await _financeRepository.GetFlowAsync(id, unitOfWork)
                           ?? throw new InvalidOperationException($"No flow with id {id}.");

        flow.Name = GetString(input, "name") ?? flow.Name;
        flow.Amount = GetDecimal(input, "amount") ?? flow.Amount;
        flow.DayOfMonth = GetInt(input, "day_of_month") ?? flow.DayOfMonth;
        flow.MonthOfYear = GetInt(input, "month_of_year") ?? flow.MonthOfYear;
        flow.StartsOn = GetDate(input, "starts_on") ?? flow.StartsOn;
        flow.EndsOn = GetDate(input, "ends_on") ?? flow.EndsOn;
        flow.Category = GetString(input, "category") ?? flow.Category;

        string? cadence = GetString(input, "cadence");
        if (cadence is not null) flow.Cadence = ParseCadence(cadence);

        string? currency = GetString(input, "currency");
        if (currency is not null) flow.Currency = await ResolveCurrencyAsync(currency, unitOfWork);

        if (input.TryGetProperty("is_active", out JsonElement active) &&
            active.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            flow.IsActive = active.GetBoolean();
        }

        await _financeRepository.UpdateFlowAsync(flow, unitOfWork);

        return Serialize(new { updated = true, id = flow.Id });
    }

    private async Task<string> DeleteFlowAsync(JsonElement input, IUnitOfWork unitOfWork)
    {
        Guid id = GetRequiredGuid(input, "id");
        bool deleted = await _financeRepository.DeleteFlowAsync(id, unitOfWork);

        return Serialize(new { deleted, id });
    }

    private async Task<string> LogEntryAsync(JsonElement input, IUnitOfWork unitOfWork)
    {
        Guid? flowId = null;

        if (GetString(input, "flow_id") is not null)
        {
            flowId = GetRequiredGuid(input, "flow_id");

            if (await _financeRepository.GetFlowAsync(flowId.Value, unitOfWork) is null)
            {
                throw new InvalidOperationException($"No flow with id {flowId}.");
            }
        }

        var entry = new LedgerEntry
        {
            Id = Guid.NewGuid(),
            FlowId = flowId,
            Kind = ParseFlowKind(GetRequiredString(input, "kind")),
            Name = GetRequiredString(input, "name"),
            Amount = GetRequiredDecimal(input, "amount"),
            Currency = await ResolveCurrencyAsync(GetRequiredString(input, "currency"), unitOfWork),
            OccurredOn = GetDate(input, "occurred_on") ?? throw new InvalidOperationException("occurred_on is required (YYYY-MM-DD)."),
            Category = GetString(input, "category"),
            Note = GetString(input, "note")
        };

        if (entry.Amount <= 0)
        {
            throw new InvalidOperationException("amount must be positive; kind decides income or expense.");
        }

        await _financeRepository.CreateEntryAsync(entry, unitOfWork);

        return Serialize(new { logged = true, id = entry.Id, occurred_on = entry.OccurredOn.ToString("yyyy-MM-dd") });
    }

    private async Task<string> QueryEntriesAsync(JsonElement input, IUnitOfWork unitOfWork)
    {
        string? kind = GetString(input, "kind");

        var entries = await _financeRepository.QueryEntriesAsync(
            GetDate(input, "from_date"),
            GetDate(input, "to_date"),
            kind is null ? null : ParseFlowKind(kind),
            GetString(input, "search"),
            GetInt(input, "limit") ?? 100,
            unitOfWork);

        return Serialize(entries.Select(e => new
        {
            id = e.Id,
            kind = e.Kind.ToString().ToLowerInvariant(),
            name = e.Name,
            amount = e.Amount,
            currency = e.Currency,
            occurred_on = e.OccurredOn.ToString("yyyy-MM-dd"),
            category = e.Category,
            note = e.Note,
            flow_id = e.FlowId
        }));
    }

    private async Task<string> CreateHoldingAsync(JsonElement input, IUnitOfWork unitOfWork)
    {
        string symbol = GetRequiredString(input, "symbol").Trim();

        if (await _financeRepository.GetHoldingBySymbolAsync(symbol, unitOfWork) is not null)
        {
            throw new InvalidOperationException($"{symbol} is already tracked. Use log_trade to add shares to it.");
        }

        var holding = new Holding
        {
            Id = Guid.NewGuid(),
            Symbol = symbol,
            Name = GetString(input, "name"),
            Currency = await ResolveCurrencyAsync(GetRequiredString(input, "currency"), unitOfWork)
        };

        await _financeRepository.CreateHoldingAsync(holding, unitOfWork);

        return Serialize(new { created = true, id = holding.Id, symbol = holding.Symbol });
    }

    private async Task<string> LogTradeAsync(JsonElement input, IUnitOfWork unitOfWork)
    {
        Guid holdingId = GetRequiredGuid(input, "holding_id");

        if (await _financeRepository.GetHoldingAsync(holdingId, unitOfWork) is null)
        {
            throw new InvalidOperationException($"No holding with id {holdingId}. Call {AiToolNames.QueryFinance} first.");
        }

        var trade = new Trade
        {
            Id = Guid.NewGuid(),
            HoldingId = holdingId,
            Side = ParseTradeSide(GetRequiredString(input, "side")),
            Quantity = GetRequiredDecimal(input, "quantity"),
            Price = GetRequiredDecimal(input, "price"),
            Fee = GetDecimal(input, "fee") ?? 0m,
            TradedOn = GetDate(input, "traded_on") ?? throw new InvalidOperationException("traded_on is required (YYYY-MM-DD)."),
            Note = GetString(input, "note")
        };

        if (trade.Quantity <= 0)
        {
            throw new InvalidOperationException("quantity must be positive; side decides buy or sell.");
        }

        await _financeRepository.CreateTradeAsync(trade, unitOfWork);

        return Serialize(new { logged = true, id = trade.Id });
    }

    private async Task<string> CreateDividendAsync(JsonElement input, IUnitOfWork unitOfWork)
    {
        Guid? holdingId = null;

        if (GetString(input, "holding_id") is not null)
        {
            holdingId = GetRequiredGuid(input, "holding_id");

            if (await _financeRepository.GetHoldingAsync(holdingId.Value, unitOfWork) is null)
            {
                throw new InvalidOperationException($"No holding with id {holdingId}.");
            }
        }

        var dividend = new Dividend
        {
            Id = Guid.NewGuid(),
            HoldingId = holdingId,
            Name = GetRequiredString(input, "name"),
            Amount = GetRequiredDecimal(input, "amount"),
            Currency = await ResolveCurrencyAsync(GetRequiredString(input, "currency"), unitOfWork),
            Cadence = ParseCadence(GetRequiredString(input, "cadence")),
            DayOfMonth = GetInt(input, "day_of_month"),
            MonthOfYear = GetInt(input, "month_of_year"),
            StartsOn = GetDate(input, "starts_on") ?? throw new InvalidOperationException("starts_on is required (YYYY-MM-DD)."),
            EndsOn = GetDate(input, "ends_on"),
            IsActive = true
        };

        await _financeRepository.CreateDividendAsync(dividend, unitOfWork);

        return Serialize(new { created = true, id = dividend.Id });
    }

    private async Task<string> CreateDepositAsync(JsonElement input, IUnitOfWork unitOfWork)
    {
        var deposit = new Deposit
        {
            Id = Guid.NewGuid(),
            Name = GetRequiredString(input, "name"),
            Principal = GetRequiredDecimal(input, "principal"),
            Currency = await ResolveCurrencyAsync(GetRequiredString(input, "currency"), unitOfWork),
            AnnualRate = GetRequiredDecimal(input, "annual_rate"),
            Compounding = ParseCompounding(GetString(input, "compounding")),
            OpenedOn = GetDate(input, "opened_on") ?? throw new InvalidOperationException("opened_on is required (YYYY-MM-DD)."),
            MaturesOn = GetDate(input, "matures_on"),
            Note = GetString(input, "note")
        };

        // 4.25 means 4.25%. A rate above 100 is almost always a fraction typed
        // as a percentage twice over, and it would silently wreck the projection.
        if (deposit.AnnualRate is < 0 or > 100)
        {
            throw new InvalidOperationException("annual_rate is a percentage between 0 and 100: 4.25 means 4.25%.");
        }

        await _financeRepository.CreateDepositAsync(deposit, unitOfWork);

        return Serialize(new { created = true, id = deposit.Id });
    }

    private async Task<string> CreateTargetAsync(JsonElement input, IUnitOfWork unitOfWork)
    {
        var target = new FinanceTarget
        {
            Id = Guid.NewGuid(),
            Label = GetRequiredString(input, "label"),
            TargetOn = GetDate(input, "target_on"),
            Amount = GetDecimal(input, "amount"),
            Note = GetString(input, "note")
        };

        if (target.TargetOn is null && target.Amount is null)
        {
            throw new InvalidOperationException("A target needs a date, an amount, or both — otherwise there is nothing to draw.");
        }

        await _financeRepository.CreateTargetAsync(target, unitOfWork);

        return Serialize(new { created = true, id = target.Id });
    }

    /// <summary>
    /// Money tables have a foreign key to a plain-text currency code, so a
    /// lowercase code from the model would be rejected by the database rather
    /// than corrected here. Mirrors FinanceGuards for the operation path.
    /// </summary>
    private async Task<string> ResolveCurrencyAsync(string code, IUnitOfWork unitOfWork)
    {
        Data.Models.Finance.Currency? currency = await _financeRepository.GetCurrencyAsync(code, unitOfWork);

        return currency?.Code
               ?? throw new InvalidOperationException($"Unknown currency '{code}'. It must already exist in the currency table.");
    }

    private static FinanceFlowKind ParseFlowKind(string value) => value.ToLowerInvariant() switch
    {
        "income" => FinanceFlowKind.Income,
        "expense" => FinanceFlowKind.Expense,
        _ => throw new InvalidOperationException($"Unknown kind '{value}'. Use income or expense.")
    };

    private static FinanceCadence ParseCadence(string value) => value.ToLowerInvariant() switch
    {
        "weekly" => FinanceCadence.Weekly,
        "monthly" => FinanceCadence.Monthly,
        "quarterly" => FinanceCadence.Quarterly,
        "yearly" => FinanceCadence.Yearly,
        _ => throw new InvalidOperationException($"Unknown cadence '{value}'. Use weekly, monthly, quarterly or yearly.")
    };

    private static TradeSide ParseTradeSide(string value) => value.ToLowerInvariant() switch
    {
        "buy" => TradeSide.Buy,
        "sell" => TradeSide.Sell,
        _ => throw new InvalidOperationException($"Unknown side '{value}'. Use buy or sell.")
    };

    private static DepositCompounding ParseCompounding(string? value) => value?.ToLowerInvariant() switch
    {
        null or "" or "annual" => DepositCompounding.Annual,
        "simple" => DepositCompounding.Simple,
        "monthly" => DepositCompounding.Monthly,
        _ => throw new InvalidOperationException($"Unknown compounding '{value}'. Use simple, monthly or annual.")
    };

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

    private static decimal? GetDecimal(JsonElement input, string name)
    {
        if (!input.TryGetProperty(name, out JsonElement value))
        {
            return null;
        }

        // Same tolerance as GetInt: models send money as "1200.50" often enough
        // that bouncing the call over quoting would just cost a round trip.
        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetDecimal(out decimal number) => number,
            JsonValueKind.String when decimal.TryParse(
                value.GetString(),
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out decimal parsed) => parsed,
            _ => null
        };
    }

    private static decimal GetRequiredDecimal(JsonElement input, string name) =>
        GetDecimal(input, name) ?? throw new InvalidOperationException($"'{name}' is required and must be a number.");

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
