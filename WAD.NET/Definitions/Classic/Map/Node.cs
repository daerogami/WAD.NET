using System;
using System.Buffers.Binary;

namespace WAD.NET.Definitions.Classic.Map
{
    /// <summary>
    /// Represents a node in the BSP tree.
    /// </summary>
    /// <remarks>
    /// Binary format: 28 bytes per node.
    /// Nodes define the BSP partition lines and child references.
    /// </remarks>
    public readonly struct Node
    {
        /// <summary>
        /// Size of a node entry in bytes.
        /// </summary>
        public const int EntrySize = 28;

        /// <summary>
        /// Bit mask indicating a subsector reference (bit 15 set).
        /// </summary>
        public const ushort SubsectorMask = 0x8000;

        /// <summary>
        /// X coordinate of partition line start.
        /// </summary>
        public short PartitionX { get; init; }

        /// <summary>
        /// Y coordinate of partition line start.
        /// </summary>
        public short PartitionY { get; init; }

        /// <summary>
        /// X change along partition line.
        /// </summary>
        public short PartitionDx { get; init; }

        /// <summary>
        /// Y change along partition line.
        /// </summary>
        public short PartitionDy { get; init; }

        /// <summary>
        /// Bounding box for right child.
        /// </summary>
        public BoundingBox RightBounds { get; init; }

        /// <summary>
        /// Bounding box for left child.
        /// </summary>
        public BoundingBox LeftBounds { get; init; }

        /// <summary>
        /// Right child index. If bit 15 is set, refers to a subsector.
        /// </summary>
        public ushort RightChild { get; init; }

        /// <summary>
        /// Left child index. If bit 15 is set, refers to a subsector.
        /// </summary>
        public ushort LeftChild { get; init; }

        /// <summary>
        /// True if the right child is a subsector.
        /// </summary>
        public bool RightIsSubsector => (RightChild & SubsectorMask) != 0;

        /// <summary>
        /// True if the left child is a subsector.
        /// </summary>
        public bool LeftIsSubsector => (LeftChild & SubsectorMask) != 0;

        /// <summary>
        /// Gets the right child node index (only valid if RightIsSubsector is false).
        /// </summary>
        public ushort RightNodeIndex => RightChild;

        /// <summary>
        /// Gets the left child node index (only valid if LeftIsSubsector is false).
        /// </summary>
        public ushort LeftNodeIndex => LeftChild;

        /// <summary>
        /// Gets the right child subsector index (only valid if RightIsSubsector is true).
        /// </summary>
        public ushort RightSubsectorIndex => (ushort)(RightChild & ~SubsectorMask);

        /// <summary>
        /// Gets the left child subsector index (only valid if LeftIsSubsector is true).
        /// </summary>
        public ushort LeftSubsectorIndex => (ushort)(LeftChild & ~SubsectorMask);

        /// <summary>
        /// Determines which side of the partition line a point is on.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        /// <returns>Positive for right side, negative for left side, zero on line.</returns>
        public int PointOnSide(int x, int y)
        {
            // Cross product to determine side
            int dx = x - PartitionX;
            int dy = y - PartitionY;
            return dy * PartitionDx - dx * PartitionDy;
        }

        /// <summary>
        /// Parses a Node from binary data.
        /// </summary>
        /// <param name="data">The binary data (must be at least 28 bytes).</param>
        /// <returns>A parsed Node.</returns>
        public static Node Parse(ReadOnlySpan<byte> data)
        {
            if (data.Length < EntrySize)
                throw new ArgumentException($"Data must be at least {EntrySize} bytes", nameof(data));

            return new Node
            {
                PartitionX = BinaryPrimitives.ReadInt16LittleEndian(data),
                PartitionY = BinaryPrimitives.ReadInt16LittleEndian(data.Slice(2)),
                PartitionDx = BinaryPrimitives.ReadInt16LittleEndian(data.Slice(4)),
                PartitionDy = BinaryPrimitives.ReadInt16LittleEndian(data.Slice(6)),
                RightBounds = BoundingBox.Parse(data.Slice(8)),
                LeftBounds = BoundingBox.Parse(data.Slice(16)),
                RightChild = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(24)),
                LeftChild = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(26))
            };
        }
    }
}
