using WAD.NET.Definitions.Classic.Map;
using WAD.NET.Definitions.Hexen;
using WAD.NET.Enums;

namespace WAD.NET.Maps
{
    /// <summary>
    /// Represents a complete Hexen format map.
    /// </summary>
    public class HexenMap : IMap
    {
        /// <summary>
        /// The map name (e.g., MAP01).
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// The map format is always Hexen.
        /// </summary>
        public MapFormat Format => MapFormat.Hexen;

        /// <summary>
        /// Things (entities) in the map.
        /// </summary>
        public HexenThing[] Things { get; init; } = System.Array.Empty<HexenThing>();

        /// <summary>
        /// Vertices in the map.
        /// </summary>
        public MapVertex[] Vertices { get; init; } = System.Array.Empty<MapVertex>();

        /// <summary>
        /// Linedefs in the map.
        /// </summary>
        public HexenLinedef[] Linedefs { get; init; } = System.Array.Empty<HexenLinedef>();

        /// <summary>
        /// Sidedefs in the map.
        /// </summary>
        public DoomSidedef[] Sidedefs { get; init; } = System.Array.Empty<DoomSidedef>();

        /// <summary>
        /// Sectors in the map.
        /// </summary>
        public DoomSector[] Sectors { get; init; } = System.Array.Empty<DoomSector>();

        /// <summary>
        /// BSP segs.
        /// </summary>
        public Seg[] Segs { get; init; } = System.Array.Empty<Seg>();

        /// <summary>
        /// BSP subsectors.
        /// </summary>
        public Subsector[] Subsectors { get; init; } = System.Array.Empty<Subsector>();

        /// <summary>
        /// BSP nodes.
        /// </summary>
        public Node[] Nodes { get; init; } = System.Array.Empty<Node>();

        /// <summary>
        /// Reject table data.
        /// </summary>
        public byte[] RejectTable { get; init; } = System.Array.Empty<byte>();

        /// <summary>
        /// Blockmap data.
        /// </summary>
        public byte[] BlockmapData { get; init; } = System.Array.Empty<byte>();

        /// <summary>
        /// ACS bytecode from BEHAVIOR lump.
        /// </summary>
        public byte[] BehaviorData { get; init; } = System.Array.Empty<byte>();

        /// <summary>
        /// Number of things.
        /// </summary>
        public int ThingCount => Things?.Length ?? 0;

        /// <summary>
        /// Number of vertices.
        /// </summary>
        public int VertexCount => Vertices?.Length ?? 0;

        /// <summary>
        /// Number of linedefs.
        /// </summary>
        public int LinedefCount => Linedefs?.Length ?? 0;

        /// <summary>
        /// Number of sidedefs.
        /// </summary>
        public int SidedefCount => Sidedefs?.Length ?? 0;

        /// <summary>
        /// Number of sectors.
        /// </summary>
        public int SectorCount => Sectors?.Length ?? 0;

        /// <summary>
        /// Number of segs.
        /// </summary>
        public int SegCount => Segs?.Length ?? 0;

        /// <summary>
        /// Number of subsectors.
        /// </summary>
        public int SubsectorCount => Subsectors?.Length ?? 0;

        /// <summary>
        /// Number of nodes.
        /// </summary>
        public int NodeCount => Nodes?.Length ?? 0;

        /// <summary>
        /// Gets a thing by index.
        /// </summary>
        public HexenThing GetThing(int index) => Things[index];

        /// <summary>
        /// Gets a vertex by index.
        /// </summary>
        public MapVertex GetVertex(int index) => Vertices[index];

        /// <summary>
        /// Gets a linedef by index.
        /// </summary>
        public HexenLinedef GetLinedef(int index) => Linedefs[index];

        /// <summary>
        /// Gets a sidedef by index.
        /// </summary>
        public DoomSidedef GetSidedef(int index) => Sidedefs[index];

        /// <summary>
        /// Gets a sector by index.
        /// </summary>
        public DoomSector GetSector(int index) => Sectors[index];

        /// <summary>
        /// Gets the start and end vertices of a linedef.
        /// </summary>
        public (MapVertex start, MapVertex end) GetLinedefVertices(int linedefIndex)
        {
            var linedef = Linedefs[linedefIndex];
            return (Vertices[linedef.StartVertex], Vertices[linedef.EndVertex]);
        }

        /// <summary>
        /// Gets the sector that a sidedef belongs to.
        /// </summary>
        public DoomSector GetSidedefSector(int sidedefIndex)
        {
            var sidedef = Sidedefs[sidedefIndex];
            return Sectors[sidedef.Sector];
        }
    }
}
