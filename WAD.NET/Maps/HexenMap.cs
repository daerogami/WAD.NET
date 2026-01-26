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
        public string Name { get; init; }

        /// <summary>
        /// The map format is always Hexen.
        /// </summary>
        public MapFormat Format => MapFormat.Hexen;

        /// <summary>
        /// Things (entities) in the map.
        /// </summary>
        public HexenThing[] Things { get; init; }

        /// <summary>
        /// Vertices in the map.
        /// </summary>
        public MapVertex[] Vertices { get; init; }

        /// <summary>
        /// Linedefs in the map.
        /// </summary>
        public HexenLinedef[] Linedefs { get; init; }

        /// <summary>
        /// Sidedefs in the map.
        /// </summary>
        public DoomSidedef[] Sidedefs { get; init; }

        /// <summary>
        /// Sectors in the map.
        /// </summary>
        public DoomSector[] Sectors { get; init; }

        /// <summary>
        /// BSP segs.
        /// </summary>
        public Seg[] Segs { get; init; }

        /// <summary>
        /// BSP subsectors.
        /// </summary>
        public Subsector[] Subsectors { get; init; }

        /// <summary>
        /// BSP nodes.
        /// </summary>
        public Node[] Nodes { get; init; }

        /// <summary>
        /// Reject table data.
        /// </summary>
        public byte[] RejectTable { get; init; }

        /// <summary>
        /// Blockmap data.
        /// </summary>
        public byte[] BlockmapData { get; init; }

        /// <summary>
        /// ACS bytecode from BEHAVIOR lump.
        /// </summary>
        public byte[] BehaviorData { get; init; }

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
