using System;

namespace WAD.NET.Definitions.Hexen
{
    /// <summary>
    /// Flags for thing spawn options in Hexen format maps.
    /// </summary>
    [Flags]
    public enum HexenThingFlags : ushort
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
        /// Thing is dormant (inactive until triggered).
        /// </summary>
        Dormant = 0x0010,

        /// <summary>
        /// Appears for Fighter class.
        /// </summary>
        Class1 = 0x0020,

        /// <summary>
        /// Appears for Cleric class.
        /// </summary>
        Class2 = 0x0040,

        /// <summary>
        /// Appears for Mage class.
        /// </summary>
        Class3 = 0x0080,

        /// <summary>
        /// Appears in single player mode.
        /// </summary>
        Single = 0x0100,

        /// <summary>
        /// Appears in cooperative mode.
        /// </summary>
        Coop = 0x0200,

        /// <summary>
        /// Appears in deathmatch mode.
        /// </summary>
        Deathmatch = 0x0400
    }
}
