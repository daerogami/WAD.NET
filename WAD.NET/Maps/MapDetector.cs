using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using WAD.NET.Definitions.GameData;
using WAD.NET.Enums;

namespace WAD.NET.Maps
{
    /// <summary>
    /// Utility class for detecting map markers and formats.
    /// </summary>
    public static class MapDetector
    {
        /// <summary>
        /// Pattern for DOOM episode maps (E1M1 through E4M9).
        /// </summary>
        private static readonly Regex DoomMapPattern = new Regex(@"^E[1-9]M[1-9]$", RegexOptions.Compiled);

        /// <summary>
        /// Pattern for DOOM II/Hexen maps (MAP01 through MAP99).
        /// </summary>
        private static readonly Regex Doom2MapPattern = new Regex(@"^MAP\d{2}$", RegexOptions.Compiled);

        /// <summary>
        /// Standard map lump names in order.
        /// </summary>
        public static readonly string[] StandardMapLumps = new[]
        {
            "THINGS", "LINEDEFS", "SIDEDEFS", "VERTEXES", "SEGS",
            "SSECTORS", "NODES", "SECTORS", "REJECT", "BLOCKMAP"
        };

        /// <summary>
        /// Determines if a lump name is a map marker (E1M1, MAP01, etc.).
        /// </summary>
        /// <param name="lumpName">The lump name to check.</param>
        /// <returns>True if the name matches a map marker pattern.</returns>
        public static bool IsMapMarker(string lumpName)
        {
            if (string.IsNullOrEmpty(lumpName))
                return false;

            return DoomMapPattern.IsMatch(lumpName) || Doom2MapPattern.IsMatch(lumpName);
        }

        /// <summary>
        /// Determines if a lump name is a standard map data lump.
        /// </summary>
        /// <param name="lumpName">The lump name to check.</param>
        /// <returns>True if the name is a recognized map lump.</returns>
        public static bool IsMapLump(string lumpName)
        {
            if (string.IsNullOrEmpty(lumpName))
                return false;

            return lumpName switch
            {
                "THINGS" or "LINEDEFS" or "SIDEDEFS" or "VERTEXES" or
                "SEGS" or "SSECTORS" or "NODES" or "SECTORS" or
                "REJECT" or "BLOCKMAP" or "BEHAVIOR" or "SCRIPTS" or
                "TEXTMAP" or "ENDMAP" or "ZNODES" or "DIALOGUE" => true,
                _ => false
            };
        }

        /// <summary>
        /// Detects the map format based on the lumps present.
        /// </summary>
        /// <param name="lumpNames">Collection of lump names following the map marker.</param>
        /// <returns>The detected map format.</returns>
        public static MapFormat DetectFormat(IEnumerable<string> lumpNames)
        {
            var names = lumpNames?.ToHashSet() ?? new HashSet<string>();

            // UDMF: Contains TEXTMAP lump
            if (names.Contains("TEXTMAP"))
                return MapFormat.UDMF;

            // Hexen: Contains BEHAVIOR lump (ACS bytecode)
            if (names.Contains("BEHAVIOR"))
                return MapFormat.Hexen;

            // Doom: Contains standard binary map lumps
            if (names.Contains("THINGS") && names.Contains("LINEDEFS"))
                return MapFormat.Doom;

            return MapFormat.Unknown;
        }

        /// <summary>
        /// Gets the episode number from a DOOM format map name.
        /// </summary>
        /// <param name="mapName">The map name (e.g., E1M1).</param>
        /// <returns>The episode number, or -1 if not a DOOM format map.</returns>
        public static int GetEpisode(string mapName)
        {
            var parsed = MapNamePatterns.Parse(mapName);
            return parsed?.Episode ?? -1;
        }

        /// <summary>
        /// Gets the map number from a map name.
        /// </summary>
        /// <param name="mapName">The map name (e.g., E1M1 or MAP01).</param>
        /// <returns>The map number, or -1 if not recognized.</returns>
        public static int GetMapNumber(string mapName)
        {
            var parsed = MapNamePatterns.Parse(mapName);
            return parsed?.Map ?? -1;
        }
    }
}
