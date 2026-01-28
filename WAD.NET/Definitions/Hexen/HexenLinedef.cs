using System;
using System.Buffers.Binary;

namespace WAD.NET.Definitions.Hexen
{
    /// <summary>
    /// Represents a linedef in a Hexen format map.
    /// </summary>
    /// <remarks>
    /// Binary format: 16 bytes per linedef.
    /// Extended from DOOM format with script args instead of tag.
    /// </remarks>
    public readonly struct HexenLinedef
    {
        /// <summary>
        /// Size of a Hexen linedef entry in bytes.
        /// </summary>
        public const int EntrySize = 16;

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
        public HexenLinedefFlags Flags { get; init; }

        /// <summary>
        /// Special action type.
        /// </summary>
        public byte Special { get; init; }

        /// <summary>
        /// Argument 0 for the special.
        /// </summary>
        public byte Arg0 { get; init; }

        /// <summary>
        /// Argument 1 for the special.
        /// </summary>
        public byte Arg1 { get; init; }

        /// <summary>
        /// Argument 2 for the special.
        /// </summary>
        public byte Arg2 { get; init; }

        /// <summary>
        /// Argument 3 for the special.
        /// </summary>
        public byte Arg3 { get; init; }

        /// <summary>
        /// Argument 4 for the special.
        /// </summary>
        public byte Arg4 { get; init; }

        /// <summary>
        /// Index of the front (right) sidedef.
        /// </summary>
        public ushort FrontSidedef { get; init; }

        /// <summary>
        /// Index of the back (left) sidedef. 0xFFFF if none.
        /// </summary>
        public ushort BackSidedef { get; init; }

        /// <summary>
        /// Gets all arguments as an array.
        /// </summary>
        public byte[] Args => new[] { Arg0, Arg1, Arg2, Arg3, Arg4 };

        /// <summary>
        /// True if this linedef has a back side.
        /// </summary>
        public bool HasBackSide => BackSidedef != NoSidedef;

        /// <summary>
        /// True if this linedef is two-sided.
        /// </summary>
        public bool IsTwoSided => (Flags & HexenLinedefFlags.TwoSided) != 0;

        /// <summary>
        /// Gets the activation type for this linedef.
        /// </summary>
        public HexenActivationType ActivationType => (HexenActivationType)((ushort)Flags & 0x1C00);

        /// <summary>
        /// Parses a HexenLinedef from binary data.
        /// </summary>
        /// <param name="data">The binary data (must be at least 16 bytes).</param>
        /// <returns>A parsed HexenLinedef.</returns>
        public static HexenLinedef Parse(ReadOnlySpan<byte> data)
        {
            if (data.Length < EntrySize)
                throw new ArgumentException($"Data must be at least {EntrySize} bytes", nameof(data));

            return new HexenLinedef
            {
                StartVertex = BinaryPrimitives.ReadUInt16LittleEndian(data),
                EndVertex = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(2)),
                Flags = (HexenLinedefFlags)BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(4)),
                Special = data[6],
                Arg0 = data[7],
                Arg1 = data[8],
                Arg2 = data[9],
                Arg3 = data[10],
                Arg4 = data[11],
                FrontSidedef = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(12)),
                BackSidedef = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(14))
            };
        }
    }
}
