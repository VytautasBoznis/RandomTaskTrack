namespace RandomTaskTrack.Data.Models.Enums;

public enum AiStopReason
{
    EndTurn,
    ToolUse,
    MaxTokens,
    Refusal,
    Other
}
