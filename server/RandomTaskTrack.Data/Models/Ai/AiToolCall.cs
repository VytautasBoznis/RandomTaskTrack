namespace RandomTaskTrack.Data.Models.Ai;

/// <summary>A tool invocation requested by the model.</summary>
public class AiToolCall
{
    /// <summary>Provider-assigned id. Must be echoed back on the result so the
    /// provider can pair them.</summary>
    public string Id { get; set; } = "";

    public string Name { get; set; } = "";

    /// <summary>Raw JSON object of arguments, unparsed. Never string-match this
    /// — providers differ in unicode/slash escaping.</summary>
    public string JsonInput { get; set; } = "{}";
}
