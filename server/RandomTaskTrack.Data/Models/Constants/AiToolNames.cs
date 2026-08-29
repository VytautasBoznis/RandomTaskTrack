namespace RandomTaskTrack.Data.Models.Constants;

/// <summary>
/// The tool surface exposed to the AI. Kept in one place so the registry, the
/// system prompt and the confirmation gate can never drift apart.
/// </summary>
public static class AiToolNames
{
    public const string ListDomains = "list_domains";
    public const string QueryTasks = "query_tasks";
    public const string CreateTask = "create_task";
    public const string UpdateTask = "update_task";
    public const string DeleteTask = "delete_task";
    public const string CreateRecurrence = "create_recurrence";
    public const string UpdateRecurrence = "update_recurrence";
    public const string DeleteRecurrence = "delete_recurrence";
    public const string QueryCompletionLog = "query_completion_log";

    // Finance. query_finance and project_finances are the only sources of a
    // money figure — see the "Money" section of the system prompt.
    public const string QueryFinance = "query_finance";
    public const string ProjectFinances = "project_finances";
    public const string CreateFlow = "create_flow";
    public const string UpdateFlow = "update_flow";
    public const string DeleteFlow = "delete_flow";
    public const string LogEntry = "log_entry";
    public const string QueryEntries = "query_entries";
    public const string CreateHolding = "create_holding";
    public const string LogTrade = "log_trade";
    public const string CreateDividend = "create_dividend";
    public const string CreateDeposit = "create_deposit";
    public const string CreateTarget = "create_target";
}
