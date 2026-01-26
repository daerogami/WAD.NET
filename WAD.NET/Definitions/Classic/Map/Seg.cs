using System;
using System.Buffers.Binary;

namespace WAD.NET.Definitions.Classic.Map
{
    /// <summary>
    /// Represents a seg (segment) in the BSP tree.
    /// </summary>
    /// <remarks>
    /// Binary format: 12 bytes per seg.
    /// Segs are portions of linedefs used by the BSP renderer.
    /// </remarks>
    public readonly struct Seg
    {
        /// <summary>
        /// Size of a seg entry in bytes.
        /// </summary>
        public const int EntrySize = 12;

        /// <summary>
        /// Index of the start vertex.
        /// </summary>
        public ushort StartVertex { get; init; }

        /// <summary>
        /// Index of the end vertex.
        /// </summary>
        public ushort EndVertex { get; init; }

        /// <summary>
        /// Angle in Binary Angle Measurement (BAM) format.
        /// 0x0000 = East, 0x4000 = North, 0x8000 = West, 0xC000 = South.
        /// </summary>
        public short Angle { get; init; }

        /// <summary>
        /// Index of the linedef this seg is part of.
        /// </summary>
        public ushort LinedefIndex { get; init; }

        /// <summary>
        /// Direction: 0 = same as linedef, 1 = opposite direction.
        /// </summary>
        public ushort Direction { get; init; }

        /// <summary>
        /// Offset distance from start of linedef to start of seg.
        /// </summary>
        public short Offset { get; init; }

        /// <summary>
        /// True if this seg runs in the opposite direction of the linedef.
        /// </summary>
        public bool IsBackSide => Direction != 0;

        /// <summary>
        /// Converts the BAM angle to degrees.
        /// </summary>
        public double AngleDegrees => Angle * 360.0 / 65536.0;

        /// <summary>
        /// Parses a Seg from binary data.
        /// </summary>
        /// <param name="data">The binary data (must be at least 12 bytes).</param>
        /// <returns>A parsed Seg.</returns>
        public static Seg Parse(ReadOnlySpan<byte> data)
        {
            if (data.Length < EntrySize)
                throw new ArgumentException($"Data must be at least {EntrySize} bytes", nameof(data));

            return new Seg
            {
                StartVertex = BinaryPrimitives.ReadUInt16LittleEndian(data),
                EndVertex = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(2)),
                Angle = BinaryPrimitives.ReadInt16LittleEndian(data.Slice(4)),
                LinedefIndex = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(6)),
                Direction = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(8)),
                Offset = BinaryPrimitives.ReadInt16LittleEndian(data.Slice(10))
            };
        }
    }
}
