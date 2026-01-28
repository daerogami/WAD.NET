using System.Collections.Generic;

namespace WAD.NET.LaunchConfig
{
    /// <summary>
    /// Engine-agnostic launch configuration for a DOOM engine game session.
    /// Captures the IWAD, load order, map, skill, and multiplayer settings
    /// in a format that can be serialized to any engine's native configuration.
    /// </summary>
    public class WadLaunchConfiguration
    {
        /// <summary>
        /// Path or identifier for the base IWAD. Required.
        /// </summary>
        public string Iwad { get; set; } = null!;

        /// <summary>
        /// Ordered list of PWADs/mods to load after the IWAD.
        /// </summary>
        public List<string> Files { get; set; } = new List<string>();

        /// <summary>
        /// Optional starting map (e.g., "MAP01", "E1M1").
        /// </summary>
        public string? Map { get; set; }

        /// <summary>
        /// Optional skill level (1-5, where 1 = easiest).
        /// </summary>
        public int? Skill { get; set; }

        /// <summary>
        /// Optional game mode for multiplayer.
        /// </summary>
        public GameMode? Mode { get; set; }

        /// <summary>
        /// Optional number of players (for multiplayer configs).
        /// </summary>
        public int? Players { get; set; }

        /// <summary>
        /// Optional network port.
        /// </summary>
        public int? Port { get; set; }

        /// <summary>
        /// Engine-specific extra arguments that don't fit the common model.
        /// Provides an escape hatch for settings unique to a particular source port.
        /// </summary>
        public Dictionary<string, string> ExtraParameters { get; set; } = new Dictionary<string, string>();
    }
}
