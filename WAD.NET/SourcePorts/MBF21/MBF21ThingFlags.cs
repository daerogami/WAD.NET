using System;

namespace WAD.NET.SourcePorts.MBF21
{
    /// <summary>
    /// Extended thing flags for MBF21-compatible source ports.
    /// These flags are typically set via DEHACKED patches with "MBF21 Bits" field.
    /// </summary>
    [Flags]
    public enum MBF21ThingFlags : uint
    {
        /// <summary>
        /// No flags set.
        /// </summary>
        None = 0,

        // Standard thing flags would be in lower bits
        // MBF21 extensions are in upper bits

        /// <summary>
        /// Kill is logged/counted (LOGKILL).
        /// </summary>
        LogKill = 0x00010000,

        /// <summary>
        /// Sounds are played at full volume regardless of distance (FULLVOLSOUNDS).
        /// </summary>
        FullVolSounds = 0x00020000,

        /// <summary>
        /// Considered a monster for A_Look purposes (ISMONSTER).
        /// </summary>
        IsMonster = 0x00040000,

        /// <summary>
        /// Counts toward kill percentage (COUNTKILL).
        /// </summary>
        CountKill = 0x00080000,

        /// <summary>
        /// Triggers tag 666 action on death (MAP07 Mancubus behavior).
        /// </summary>
        Map07Boss1 = 0x00100000,

        /// <summary>
        /// Triggers tag 667 action on death (MAP07 Arachnotron behavior).
        /// </summary>
        Map07Boss2 = 0x00200000,

        /// <summary>
        /// E1M8 Baron death special.
        /// </summary>
        E1M8Boss = 0x00400000,

        /// <summary>
        /// E2M8 Cyberdemon death special.
        /// </summary>
        E2M8Boss = 0x00800000,

        /// <summary>
        /// E3M8 Spider Mastermind death special.
        /// </summary>
        E3M8Boss = 0x01000000,

        /// <summary>
        /// E4M6 Cyberdemon death special.
        /// </summary>
        E4M6Boss = 0x02000000,

        /// <summary>
        /// E4M8 Spider death special.
        /// </summary>
        E4M8Boss = 0x04000000,

        /// <summary>
        /// Makes ripper sound when projectile passes through enemies.
        /// </summary>
        RipSound = 0x08000000,

        /// <summary>
        /// Icon of Sin cannot spawn this monster.
        /// </summary>
        NoBossSpawn = 0x10000000
    }
}
