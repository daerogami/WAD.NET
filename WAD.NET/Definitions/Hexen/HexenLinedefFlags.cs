using System;

namespace WAD.NET.Definitions.Hexen
{
    /// <summary>
    /// Flags for linedefs in Hexen format maps.
    /// </summary>
    [Flags]
    public enum HexenLinedefFlags : ushort
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

        /// <summary>
        /// Line can be activated more than once (repeatable).
        /// </summary>
        Repeatable = 0x0200,

        // Activation types (bits 10-12)
        /// <summary>
        /// Activated when player crosses.
        /// </summary>
        ActivatePlayerCross = 0x0000,

        /// <summary>
        /// Activated when player uses.
        /// </summary>
        ActivatePlayerUse = 0x0400,

        /// <summary>
        /// Activated when monster crosses.
        /// </summary>
        ActivateMonsterCross = 0x0800,

        /// <summary>
        /// Activated when projectile hits.
        /// </summary>
        ActivateProjectileHit = 0x0C00,

        /// <summary>
        /// Activated when player bumps.
        /// </summary>
        ActivatePlayerBump = 0x1000,

        /// <summary>
        /// Activated when projectile crosses.
        /// </summary>
        ActivateProjectileCross = 0x1400,

        /// <summary>
        /// Activated when player uses (passthrough).
        /// </summary>
        ActivatePlayerUsePassthrough = 0x1800,

        /// <summary>
        /// Blocks all (players, monsters, and projectiles).
        /// </summary>
        BlockAll = 0x8000
    }

    /// <summary>
    /// Hexen linedef activation types.
    /// </summary>
    public enum HexenActivationType : ushort
    {
        /// <summary>
        /// Activated when player crosses.
        /// </summary>
        PlayerCross = 0x0000,

        /// <summary>
        /// Activated when player uses.
        /// </summary>
        PlayerUse = 0x0400,

        /// <summary>
        /// Activated when monster crosses.
        /// </summary>
        MonsterCross = 0x0800,

        /// <summary>
        /// Activated when projectile hits.
        /// </summary>
        ProjectileHit = 0x0C00,

        /// <summary>
        /// Activated when player bumps.
        /// </summary>
        PlayerBump = 0x1000,

        /// <summary>
        /// Activated when projectile crosses.
        /// </summary>
        ProjectileCross = 0x1400,

        /// <summary>
        /// Activated when player uses (passthrough).
        /// </summary>
        PlayerUsePassthrough = 0x1800
    }
}
