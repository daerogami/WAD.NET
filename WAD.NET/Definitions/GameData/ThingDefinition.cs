namespace WAD.NET.Definitions.GameData
{
    /// <summary>
    /// Defines a thing type (DoomEd number) with its properties and metadata.
    /// </summary>
    public class ThingDefinition
    {
        /// <summary>
        /// DoomEd number used to place this thing in map editors.
        /// </summary>
        public int DoomEdNum { get; init; }

        /// <summary>
        /// ZDoom/GZDoom class name (e.g., "ZombieMan", "Cyberdemon").
        /// </summary>
        public string ClassName { get; init; } = "";

        /// <summary>
        /// Legacy DECORATE name if different from ClassName.
        /// </summary>
        public string? LegacyName { get; init; }

        /// <summary>
        /// Human-readable display name (e.g., "Former Human", "Baron of Hell").
        /// </summary>
        public string DisplayName { get; init; } = "";

        /// <summary>
        /// Category classification for this thing.
        /// </summary>
        public ThingCategory Category { get; init; }

        /// <summary>
        /// Behavioral flags describing this thing's intrinsic properties.
        /// </summary>
        public ThingBehaviorFlags Flags { get; init; }

        /// <summary>
        /// Which game(s) this thing appears in.
        /// </summary>
        public GameType Game { get; init; }

        /// <summary>
        /// Default health value (null for non-shootable things).
        /// </summary>
        public int? Health { get; init; }

        /// <summary>
        /// Default movement speed in map units per tic.
        /// </summary>
        public int? Speed { get; init; }

        /// <summary>
        /// Collision radius in map units.
        /// </summary>
        public int? Radius { get; init; }

        /// <summary>
        /// Collision height in map units.
        /// </summary>
        public int? Height { get; init; }

        /// <summary>
        /// Default damage value for attacks.
        /// </summary>
        public int? Damage { get; init; }

        /// <summary>
        /// Mass value affecting thrust from damage.
        /// </summary>
        public int? Mass { get; init; }

        /// <summary>
        /// Reaction time in tics before first attack.
        /// </summary>
        public int? ReactionTime { get; init; }

        /// <summary>
        /// Probability of entering pain state (0-256).
        /// </summary>
        public int? PainChance { get; init; }

        /// <summary>
        /// 4-character sprite prefix for the spawn state.
        /// </summary>
        public string? SpawnSprite { get; init; }

        /// <summary>
        /// Sprite frame letter for the spawn state.
        /// </summary>
        public char SpawnFrame { get; init; } = 'A';

        /// <summary>
        /// Weapon slot number (1-9), for weapon things only.
        /// </summary>
        public int? Slot { get; init; }
    }
}
