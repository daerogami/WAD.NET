using System;
using WAD.NET.Abstract;
using WAD.NET.Definitions.Classic.Map;
using WAD.NET.Interfaces;

namespace WAD.NET.Concrete.Maps
{
    /// <summary>
    /// Lump containing sidedef data for a DOOM format map.
    /// </summary>
    public sealed class SidedefsLump : Lump, IMapLump
    {
        /// <summary>
        /// The sidedefs in this lump.
        /// </summary>
        public DoomSidedef[] Sidedefs { get; }

        /// <summary>
        /// Number of sidedefs in this lump.
        /// </summary>
        public int Count => Sidedefs.Length;

        /// <summary>
        /// Creates a new SidedefsLump by parsing binary data.
        /// </summary>
        /// <param name="name">Lump name.</param>
        /// <param name="source">Source WAD name.</param>
        /// <param name="data">Binary lump data.</param>
        public SidedefsLump(string name, string source, ReadOnlySpan<byte> data)
            : base(name, source)
        {
            int count = data.Length / DoomSidedef.EntrySize;
            Sidedefs = new DoomSidedef[count];

            for (int i = 0; i < count; i++)
            {
                Sidedefs[i] = DoomSidedef.Parse(data.Slice(i * DoomSidedef.EntrySize, DoomSidedef.EntrySize));
            }
        }

        /// <summary>
        /// Gets the sidedef at the specified index.
        /// </summary>
        public DoomSidedef this[int index] => Sidedefs[index];
    }
}
