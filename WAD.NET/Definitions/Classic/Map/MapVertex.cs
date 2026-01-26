using System;
using System.Buffers.Binary;

namespace WAD.NET.Definitions.Classic.Map
{
    /// <summary>
    /// Represents a vertex in a DOOM format map.
    /// </summary>
    /// <remarks>
    /// Binary format: 4 bytes per vertex.
    /// Note: This is distinct from UDMF vertex which uses floating point.
    /// </remarks>
    public readonly struct MapVertex : IEquatable<MapVertex>
    {
        /// <summary>
        /// Size of a vertex entry in bytes.
        /// </summary>
        public const int EntrySize = 4;

        /// <summary>
        /// X coordinate in map units.
        /// </summary>
        public short X { get; init; }

        /// <summary>
        /// Y coordinate in map units.
        /// </summary>
        public short Y { get; init; }

        /// <summary>
        /// Creates a new vertex with the specified coordinates.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        public MapVertex(short x, short y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Parses a MapVertex from binary data.
        /// </summary>
        /// <param name="data">The binary data (must be at least 4 bytes).</param>
        /// <returns>A parsed MapVertex.</returns>
        public static MapVertex Parse(ReadOnlySpan<byte> data)
        {
            if (data.Length < EntrySize)
                throw new ArgumentException($"Data must be at least {EntrySize} bytes", nameof(data));

            return new MapVertex
            {
                X = BinaryPrimitives.ReadInt16LittleEndian(data),
                Y = BinaryPrimitives.ReadInt16LittleEndian(data.Slice(2))
            };
        }

        /// <summary>
        /// Calculates the distance to another vertex.
        /// </summary>
        /// <param name="other">The other vertex.</param>
        /// <returns>The distance in map units.</returns>
        public double DistanceTo(MapVertex other)
        {
            double dx = other.X - X;
            double dy = other.Y - Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        public bool Equals(MapVertex other) => X == other.X && Y == other.Y;

        public override bool Equals(object obj) => obj is MapVertex other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(X, Y);

        public override string ToString() => $"({X}, {Y})";

        public static bool operator ==(MapVertex left, MapVertex right) => left.Equals(right);

        public static bool operator !=(MapVertex left, MapVertex right) => !left.Equals(right);
    }
}
