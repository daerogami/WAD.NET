namespace WAD.NET.LaunchConfig
{
    /// <summary>
    /// Represents the game mode for a DOOM engine launch configuration.
    /// </summary>
    public enum GameMode
    {
        /// <summary>Single-player game.</summary>
        SinglePlayer,

        /// <summary>Cooperative multiplayer.</summary>
        Cooperative,

        /// <summary>Free-for-all deathmatch.</summary>
        Deathmatch,

        /// <summary>Team-based deathmatch.</summary>
        TeamDeathmatch,

        /// <summary>Capture the flag.</summary>
        CaptureTheFlag,

        /// <summary>Last man standing.</summary>
        LastManStanding,

        /// <summary>Survival mode.</summary>
        Survival
    }
}
