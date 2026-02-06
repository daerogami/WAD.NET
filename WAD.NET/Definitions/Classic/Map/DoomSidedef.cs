using System;
using System.Buffers.Binary;
using System.Text;
using WAD.NET.Definitions;

namespace WAD.NET.Definitions.Classic.Map
{
    /// <summary>
    /// Represents a sidedef in a DOOM format map.
    /// </summary>
    /// <remarks>
    /// Binary format: 30 bytes per sidedef.
    /// </remarks>
    public readonly struct DoomSidedef
    {
        /// <summary>
        /// Size of a sidedef entry in bytes.
        /// </summary>
        public const int EntrySize = 30;

        /// <summary>
        /// X texture offset in pixels.
        /// </summary>
        public short XOffset { get; init; }

        /// <summary>
        /// Y texture offset in pixels.
        /// </summary>
        public short YOffset { get; init; }

        /// <summary>
        /// Upper texture name (8 characters max).
        /// </summary>
        public string UpperTexture { get; init; }

        /// <summary>
        /// Lower texture name (8 characters max).
        /// </summary>
        public string LowerTexture { get; init; }

        /// <summary>
        /// Middle texture name (8 characters max).
        /// </summary>
        public string MiddleTexture { get; init; }

        /// <summary>
        /// Index of the sector this sidedef faces.
        /// </summary>
        public ushort Sector { get; init; }

        /// <summary>
        /// True if this sidedef has an upper texture.
        /// </summary>
        public bool HasUpperTexture => !TextureConstants.IsNull(UpperTexture);

        /// <summary>
        /// True if this sidedef has a lower texture.
        /// </summary>
        public bool HasLowerTexture => !TextureConstants.IsNull(LowerTexture);

        /// <summary>
        /// True if this sidedef has a middle texture.
        /// </summary>
        public bool HasMiddleTexture => !TextureConstants.IsNull(MiddleTexture);

        /// <summary>
        /// Parses a DoomSidedef from binary data.
        /// </summary>
        /// <param name="data">The binary data (must be at least 30 bytes).</param>
        /// <returns>A parsed DoomSidedef.</returns>
        public static DoomSidedef Parse(ReadOnlySpan<byte> data)
        {
            if (data.Length < EntrySize)
                throw new ArgumentException($"Data must be at least {EntrySize} bytes", nameof(data));

            return new DoomSidedef
            {
                XOffset = BinaryPrimitives.ReadInt16LittleEndian(data),
                YOffset = BinaryPrimitives.ReadInt16LittleEndian(data.Slice(2)),
                UpperTexture = ReadTextureName(data.Slice(4, 8)),
                LowerTexture = ReadTextureName(data.Slice(12, 8)),
                MiddleTexture = ReadTextureName(data.Slice(20, 8)),
                Sector = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(28))
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
