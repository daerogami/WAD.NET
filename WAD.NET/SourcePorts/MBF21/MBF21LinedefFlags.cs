using System;

namespace WAD.NET.SourcePorts.MBF21
{
    /// <summary>
    /// Extended linedef flags for MBF21-compatible source ports.
    /// These are 32-bit flags that extend beyond the standard 16-bit DOOM flags.
    /// </summary>
    [Flags]
    public enum MBF21LinedefFlags : uint
    {
        /// <summary>
        /// No flags set.
        /// </summary>
        None = 0,

        // Standard DOOM flags (bits 0-8)
        /// <summary>
        /// Blocks players and monsters.
        /// </summary>
        Impassable = 0x00000001,

        /// <summary>
        /// Blocks monsters only.
        /// </summary>
        BlockMonsters = 0x00000002,

        /// <summary>
        /// Line has two sides (not a wall).
        /// </summary>
        TwoSided = 0x00000004,

        /// <summary>
        /// Upper texture is unpegged.
        /// </summary>
        UpperUnpegged = 0x00000008,

        /// <summary>
        /// Lower texture is unpegged.
        /// </summary>
        LowerUnpegged = 0x00000010,

        /// <summary>
        /// Shown as one-sided on automap (secret door).
        /// </summary>
        Secret = 0x00000020,

        /// <summary>
        /// Blocks sound propagation.
        /// </summary>
        BlockSound = 0x00000040,

        /// <summary>
        /// Never shown on automap.
        /// </summary>
        NotOnMap = 0x00000080,

        /// <summary>
        /// Already visible on automap.
        /// </summary>
        AlreadyOnMap = 0x00000100,

        // Boom flags (bits 9-15)
        /// <summary>
        /// Pass activation through (Boom ML_PASSUSE).
        /// </summary>
        PassThru = 0x00000200,

        // MBF21 extended flags (bits 16+)
        /// <summary>
        /// Block players only (not monsters).
        /// </summary>
        BlockPlayers = 0x00010000,

        /// <summary>
        /// Block floating monsters.
        /// </summary>
        BlockFloaters = 0x00020000,

        /// <summary>
        /// Block walking (land-based) monsters.
        /// </summary>
        BlockLandMonsters = 0x00040000
    }
}
