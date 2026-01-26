using System;
using WAD.NET.Abstract;
using WAD.NET.Definitions.Classic.Map;
using WAD.NET.Interfaces;

namespace WAD.NET.Concrete.Maps
{
    /// <summary>
    /// Lump containing subsector data for a DOOM format map.
    /// </summary>
    public sealed class SubsectorsLump : Lump, IMapLump
    {
        /// <summary>
        /// The subsectors in this lump.
        /// </summary>
        public Subsector[] Subsectors { get; }

        /// <summary>
        /// Number of subsectors in this lump.
        /// </summary>
        public int Count => Subsectors.Length;

        /// <summary>
        /// Creates a new SubsectorsLump by parsing binary data.
        /// </summary>
        /// <param name="name">Lump name.</param>
        /// <param name="source">Source WAD name.</param>
        /// <param name="data">Binary lump data.</param>
        public SubsectorsLump(string name, string source, ReadOnlySpan<byte> data)
            : base(name, source)
        {
            int count = data.Length / Subsector.EntrySize;
            Subsectors = new Subsector[count];

            for (int i = 0; i < count; i++)
            {
                Subsectors[i] = Subsector.Parse(data.Slice(i * Subsector.EntrySize, Subsector.EntrySize));
            }
        }

        /// <summary>
        /// Gets the subsector at the specified index.
        /// </summary>
        public Subsector this[int index] => Subsectors[index];
    }
}
