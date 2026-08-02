namespace RandomTaskTrack.Data.Dtos.Chat;

/// <summary>What the AI actually did during a turn, surfaced to the UI so the
/// user can see the writes rather than having to trust them.</summary>
public class AppliedToolCallDto
{
    public string Name { get; set; } = "";
    public string Input { get; set; } = "{}";
    public string Result { get; set; } = "";
    public bool IsError { get; set; }
}
