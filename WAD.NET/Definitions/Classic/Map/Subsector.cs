using System;
using System.Buffers.Binary;

namespace WAD.NET.Definitions.Classic.Map
{
    /// <summary>
    /// Represents a subsector in the BSP tree.
    /// </summary>
    /// <remarks>
    /// Binary format: 4 bytes per subsector.
    /// Subsectors are convex regions defined by a list of segs.
    /// </remarks>
    public readonly struct Subsector
    {
        /// <summary>
        /// Size of a subsector entry in bytes.
        /// </summary>
        public const int EntrySize = 4;

        /// <summary>
        /// Number of segs in this subsector.
        /// </summary>
        public ushort SegCount { get; init; }

        /// <summary>
        /// Index of the first seg in this subsector.
        /// </summary>
        public ushort FirstSeg { get; init; }

        /// <summary>
        /// Parses a Subsector from binary data.
        /// </summary>
        /// <param name="data">The binary data (must be at least 4 bytes).</param>
        /// <returns>A parsed Subsector.</returns>
        public static Subsector Parse(ReadOnlySpan<byte> data)
        {
            if (data.Length < EntrySize)
                throw new ArgumentException($"Data must be at least {EntrySize} bytes", nameof(data));

            return new Subsector
            {
                SegCount = BinaryPrimitives.ReadUInt16LittleEndian(data),
                FirstSeg = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(2))
            };
        }
    }
}
