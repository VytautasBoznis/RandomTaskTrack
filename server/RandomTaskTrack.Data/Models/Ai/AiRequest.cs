namespace RandomTaskTrack.Data.Models.Ai;

public class AiRequest
{
    public string SystemPrompt { get; set; } = "";
    public List<AiMessage> Messages { get; set; } = new();
    public List<AiToolDefinition> Tools { get; set; } = new();
    public int MaxTokens { get; set; } = 8000;

    /// <summary>Overrides the configured model for this call. Lets a cheap
    /// route (title generation) run on a smaller model than the chat loop.</summary>
    public string? ModelOverride { get; set; }
}
