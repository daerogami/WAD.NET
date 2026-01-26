using WAD.NET.Enums;

namespace WAD.NET.Maps
{
    /// <summary>
    /// Interface for all map types.
    /// </summary>
    public interface IMap
    {
        /// <summary>
        /// The map name (e.g., E1M1, MAP01).
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The map format.
        /// </summary>
        MapFormat Format { get; }

        /// <summary>
        /// Number of things in the map.
        /// </summary>
        int ThingCount { get; }

        /// <summary>
        /// Number of vertices in the map.
        /// </summary>
        int VertexCount { get; }

        /// <summary>
        /// Number of linedefs in the map.
        /// </summary>
        int LinedefCount { get; }

        /// <summary>
        /// Number of sidedefs in the map.
        /// </summary>
        int SidedefCount { get; }

        /// <summary>
        /// Number of sectors in the map.
        /// </summary>
        int SectorCount { get; }
    }
}
