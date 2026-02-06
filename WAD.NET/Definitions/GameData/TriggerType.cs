namespace WAD.NET.Definitions.GameData
{
    /// <summary>
    /// How a linedef action is triggered.
    /// </summary>
    public enum TriggerType
    {
        /// <summary>Walk across the linedef (W).</summary>
        Walk,

        /// <summary>Use/activate a switch (S).</summary>
        Switch,

        /// <summary>Shoot the linedef (G).</summary>
        Gun,

        /// <summary>Push against the wall (P).</summary>
        Push,

        /// <summary>Automatically triggered.</summary>
        Automatic
    }
}
