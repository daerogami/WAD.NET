using WAD.NET.Enums;
using WAD.NET.UDMF;

namespace WAD.NET.Maps
{
    /// <summary>
    /// Wrapper that presents a UDMF map through the IMap interface.
    /// </summary>
    public class UdmfMapWrapper : IMap
    {
        /// <summary>
        /// The map name (e.g., MAP01).
        /// </summary>
        public string Name { get; init; }

        /// <summary>
        /// The map format is always UDMF.
        /// </summary>
        public MapFormat Format => MapFormat.UDMF;

        /// <summary>
        /// The underlying UDMF map.
        /// </summary>
        public UdmfMap Map { get; init; }

        /// <summary>
        /// Number of things.
        /// </summary>
        public int ThingCount => Map?.ThingCount ?? 0;

        /// <summary>
        /// Number of vertices.
        /// </summary>
        public int VertexCount => Map?.VertexCount ?? 0;

        /// <summary>
        /// Number of linedefs.
        /// </summary>
        public int LinedefCount => Map?.LinedefCount ?? 0;

        /// <summary>
        /// Number of sidedefs.
        /// </summary>
        public int SidedefCount => Map?.SidedefCount ?? 0;

        /// <summary>
        /// Number of sectors.
        /// </summary>
        public int SectorCount => Map?.SectorCount ?? 0;

        /// <summary>
        /// The UDMF namespace (doom, heretic, hexen, strife, zdoom, etc.).
        /// </summary>
        public string Namespace => Map?.Namespace ?? "doom";

        /// <summary>
        /// Gets a thing by index.
        /// </summary>
        public UdmfThing GetThing(int index) => Map.Things[index];

        /// <summary>
        /// Gets a vertex by index.
        /// </summary>
        public UdmfVertex GetVertex(int index) => Map.Vertices[index];

        /// <summary>
        /// Gets a linedef by index.
        /// </summary>
        public UdmfLinedef GetLinedef(int index) => Map.Linedefs[index];

        /// <summary>
        /// Gets a sidedef by index.
        /// </summary>
        public UdmfSidedef GetSidedef(int index) => Map.Sidedefs[index];

        /// <summary>
        /// Gets a sector by index.
        /// </summary>
        public UdmfSector GetSector(int index) => Map.Sectors[index];

        /// <summary>
        /// Gets the start and end vertices of a linedef.
        /// </summary>
        public (UdmfVertex start, UdmfVertex end) GetLinedefVertices(int linedefIndex)
        {
            var linedef = Map.Linedefs[linedefIndex];
            return (Map.Vertices[linedef.V1], Map.Vertices[linedef.V2]);
        }

        /// <summary>
        /// Gets the sector that a sidedef belongs to.
        /// </summary>
        public UdmfSector GetSidedefSector(int sidedefIndex)
        {
            var sidedef = Map.Sidedefs[sidedefIndex];
            return Map.Sectors[sidedef.Sector];
        }
    }
}
