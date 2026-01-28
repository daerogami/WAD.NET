using System;
using WAD.NET.Abstract;
using WAD.NET.Definitions.Classic.Map;
using WAD.NET.Interfaces;

namespace WAD.NET.Concrete.Maps
{
    /// <summary>
    /// Lump containing seg (BSP segment) data for a DOOM format map.
    /// </summary>
    public sealed class SegsLump : Lump, IMapLump
    {
        /// <summary>
        /// The segs in this lump.
        /// </summary>
        public Seg[] Segs { get; }

        /// <summary>
        /// Number of segs in this lump.
        /// </summary>
        public int Count => Segs.Length;

        /// <summary>
        /// Creates a new SegsLump by parsing binary data.
        /// </summary>
        /// <param name="name">Lump name.</param>
        /// <param name="source">Source WAD name.</param>
        /// <param name="data">Binary lump data.</param>
        public SegsLump(string name, string source, ReadOnlySpan<byte> data)
            : base(name, source)
        {
            int count = data.Length / Seg.EntrySize;
            Segs = new Seg[count];

            for (int i = 0; i < count; i++)
            {
                Segs[i] = Seg.Parse(data.Slice(i * Seg.EntrySize, Seg.EntrySize));
            }
        }

        /// <summary>
        /// Gets the seg at the specified index.
        /// </summary>
        public Seg this[int index] => Segs[index];
    }
}
