using System;
using System.Buffers.Binary;

namespace WAD.NET.Definitions.Classic.Map
{
    /// <summary>
    /// Represents a bounding box in a DOOM map.
    /// </summary>
    /// <remarks>
    /// Binary format: 8 bytes (4 shorts).
    /// </remarks>
    public readonly struct BoundingBox : IEquatable<BoundingBox>
    {
        /// <summary>
        /// Size of a bounding box in bytes.
        /// </summary>
        public const int Size = 8;

        /// <summary>
        /// Top of the bounding box (maximum Y).
        /// </summary>
        public short Top { get; init; }

        /// <summary>
        /// Bottom of the bounding box (minimum Y).
        /// </summary>
        public short Bottom { get; init; }

        /// <summary>
        /// Left edge of the bounding box (minimum X).
        /// </summary>
        public short Left { get; init; }

        /// <summary>
        /// Right edge of the bounding box (maximum X).
        /// </summary>
        public short Right { get; init; }

        /// <summary>
        /// Width of the bounding box.
        /// </summary>
        public int Width => Right - Left;

        /// <summary>
        /// Height of the bounding box.
        /// </summary>
        public int Height => Top - Bottom;

        /// <summary>
        /// Center X coordinate.
        /// </summary>
        public int CenterX => (Left + Right) / 2;

        /// <summary>
        /// Center Y coordinate.
        /// </summary>
        public int CenterY => (Top + Bottom) / 2;

        /// <summary>
        /// Creates a new bounding box.
        /// </summary>
        public BoundingBox(short top, short bottom, short left, short right)
        {
            Top = top;
            Bottom = bottom;
            Left = left;
            Right = right;
        }

        /// <summary>
        /// Parses a BoundingBox from binary data.
        /// </summary>
        /// <param name="data">The binary data (must be at least 8 bytes).</param>
        /// <returns>A parsed BoundingBox.</returns>
        public static BoundingBox Parse(ReadOnlySpan<byte> data)
        {
            if (data.Length < Size)
                throw new ArgumentException($"Data must be at least {Size} bytes", nameof(data));

            return new BoundingBox
            {
                Top = BinaryPrimitives.ReadInt16LittleEndian(data),
                Bottom = BinaryPrimitives.ReadInt16LittleEndian(data.Slice(2)),
                Left = BinaryPrimitives.ReadInt16LittleEndian(data.Slice(4)),
                Right = BinaryPrimitives.ReadInt16LittleEndian(data.Slice(6))
            };
        }

        /// <summary>
        /// Checks if a point is inside this bounding box.
        /// </summary>
        public bool Contains(short x, short y)
        {
            return x >= Left && x <= Right && y >= Bottom && y <= Top;
        }

        /// <summary>
        /// Checks if this bounding box intersects another.
        /// </summary>
        public bool Intersects(BoundingBox other)
        {
            return Left <= other.Right && Right >= other.Left &&
                   Bottom <= other.Top && Top >= other.Bottom;
        }

        public bool Equals(BoundingBox other) =>
            Top == other.Top && Bottom == other.Bottom &&
            Left == other.Left && Right == other.Right;

        public override bool Equals(object obj) => obj is BoundingBox other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Top, Bottom, Left, Right);

        public override string ToString() => $"[({Left},{Bottom}) - ({Right},{Top})]";

        public static bool operator ==(BoundingBox left, BoundingBox right) => left.Equals(right);

        public static bool operator !=(BoundingBox left, BoundingBox right) => !left.Equals(right);
    }
}
