using System;

namespace WAD.NET.Definitions.Classic.Map
{
    /// <summary>
    /// Flags for thing spawn options in DOOM format maps.
    /// </summary>
    [Flags]
    public enum ThingFlags : ushort
    {
        /// <summary>
        /// No flags set.
        /// </summary>
        None = 0,

        /// <summary>
        /// Appears on skill levels 1 and 2 (easy).
        /// </summary>
        SkillEasy = 0x0001,

        /// <summary>
        /// Appears on skill level 3 (medium).
        /// </summary>
        SkillMedium = 0x0002,

        /// <summary>
        /// Appears on skill levels 4 and 5 (hard).
        /// </summary>
        SkillHard = 0x0004,

        /// <summary>
        /// Thing is deaf (won't react to sound, ambush flag).
        /// </summary>
        Ambush = 0x0008,

        /// <summary>
        /// Only appears in multiplayer mode.
        /// </summary>
        Multiplayer = 0x0010,

        // Boom extended flags
        /// <summary>
        /// Not in deathmatch (Boom).
        /// </summary>
        NotInDeathmatch = 0x0020,

        /// <summary>
        /// Not in coop (Boom).
        /// </summary>
        NotInCoop = 0x0040,

        /// <summary>
        /// Friendly monster (MBF).
        /// </summary>
        Friendly = 0x0080
    }
}
