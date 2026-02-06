namespace WAD.NET.Definitions.GameData
{
    /// <summary>
    /// Categories for thing types in the game data database.
    /// </summary>
    public enum ThingCategory
    {
        /// <summary>Player start positions.</summary>
        Player,

        /// <summary>Monsters and enemies.</summary>
        Monster,

        /// <summary>Weapons.</summary>
        Weapon,

        /// <summary>Ammunition pickups.</summary>
        Ammo,

        /// <summary>Health pickups.</summary>
        Health,

        /// <summary>Armor pickups.</summary>
        Armor,

        /// <summary>Powerup items.</summary>
        Powerup,

        /// <summary>Key items.</summary>
        Key,

        /// <summary>Solid obstacles (barrels, pillars, etc.).</summary>
        Obstacle,

        /// <summary>Non-blocking decorations.</summary>
        Decoration,

        /// <summary>Gore decorations (gibs, corpses, etc.).</summary>
        Gore,

        /// <summary>Technical items (cameras, interpolation points, etc.).</summary>
        TechItem,

        /// <summary>Light sources.</summary>
        Light,

        /// <summary>Teleport destinations and effects.</summary>
        Teleport,

        /// <summary>Ambient sound sources.</summary>
        Sound,

        /// <summary>Boss monsters.</summary>
        Boss,

        /// <summary>Uncategorized things.</summary>
        Other
    }
}
