namespace WAD.NET.Definitions.GameData
{
    /// <summary>
    /// Type classification for linedef actions.
    /// </summary>
    public enum LinedefActionType
    {
        /// <summary>No action.</summary>
        None,

        /// <summary>Door open/close actions.</summary>
        Door,

        /// <summary>Floor raise/lower actions.</summary>
        Floor,

        /// <summary>Ceiling raise/lower actions.</summary>
        Ceiling,

        /// <summary>Moving platform actions.</summary>
        Platform,

        /// <summary>Lift actions.</summary>
        Lift,

        /// <summary>Stair building actions.</summary>
        Stairs,

        /// <summary>Crusher ceiling actions.</summary>
        Crusher,

        /// <summary>Light level change actions.</summary>
        Light,

        /// <summary>Teleportation actions.</summary>
        Teleport,

        /// <summary>Level exit actions.</summary>
        Exit,

        /// <summary>Scrolling texture actions.</summary>
        Scroll,

        /// <summary>Other special actions.</summary>
        Special
    }
}
