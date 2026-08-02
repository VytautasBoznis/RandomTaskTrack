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
}
