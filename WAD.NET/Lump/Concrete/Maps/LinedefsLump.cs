using System;
using WAD.NET.Abstract;
using WAD.NET.Definitions.Classic.Map;
using WAD.NET.Interfaces;

namespace WAD.NET.Concrete.Maps
{
    /// <summary>
    /// Lump containing linedef data for a DOOM format map.
    /// </summary>
    public sealed class LinedefsLump : Lump, IMapLump
    {
        /// <summary>
        /// The linedefs in this lump.
        /// </summary>
        public DoomLinedef[] Linedefs { get; }

        /// <summary>
        /// Number of linedefs in this lump.
        /// </summary>
        public int Count => Linedefs.Length;

        /// <summary>
        /// Creates a new LinedefsLump by parsing binary data.
        /// </summary>
        /// <param name="name">Lump name.</param>
        /// <param name="source">Source WAD name.</param>
        /// <param name="data">Binary lump data.</param>
        public LinedefsLump(string name, string source, ReadOnlySpan<byte> data)
            : base(name, source)
        {
            int count = data.Length / DoomLinedef.EntrySize;
            Linedefs = new DoomLinedef[count];

            for (int i = 0; i < count; i++)
            {
                Linedefs[i] = DoomLinedef.Parse(data.Slice(i * DoomLinedef.EntrySize, DoomLinedef.EntrySize));
            }
        }

        /// <summary>
        /// Gets the linedef at the specified index.
        /// </summary>
        public DoomLinedef this[int index] => Linedefs[index];
    }
}
