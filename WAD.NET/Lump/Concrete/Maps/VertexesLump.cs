using System;
using WAD.NET.Abstract;
using WAD.NET.Definitions.Classic.Map;
using WAD.NET.Interfaces;

namespace WAD.NET.Concrete.Maps
{
    /// <summary>
    /// Lump containing vertex data for a DOOM format map.
    /// </summary>
    public sealed class VertexesLump : Lump, IMapLump
    {
        /// <summary>
        /// The vertices in this lump.
        /// </summary>
        public MapVertex[] Vertices { get; }

        /// <summary>
        /// Number of vertices in this lump.
        /// </summary>
        public int Count => Vertices.Length;

        /// <summary>
        /// Creates a new VertexesLump by parsing binary data.
        /// </summary>
        /// <param name="name">Lump name.</param>
        /// <param name="source">Source WAD name.</param>
        /// <param name="data">Binary lump data.</param>
        public VertexesLump(string name, string source, ReadOnlySpan<byte> data)
            : base(name, source)
        {
            int count = data.Length / MapVertex.EntrySize;
            Vertices = new MapVertex[count];

            for (int i = 0; i < count; i++)
            {
                Vertices[i] = MapVertex.Parse(data.Slice(i * MapVertex.EntrySize, MapVertex.EntrySize));
            }
        }

        /// <summary>
        /// Gets the vertex at the specified index.
        /// </summary>
        public MapVertex this[int index] => Vertices[index];
    }
}
