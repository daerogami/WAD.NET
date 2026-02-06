using System;

namespace WAD.NET.Definitions.GameData
{
    /// <summary>
    /// Behavioral metadata flags for thing definitions in the game data database.
    /// These describe a thing's intrinsic behavior, not binary spawn flags.
    /// </summary>
    /// <remarks>
    /// Not to be confused with <see cref="Classic.Map.ThingFlags"/> which represents
    /// the binary spawn flags stored in map data.
    /// </remarks>
    [Flags]
    public enum ThingBehaviorFlags : uint
    {
        /// <summary>No flags set.</summary>
        None = 0,

        /// <summary>Blocks other things from occupying the same space.</summary>
        Solid = 1 << 0,

        /// <summary>Can be damaged and destroyed.</summary>
        Shootable = 1 << 1,

        /// <summary>Not linked into the sector list.</summary>
        NoSector = 1 << 2,

        /// <summary>Not linked into the blockmap.</summary>
        NoBlockmap = 1 << 3,

        /// <summary>Spawns on the ceiling instead of the floor.</summary>
        SpawnCeiling = 1 << 4,

        /// <summary>Not affected by gravity.</summary>
        NoGravity = 1 << 5,

        /// <summary>Can be picked up by the player.</summary>
        Pickup = 1 << 6,

        /// <summary>Uses reduced collision detection.</summary>
        Clip = 1 << 7,

        /// <summary>This thing is a monster.</summary>
        IsMonster = 1 << 8,

        /// <summary>Counts toward the kill percentage.</summary>
        Countkill = 1 << 9,

        /// <summary>Counts toward the item percentage.</summary>
        CountItem = 1 << 10,

        /// <summary>Can fly/float in the air.</summary>
        Float = 1 << 11,

        /// <summary>Friendly monster (MBF/Boom extension).</summary>
        Friend = 1 << 12,

        /// <summary>Rendered with translucency (Boom extension).</summary>
        Translucent = 1 << 13
    }
}
