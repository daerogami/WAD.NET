using System;

namespace WAD.NET.SourcePorts.Boom
{
    /// <summary>
    /// Extended linedef flags for Boom-compatible source ports.
    /// </summary>
    [Flags]
    public enum BoomLinedefFlags : ushort
    {
        /// <summary>
        /// No flags set.
        /// </summary>
        None = 0,

        // Standard DOOM flags (0x0001 - 0x0100)
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

        // Boom extensions (0x0200+)
        /// <summary>
        /// Pass activation through to other linedefs (ML_PASSUSE).
        /// </summary>
        PassThru = 0x0200,

        /// <summary>
        /// Reserved for future use.
        /// </summary>
        Reserved1 = 0x0400,

        /// <summary>
        /// Reserved for future use.
        /// </summary>
        Reserved2 = 0x0800,

        // MBF extensions
        /// <summary>
        /// Translucent middle texture (MBF).
        /// </summary>
        TranslucentMidtex = 0x1000
    }
}
