using System.Text.RegularExpressions;

namespace WAD.NET.Definitions.GameData
{
    /// <summary>
    /// Regex patterns and parsing for map name formats.
    /// </summary>
    public static class MapNamePatterns
    {
        /// <summary>DOOM format: E#M# (Episode 1-9, Map 1-9).</summary>
        public static readonly Regex DoomPattern = new Regex(@"^E([1-9])M([1-9])$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>DOOM II format: MAP## (01-99).</summary>
        public static readonly Regex Doom2Pattern = new Regex(@"^MAP(\d{2})$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Parses a map name into its components.
        /// </summary>
        /// <param name="mapName">The map name to parse (e.g., "E1M1", "MAP23").</param>
        /// <returns>A parsed MapName, or null if the name is null/empty.</returns>
        public static MapName? Parse(string mapName)
        {
            if (string.IsNullOrEmpty(mapName))
                return null;

            var doomMatch = DoomPattern.Match(mapName);
            if (doomMatch.Success)
            {
                return new MapName
                {
                    Format = MapNameFormat.Doom,
                    RawName = mapName,
                    Episode = int.Parse(doomMatch.Groups[1].Value),
                    Map = int.Parse(doomMatch.Groups[2].Value)
                };
            }

            var doom2Match = Doom2Pattern.Match(mapName);
            if (doom2Match.Success)
            {
                return new MapName
                {
                    Format = MapNameFormat.Doom2,
                    RawName = mapName,
                    Map = int.Parse(doom2Match.Groups[1].Value)
                };
            }

            // Non-standard name
            return new MapName
            {
                Format = MapNameFormat.Custom,
                RawName = mapName
            };
        }
    }
}
