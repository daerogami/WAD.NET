namespace WAD.NET.Definitions.GameData
{
    /// <summary>
    /// Defines a sector special type with its effects and properties.
    /// </summary>
    public class SectorSpecialDefinition
    {
        /// <summary>
        /// Sector special number.
        /// </summary>
        public int Special { get; init; }

        /// <summary>
        /// Short name describing the special (e.g., "Light Blink (random)").
        /// </summary>
        public string Name { get; init; } = "";

        /// <summary>
        /// Detailed description of the special's behavior.
        /// </summary>
        public string Description { get; init; } = "";

        /// <summary>
        /// Type classification for this special.
        /// </summary>
        public SectorSpecialType Type { get; init; }

        /// <summary>
        /// Amount of damage dealt per interval (0 if no damage).
        /// </summary>
        public int DamageAmount { get; init; }

        /// <summary>
        /// Number of tics between damage applications.
        /// </summary>
        public int DamageInterval { get; init; }

        /// <summary>
        /// True if the radiation suit does not fully protect against this damage.
        /// </summary>
        public bool LeakySuit { get; init; }

        /// <summary>
        /// Lighting effect applied by this sector special.
        /// </summary>
        public LightEffect LightEffect { get; init; }

        /// <summary>
        /// Which game(s) this special appears in.
        /// </summary>
        public GameType Game { get; init; }
    }
}
