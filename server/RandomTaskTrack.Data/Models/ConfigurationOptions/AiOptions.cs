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

    /// <summary>Hard ceiling on agent-loop iterations, so a misbehaving model
    /// cannot spin up an unbounded number of billed round trips.</summary>
    public int MaxToolIterations { get; set; } = 8;

    /// <summary>Provider-specific knobs (Anthropic effort/thinking, etc). Kept
    /// out of IAiProvider on purpose — forcing these into a common shape gives
    /// a lowest-common-denominator abstraction.</summary>
    public Dictionary<string, string> ProviderOptions { get; set; } = new();
}
