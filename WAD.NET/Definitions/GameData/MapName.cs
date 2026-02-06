namespace WAD.NET.Definitions.GameData
{
    /// <summary>
    /// Parsed representation of a map name with its components.
    /// </summary>
    public class MapName
    {
        /// <summary>
        /// The naming format of this map.
        /// </summary>
        public MapNameFormat Format { get; init; }

        /// <summary>
        /// The original map name string.
        /// </summary>
        public string RawName { get; init; } = "";

        /// <summary>
        /// Episode number for DOOM format maps (E#M#), null otherwise.
        /// </summary>
        public int? Episode { get; init; }

        /// <summary>
        /// Map number (M# for DOOM, ## for DOOM II), null for custom names.
        /// </summary>
        public int? Map { get; init; }

        /// <summary>
        /// Gets a sortable index for ordering maps.
        /// </summary>
        public int SortOrder => Format switch
        {
            MapNameFormat.Doom => (Episode ?? 0) * 10 + (Map ?? 0),
            MapNameFormat.Doom2 => Map ?? 0,
            _ => 0
        };
    }
}
