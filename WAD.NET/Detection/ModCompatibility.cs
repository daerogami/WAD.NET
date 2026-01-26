using System.Collections.Generic;

namespace WAD.NET.Detection
{
    /// <summary>
    /// Represents the compatibility requirements of a mod/WAD.
    /// </summary>
    public class ModCompatibility
    {
        /// <summary>
        /// Minimum source port required to run this mod.
        /// </summary>
        public SourcePort MinimumPort { get; set; } = SourcePort.Vanilla;

        /// <summary>
        /// Features required by this mod.
        /// </summary>
        public List<string> RequiredFeatures { get; } = new List<string>();

        /// <summary>
        /// Warnings about potential compatibility issues.
        /// </summary>
        public List<string> Warnings { get; } = new List<string>();

        /// <summary>
        /// Map formats detected in the archive.
        /// </summary>
        public HashSet<string> MapFormats { get; } = new HashSet<string>();

        /// <summary>
        /// Whether the mod uses DEHACKED.
        /// </summary>
        public bool UsesDehacked { get; set; }

        /// <summary>
        /// Whether the mod uses DECORATE.
        /// </summary>
        public bool UsesDecorate { get; set; }

        /// <summary>
        /// Whether the mod uses ZScript.
        /// </summary>
        public bool UsesZScript { get; set; }

        /// <summary>
        /// Whether the mod uses MAPINFO.
        /// </summary>
        public bool UsesMapInfo { get; set; }

        /// <summary>
        /// Whether the mod uses UMAPINFO.
        /// </summary>
        public bool UsesUMapInfo { get; set; }

        /// <summary>
        /// Whether the mod uses SNDINFO.
        /// </summary>
        public bool UsesSndInfo { get; set; }

        /// <summary>
        /// Whether the mod uses ACS scripts.
        /// </summary>
        public bool UsesACS { get; set; }

        /// <summary>
        /// Number of actors defined in DECORATE/ZScript.
        /// </summary>
        public int ActorCount { get; set; }

        /// <summary>
        /// Whether the mod is likely compatible with Boom-family ports.
        /// </summary>
        public bool IsBoomCompatible => MinimumPort <= SourcePort.MBF21;

        /// <summary>
        /// Whether the mod requires ZDoom-family ports.
        /// </summary>
        public bool RequiresZDoomFamily => MinimumPort >= SourcePort.ZDoom;

        /// <summary>
        /// A human-readable description of the compatibility requirements.
        /// </summary>
        public string Description
        {
            get
            {
                switch (MinimumPort)
                {
                    case SourcePort.Vanilla:
                        return "Compatible with vanilla DOOM and all source ports";
                    case SourcePort.Boom:
                        return "Requires Boom-compatible source port";
                    case SourcePort.MBF:
                        return "Requires MBF-compatible source port";
                    case SourcePort.MBF21:
                        return "Requires MBF21-compliant source port";
                    case SourcePort.ZDoom:
                        return "Requires ZDoom or compatible port";
                    case SourcePort.GZDoom:
                        return "Requires GZDoom";
                    case SourcePort.Eternity:
                        return "Requires Eternity Engine";
                    default:
                        return "Unknown compatibility requirements";
                }
            }
        }
    }
}
