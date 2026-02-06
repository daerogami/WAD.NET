using System;

namespace WAD.NET.Definitions.GameData
{
    /// <summary>
    /// Identifies which game(s) a definition belongs to.
    /// </summary>
    [Flags]
    public enum GameType
    {
        /// <summary>No game specified.</summary>
        None = 0,

        /// <summary>DOOM (Episode-based maps).</summary>
        Doom = 1 << 0,

        /// <summary>DOOM II (MAP01-MAP32).</summary>
        Doom2 = 1 << 1,

        /// <summary>Heretic.</summary>
        Heretic = 1 << 2,

        /// <summary>Hexen.</summary>
        Hexen = 1 << 3,

        /// <summary>Strife.</summary>
        Strife = 1 << 4,

        /// <summary>All DOOM engine games (DOOM + DOOM II).</summary>
        AllDoom = Doom | Doom2,

        /// <summary>All supported games.</summary>
        All = Doom | Doom2 | Heretic | Hexen | Strife
    }
}
