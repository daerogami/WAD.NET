using System;
using WAD.NET.Abstract;
using WAD.NET.Definitions.Classic.Map;
using WAD.NET.Interfaces;

namespace WAD.NET.Concrete.Maps
{
    /// <summary>
    /// Lump containing sector data for a DOOM format map.
    /// </summary>
    public sealed class SectorsLump : Lump, IMapLump
    {
        /// <summary>
        /// The sectors in this lump.
        /// </summary>
        public DoomSector[] Sectors { get; }

        /// <summary>
        /// Number of sectors in this lump.
        /// </summary>
        public int Count => Sectors.Length;

        /// <summary>
        /// Creates a new SectorsLump by parsing binary data.
        /// </summary>
        /// <param name="name">Lump name.</param>
        /// <param name="source">Source WAD name.</param>
        /// <param name="data">Binary lump data.</param>
        public SectorsLump(string name, string source, ReadOnlySpan<byte> data)
            : base(name, source)
        {
            int count = data.Length / DoomSector.EntrySize;
            Sectors = new DoomSector[count];

            for (int i = 0; i < count; i++)
            {
                Sectors[i] = DoomSector.Parse(data.Slice(i * DoomSector.EntrySize, DoomSector.EntrySize));
            }
        }

        /// <summary>
        /// Gets the sector at the specified index.
        /// </summary>
        public DoomSector this[int index] => Sectors[index];
    }
}
