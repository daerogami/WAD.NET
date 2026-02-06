using System;
using System.Buffers.Binary;
using System.Text;
using WAD.NET.Definitions.GameData;

namespace WAD.NET.Definitions.Classic.Map
{
    /// <summary>
    /// Represents a sector in a DOOM format map.
    /// </summary>
    /// <remarks>
    /// Binary format: 26 bytes per sector.
    /// </remarks>
    public readonly struct DoomSector
    {
        /// <summary>
        /// Size of a sector entry in bytes.
        /// </summary>
        public const int EntrySize = 26;

        /// <summary>
        /// Floor height in map units.
        /// </summary>
        public short FloorHeight { get; init; }

        /// <summary>
        /// Ceiling height in map units.
        /// </summary>
        public short CeilingHeight { get; init; }

        /// <summary>
        /// Floor texture name (8 characters max).
        /// </summary>
        public string FloorTexture { get; init; }

        /// <summary>
        /// Ceiling texture name (8 characters max).
        /// </summary>
        public string CeilingTexture { get; init; }

        /// <summary>
        /// Light level (0-255).
        /// </summary>
        public ushort LightLevel { get; init; }

        /// <summary>
        /// Sector special type (damage, secret, etc.).
        /// </summary>
        public ushort Special { get; init; }

        /// <summary>
        /// Sector tag for triggering actions.
        /// </summary>
        public ushort Tag { get; init; }

        /// <summary>
        /// Height of the sector (ceiling - floor).
        /// </summary>
        public int Height => CeilingHeight - FloorHeight;

        /// <summary>
        /// True if this is a secret sector.
        /// </summary>
        public bool IsSecret => SectorDatabase.IsSecret(Special);

        /// <summary>
        /// True if this sector causes damage.
        /// </summary>
        public bool IsDamaging => SectorDatabase.IsDamaging(Special);

        /// <summary>
        /// Parses a DoomSector from binary data.
        /// </summary>
        /// <param name="data">The binary data (must be at least 26 bytes).</param>
        /// <returns>A parsed DoomSector.</returns>
        public static DoomSector Parse(ReadOnlySpan<byte> data)
        {
            if (data.Length < EntrySize)
                throw new ArgumentException($"Data must be at least {EntrySize} bytes", nameof(data));

            return new DoomSector
            {
                FloorHeight = BinaryPrimitives.ReadInt16LittleEndian(data),
                CeilingHeight = BinaryPrimitives.ReadInt16LittleEndian(data.Slice(2)),
                FloorTexture = ReadTextureName(data.Slice(4, 8)),
                CeilingTexture = ReadTextureName(data.Slice(12, 8)),
                LightLevel = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(20)),
                Special = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(22)),
                Tag = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(24))
            };
        }

        private static string ReadTextureName(ReadOnlySpan<byte> data)
        {
            int length = data.IndexOf((byte)0);
            if (length < 0) length = 8;

#if NETSTANDARD2_1_OR_GREATER
            return Encoding.ASCII.GetString(data.Slice(0, length)).ToUpperInvariant();
#else
            return Encoding.ASCII.GetString(data.Slice(0, length).ToArray()).ToUpperInvariant();
#endif
        }
    }
}
