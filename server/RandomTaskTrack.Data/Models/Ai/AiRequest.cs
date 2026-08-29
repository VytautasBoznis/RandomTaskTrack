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

    /// <summary>
    /// Lets the model search the web for this request. A capability the caller
    /// asks for rather than a provider setting: only the caller knows whether
    /// the question is worth a search, and a provider that cannot search is
    /// free to ignore it and answer from what it knows.
    ///
    /// Searches are billed on top of tokens, so this is off unless asked for.
    /// </summary>
    public bool AllowWebSearch { get; set; }
}
