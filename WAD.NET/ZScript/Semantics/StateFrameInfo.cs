namespace WAD.NET.ZScript.Semantics;

/// <summary>
/// Holds information about a single state frame within a state label group.
/// </summary>
public class StateFrameInfo
{
    /// <summary>
    /// The 4-character sprite name (e.g., "POSS", "PLAY").
    /// </summary>
    public string SpriteName { get; set; } = "";

    /// <summary>
    /// The frame letter(s) (e.g., "A", "ABCD").
    /// </summary>
    public string FrameLetters { get; set; } = "";

    /// <summary>
    /// The duration in tics.
    /// </summary>
    public int Duration { get; set; }

    /// <summary>
    /// Whether the <c>Bright</c> modifier is present.
    /// </summary>
    public bool IsBright { get; set; }

    /// <summary>
    /// The name of the action function called on this frame, or <see langword="null"/> if none.
    /// </summary>
    public string? ActionName { get; set; }
}
