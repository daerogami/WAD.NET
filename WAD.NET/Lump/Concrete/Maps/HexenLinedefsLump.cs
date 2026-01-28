using System;
using WAD.NET.Abstract;
using WAD.NET.Definitions.Hexen;
using WAD.NET.Interfaces;

namespace WAD.NET.Concrete.Maps
{
    /// <summary>
    /// Lump containing linedef data for a Hexen format map.
    /// </summary>
    public sealed class HexenLinedefsLump : Lump, IMapLump
    {
        /// <summary>
        /// The linedefs in this lump.
        /// </summary>
        public HexenLinedef[] Linedefs { get; }

        /// <summary>
        /// Number of linedefs in this lump.
        /// </summary>
        public int Count => Linedefs.Length;

        /// <summary>
        /// Creates a new HexenLinedefsLump by parsing binary data.
        /// </summary>
        /// <param name="name">Lump name.</param>
        /// <param name="source">Source WAD name.</param>
        /// <param name="data">Binary lump data.</param>
        public HexenLinedefsLump(string name, string source, ReadOnlySpan<byte> data)
            : base(name, source)
        {
            int count = data.Length / HexenLinedef.EntrySize;
            Linedefs = new HexenLinedef[count];

            for (int i = 0; i < count; i++)
            {
                Linedefs[i] = HexenLinedef.Parse(data.Slice(i * HexenLinedef.EntrySize, HexenLinedef.EntrySize));
            }
        }

        /// <summary>
        /// Gets the linedef at the specified index.
        /// </summary>
        public HexenLinedef this[int index] => Linedefs[index];
    }
}
