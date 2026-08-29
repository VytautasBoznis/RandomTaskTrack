namespace RandomTaskTrack.Data.Models.ConfigurationOptions;

public class AiOptions
{
    /// <summary>Selects the IAiProvider implementation. See AiProviderNames.</summary>
    public string Provider { get; set; } = "anthropic";

    public string Model { get; set; } = "claude-opus-5";
    public string ApiKey { get; set; } = "";

    /// <summary>Base URL override. Only needed for OpenAI-compatible / local providers.</summary>
    public string? BaseUrl { get; set; }

    public int MaxTokens { get; set; } = 8000;

    /// <summary>
    /// Whether this deployment's key may use server-side web search. A veto,
    /// not a request: callers ask for search per request (AiRequest), and this
    /// can only take it away. Off is the setting for a key whose organisation
    /// has not enabled the tool, or when the per-search charge is not wanted —
    /// the plant lookup then answers from what the model already knows.
    /// </summary>
    public bool WebSearch { get; set; } = true;

    /// <summary>Hard ceiling on agent-loop iterations, so a misbehaving model
    /// cannot spin up an unbounded number of billed round trips.</summary>
    public int MaxToolIterations { get; set; } = 8;

    /// <summary>Provider-specific knobs (Anthropic effort/thinking, etc). Kept
    /// out of IAiProvider on purpose — forcing these into a common shape gives
    /// a lowest-common-denominator abstraction.</summary>
    public Dictionary<string, string> ProviderOptions { get; set; } = new();
}
