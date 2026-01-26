using System;

namespace WAD.NET.Definitions.Classic.Map
{
    /// <summary>
    /// Flags for linedefs in DOOM format maps.
    /// </summary>
    [Flags]
    public enum LinedefFlags : ushort
    {
        /// <summary>
        /// No flags set.
        /// </summary>
        None = 0,

        /// <summary>
        /// Blocks players and monsters.
        /// </summary>
        Impassable = 0x0001,

        /// <summary>
        /// Blocks monsters only.
        /// </summary>
        BlockMonsters = 0x0002,

        /// <summary>
        /// Line has two sides (not a wall).
        /// </summary>
        TwoSided = 0x0004,

        /// <summary>
        /// Upper texture is unpegged (draws from top down).
        /// </summary>
        UpperUnpegged = 0x0008,

        /// <summary>
        /// Lower texture is unpegged (draws from bottom up).
        /// </summary>
        LowerUnpegged = 0x0010,

        /// <summary>
        /// Shown as one-sided on automap (secret door).
        /// </summary>
        Secret = 0x0020,

        /// <summary>
        /// Blocks sound propagation.
        /// </summary>
        BlockSound = 0x0040,

        /// <summary>
        /// Never shown on automap.
        /// </summary>
        NotOnMap = 0x0080,

        /// <summary>
        /// Already visible on automap at start.
        /// </summary>
        AlreadyOnMap = 0x0100,

        // Boom extended flags
        /// <summary>
        /// Can be activated by players (Boom passthru).
        /// </summary>
        PassThru = 0x0200,

        // Strife flags (also used by some Boom extensions)
        /// <summary>
        /// Translucent line (Strife/Boom).
        /// </summary>
        Translucent = 0x0400,

        /// <summary>
        /// Line can be jumped over (Strife).
        /// </summary>
        JumpOver = 0x0800,

        /// <summary>
        /// Block floating monsters (Strife).
        /// </summary>
        BlockFloaters = 0x1000
    }
}
