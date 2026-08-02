using RandomTaskTrack.Data.Models.Enums;

namespace RandomTaskTrack.Data.Models.Ai;

public class AiMessage
{
    public AiMessageRole Role { get; set; }
    public string? Content { get; set; }

    /// <summary>Set on assistant turns where the model asked for tools.</summary>
    public List<AiToolCall> ToolCalls { get; set; } = new();

    /// <summary>Set on tool turns. All results for one assistant turn must be
    /// returned together — splitting them trains the model out of parallel
    /// tool calls.</summary>
    public List<AiToolResult> ToolResults { get; set; } = new();

    public static AiMessage FromUser(string content) =>
        new() { Role = AiMessageRole.User, Content = content };

    public static AiMessage FromAssistant(string? content, List<AiToolCall>? toolCalls = null) =>
        new() { Role = AiMessageRole.Assistant, Content = content, ToolCalls = toolCalls ?? new() };

    public static AiMessage FromToolResults(List<AiToolResult> results) =>
        new() { Role = AiMessageRole.Tool, ToolResults = results };
}
