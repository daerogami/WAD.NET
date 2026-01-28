using System.Collections.Generic;

namespace WAD.NET.UDMF
{
    /// <summary>
    /// Represents a complete UDMF map.
    /// </summary>
    public class UdmfMap
    {
        /// <summary>
        /// The UDMF namespace (doom, heretic, hexen, strife, zdoom, etc.).
        /// </summary>
        public string Namespace { get; set; } = "doom";

        /// <summary>
        /// The vertices in this map.
        /// </summary>
        public List<UdmfVertex> Vertices { get; } = new List<UdmfVertex>();

        /// <summary>
        /// The linedefs in this map.
        /// </summary>
        public List<UdmfLinedef> Linedefs { get; } = new List<UdmfLinedef>();

        /// <summary>
        /// The sidedefs in this map.
        /// </summary>
        public List<UdmfSidedef> Sidedefs { get; } = new List<UdmfSidedef>();

        /// <summary>
        /// The sectors in this map.
        /// </summary>
        public List<UdmfSector> Sectors { get; } = new List<UdmfSector>();

        /// <summary>
        /// The things in this map.
        /// </summary>
        public List<UdmfThing> Things { get; } = new List<UdmfThing>();

        /// <summary>
        /// Number of vertices.
        /// </summary>
        public int VertexCount => Vertices.Count;

        /// <summary>
        /// Number of linedefs.
        /// </summary>
        public int LinedefCount => Linedefs.Count;

        /// <summary>
        /// Number of sidedefs.
        /// </summary>
        public int SidedefCount => Sidedefs.Count;

        /// <summary>
        /// Number of sectors.
        /// </summary>
        public int SectorCount => Sectors.Count;

        /// <summary>
        /// Number of things.
        /// </summary>
        public int ThingCount => Things.Count;
    }
}
