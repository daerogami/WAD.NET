using System;
using WAD.NET.Abstract;
using WAD.NET.Definitions.Classic.Map;
using WAD.NET.Interfaces;

namespace WAD.NET.Concrete.Maps
{
    /// <summary>
    /// Lump containing thing (entity) data for a DOOM format map.
    /// </summary>
    public sealed class ThingsLump : Lump, IMapLump
    {
        /// <summary>
        /// The things in this lump.
        /// </summary>
        public DoomThing[] Things { get; }

        /// <summary>
        /// Number of things in this lump.
        /// </summary>
        public int Count => Things.Length;

        /// <summary>
        /// Creates a new ThingsLump by parsing binary data.
        /// </summary>
        /// <param name="name">Lump name.</param>
        /// <param name="source">Source WAD name.</param>
        /// <param name="data">Binary lump data.</param>
        public ThingsLump(string name, string source, ReadOnlySpan<byte> data)
            : base(name, source)
        {
            int count = data.Length / DoomThing.EntrySize;
            Things = new DoomThing[count];

            for (int i = 0; i < count; i++)
            {
                Things[i] = DoomThing.Parse(data.Slice(i * DoomThing.EntrySize, DoomThing.EntrySize));
            }
        }

        /// <summary>
        /// Gets the thing at the specified index.
        /// </summary>
        public DoomThing this[int index] => Things[index];
    }
}
