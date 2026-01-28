using System;
using WAD.NET.Abstract;
using WAD.NET.Definitions.Hexen;
using WAD.NET.Interfaces;

namespace WAD.NET.Concrete.Maps
{
    /// <summary>
    /// Lump containing thing (entity) data for a Hexen format map.
    /// </summary>
    public sealed class HexenThingsLump : Lump, IMapLump
    {
        /// <summary>
        /// The things in this lump.
        /// </summary>
        public HexenThing[] Things { get; }

        /// <summary>
        /// Number of things in this lump.
        /// </summary>
        public int Count => Things.Length;

        /// <summary>
        /// Creates a new HexenThingsLump by parsing binary data.
        /// </summary>
        /// <param name="name">Lump name.</param>
        /// <param name="source">Source WAD name.</param>
        /// <param name="data">Binary lump data.</param>
        public HexenThingsLump(string name, string source, ReadOnlySpan<byte> data)
            : base(name, source)
        {
            int count = data.Length / HexenThing.EntrySize;
            Things = new HexenThing[count];

            for (int i = 0; i < count; i++)
            {
                Things[i] = HexenThing.Parse(data.Slice(i * HexenThing.EntrySize, HexenThing.EntrySize));
            }
        }

        /// <summary>
        /// Gets the thing at the specified index.
        /// </summary>
        public HexenThing this[int index] => Things[index];
    }
}
