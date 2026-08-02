namespace RandomTaskTrack.Data.Models.Ai;

public class AiToolResult
{
    public string ToolCallId { get; set; } = "";
    public string Content { get; set; } = "";
    public bool IsError { get; set; }
}
