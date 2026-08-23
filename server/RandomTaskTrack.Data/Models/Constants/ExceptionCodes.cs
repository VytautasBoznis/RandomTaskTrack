namespace RandomTaskTrack.Data.Models.Constants;

public static class ExceptionCodes
{
    public const string AUTH_EMAIL_AND_PASSWORD_MISSMATCH = "AUTH_EMAIL_AND_PASSWORD_MISSMATCH";
    public const string AUTH_EMAIL_ALREADY_EXISTS = "AUTH_EMAIL_ALREADY_EXISTS";
    public const string AUTH_CURRENT_PASSWORD_INVALID = "AUTH_CURRENT_PASSWORD_INVALID";
    public const string USER_UNAUTHORIZED = "USER_UNAUTHORIZED";

    public const string TASK_NOT_FOUND = "TASK_NOT_FOUND";
    public const string TASK_ALREADY_COMPLETED = "TASK_ALREADY_COMPLETED";
    public const string RECURRENCE_NOT_FOUND = "RECURRENCE_NOT_FOUND";
    public const string RECURRENCE_INVALID_RULE = "RECURRENCE_INVALID_RULE";
    public const string DOMAIN_NOT_FOUND = "DOMAIN_NOT_FOUND";
    public const string CONVERSATION_NOT_FOUND = "CONVERSATION_NOT_FOUND";

    public const string RECIPE_PICK_NOT_FOUND = "RECIPE_PICK_NOT_FOUND";
    public const string RECIPE_FAMILY_NOT_FOUND = "RECIPE_FAMILY_NOT_FOUND";
    public const string RECIPE_NO_FAMILIES = "RECIPE_NO_FAMILIES";
    public const string RECIPE_NO_NEW_DISHES = "RECIPE_NO_NEW_DISHES";
    public const string RECIPE_SOURCE_NOT_CONFIGURED = "RECIPE_SOURCE_NOT_CONFIGURED";
    public const string RECIPE_SOURCE_FAILED = "RECIPE_SOURCE_FAILED";

    public const string AI_PROVIDER_NOT_CONFIGURED = "AI_PROVIDER_NOT_CONFIGURED";
    public const string AI_PROVIDER_FAILED = "AI_PROVIDER_FAILED";
    public const string AI_TOOL_LIMIT_EXCEEDED = "AI_TOOL_LIMIT_EXCEEDED";

    public const string VALIDATION_FAILED = "VALIDATION_FAILED";
}
