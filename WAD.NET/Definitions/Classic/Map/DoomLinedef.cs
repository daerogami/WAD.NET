using System;
using System.Buffers.Binary;

namespace WAD.NET.Definitions.Classic.Map
{
    /// <summary>
    /// Represents a linedef in a DOOM format map.
    /// </summary>
    /// <remarks>
    /// Binary format: 14 bytes per linedef.
    /// </remarks>
    public readonly struct DoomLinedef
    {
        /// <summary>
        /// Size of a linedef entry in bytes.
        /// </summary>
        public const int EntrySize = 14;

        /// <summary>
        /// Value indicating no sidedef (0xFFFF).
        /// </summary>
        public const ushort NoSidedef = 0xFFFF;

        /// <summary>
        /// Index of the start vertex.
        /// </summary>
        public ushort StartVertex { get; init; }

        /// <summary>
        /// Index of the end vertex.
        /// </summary>
        public ushort EndVertex { get; init; }

        /// <summary>
        /// Linedef flags.
        /// </summary>
        public LinedefFlags Flags { get; init; }

        /// <summary>
        /// Special action type.
        /// </summary>
        public ushort Special { get; init; }

        /// <summary>
        /// Sector tag for triggering actions.
        /// </summary>
        public ushort Tag { get; init; }

        /// <summary>
        /// Index of the front (right) sidedef.
        /// </summary>
        public ushort FrontSidedef { get; init; }

        /// <summary>
        /// Index of the back (left) sidedef. 0xFFFF if none.
        /// </summary>
        public ushort BackSidedef { get; init; }

        /// <summary>
        /// True if this linedef has a back side.
        /// </summary>
        public bool HasBackSide => BackSidedef != NoSidedef;

        /// <summary>
        /// True if this linedef is two-sided.
        /// </summary>
        public bool IsTwoSided => (Flags & LinedefFlags.TwoSided) != 0;

        /// <summary>
        /// True if this linedef blocks players and monsters.
        /// </summary>
        public bool IsImpassable => (Flags & LinedefFlags.Impassable) != 0;

        /// <summary>
        /// Parses a DoomLinedef from binary data.
        /// </summary>
        /// <param name="data">The binary data (must be at least 14 bytes).</param>
        /// <returns>A parsed DoomLinedef.</returns>
        public static DoomLinedef Parse(ReadOnlySpan<byte> data)
        {
            if (data.Length < EntrySize)
                throw new ArgumentException($"Data must be at least {EntrySize} bytes", nameof(data));

            return new DoomLinedef
            {
                StartVertex = BinaryPrimitives.ReadUInt16LittleEndian(data),
                EndVertex = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(2)),
                Flags = (LinedefFlags)BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(4)),
                Special = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(6)),
                Tag = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(8)),
                FrontSidedef = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(10)),
                BackSidedef = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(12))
            };
        }
    }
}
