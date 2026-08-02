using RandomTaskTrack.Data.Models.Enums;

namespace RandomTaskTrack.Data.Models.Ai;

public class AiResponse
{
    public string? Content { get; set; }
    public List<AiToolCall> ToolCalls { get; set; } = new();
    public AiStopReason StopReason { get; set; }
    public string Model { get; set; } = "";
    public AiUsage Usage { get; set; } = new();
}
