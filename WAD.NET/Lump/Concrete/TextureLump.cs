using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;
using WAD.NET.Abstract;
using WAD.NET.Definitions;

namespace WAD.NET.Concrete
{
    /// <summary>
    /// Represents a TEXTURE1 or TEXTURE2 lump containing composite texture definitions.
    /// </summary>
    /// <remarks>
    /// Format:
    /// - 4 bytes: Number of textures (int32)
    /// - N * 4 bytes: Offsets to texture definitions (int32[])
    /// - Texture definitions at each offset
    ///
    /// Texture Definition (22 bytes + patches):
    /// - 8 bytes: Name
    /// - 4 bytes: Masked (unused)
    /// - 2 bytes: Width
    /// - 2 bytes: Height
    /// - 4 bytes: Column directory (unused)
    /// - 2 bytes: Patch count
    /// - N * 10 bytes: Patch descriptors
    ///
    /// Patch Descriptor (10 bytes):
    /// - 2 bytes: X offset
    /// - 2 bytes: Y offset
    /// - 2 bytes: Patch index (into PNAMES)
    /// - 2 bytes: Step dir (unused)
    /// - 2 bytes: Colormap (unused)
    /// </remarks>
    public sealed class TextureLump : Lump
    {
        /// <summary>
        /// The texture definitions in this lump.
        /// </summary>
        public TextureDefinition[] Textures { get; }

        /// <summary>
        /// Number of textures in this lump.
        /// </summary>
        public int Count => Textures.Length;

        public TextureLump(string lumpName, string sourceWadFileName, ReadOnlySpan<byte> data)
            : base(lumpName, sourceWadFileName)
        {
            if (data.Length < 4)
            {
                throw new FormatException(
                    $"Texture lump must be at least 4 bytes, got {data.Length}");
            }

            int count = BinaryPrimitives.ReadInt32LittleEndian(data);

            if (data.Length < 4 + count * 4)
            {
                throw new FormatException(
                    $"Texture lump too small for offset table");
            }

            // Read offset table
            var offsets = new int[count];
            for (int i = 0; i < count; i++)
            {
                offsets[i] = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(4 + i * 4, 4));
            }

            // Parse each texture definition
            Textures = new TextureDefinition[count];
            for (int i = 0; i < count; i++)
            {
                Textures[i] = ParseTextureDefinition(data, offsets[i]);
            }
        }

        /// <summary>
        /// Gets the texture definition at the specified index.
        /// </summary>
        public TextureDefinition this[int index] => Textures[index];

        /// <summary>
        /// Finds a texture by name.
        /// </summary>
        /// <returns>The texture definition, or null if not found.</returns>
        public TextureDefinition Find(string name)
        {
            for (int i = 0; i < Textures.Length; i++)
            {
                if (Textures[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return Textures[i];
            }
            return null;
        }

        private static TextureDefinition ParseTextureDefinition(ReadOnlySpan<byte> data, int offset)
        {
            if (offset + 22 > data.Length)
            {
                throw new FormatException(
                    $"Texture definition at offset {offset} extends beyond lump");
            }

            var texData = data.Slice(offset);

            var name = ReadNullTerminatedString(texData.Slice(0, 8));
            // Skip masked (4 bytes)
            var width = BinaryPrimitives.ReadUInt16LittleEndian(texData.Slice(12, 2));
            var height = BinaryPrimitives.ReadUInt16LittleEndian(texData.Slice(14, 2));
            // Skip column directory (4 bytes)
            var patchCount = BinaryPrimitives.ReadUInt16LittleEndian(texData.Slice(20, 2));

            if (offset + 22 + patchCount * 10 > data.Length)
            {
                throw new FormatException(
                    $"Texture '{name}' patch data extends beyond lump");
            }

            var patches = new TexturePatch[patchCount];
            for (int i = 0; i < patchCount; i++)
            {
                var patchData = texData.Slice(22 + i * 10, 10);
                var originX = BinaryPrimitives.ReadInt16LittleEndian(patchData);
                var originY = BinaryPrimitives.ReadInt16LittleEndian(patchData.Slice(2));
                var patchIndex = BinaryPrimitives.ReadUInt16LittleEndian(patchData.Slice(4));
                // Skip step dir (2 bytes) and colormap (2 bytes)

                patches[i] = new TexturePatch(originX, originY, patchIndex);
            }

            return new TextureDefinition(name, width, height, patches);
        }

        private static string ReadNullTerminatedString(ReadOnlySpan<byte> data)
        {
            int length = data.IndexOf((byte)0);
            if (length < 0) length = data.Length;
            return Encoding.ASCII.GetString(data.Slice(0, length)).TrimEnd('\0').ToUpperInvariant();
        }
    }
}
