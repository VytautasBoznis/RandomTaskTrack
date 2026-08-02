namespace RandomTaskTrack.Data.Models.Ai;

/// <summary>
/// A tool offered to the model. InputSchema is raw JSON Schema — the one shape
/// every provider agrees on, which is why the abstraction sits at this level
/// rather than wrapping a provider's typed builders.
/// </summary>
public class AiToolDefinition
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string InputSchema { get; set; } = "{}";

    /// <summary>
    /// When true, the agent loop will not auto-execute this tool; it returns
    /// the pending call to the caller for confirmation. Set on anything
    /// destructive so the model can't delete thirty tasks unprompted.
    /// </summary>
    public bool RequiresConfirmation { get; set; }
}
