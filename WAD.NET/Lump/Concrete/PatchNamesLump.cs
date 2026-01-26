using System;
using System.Buffers.Binary;
using System.Text;
using WAD.NET.Abstract;

namespace WAD.NET.Concrete
{
    /// <summary>
    /// Represents the PNAMES lump containing a list of patch names used in textures.
    /// </summary>
    /// <remarks>
    /// Format:
    /// - 4 bytes: Number of patches (int32)
    /// - N * 8 bytes: Patch names (8-char, null-padded ASCII)
    /// </remarks>
    public sealed class PatchNamesLump : Lump
    {
        /// <summary>
        /// Size of a patch name entry in bytes.
        /// </summary>
        public const int NameSize = 8;

        /// <summary>
        /// The list of patch names.
        /// </summary>
        public string[] PatchNames { get; }

        /// <summary>
        /// Number of patches in this lump.
        /// </summary>
        public int Count => PatchNames.Length;

        public PatchNamesLump(string lumpName, string sourceWadFileName, ReadOnlySpan<byte> data)
            : base(lumpName, sourceWadFileName)
        {
            if (data.Length < 4)
            {
                throw new FormatException(
                    $"PNAMES lump must be at least 4 bytes, got {data.Length}");
            }

            int count = BinaryPrimitives.ReadInt32LittleEndian(data);

            int expectedSize = 4 + count * NameSize;
            if (data.Length < expectedSize)
            {
                throw new FormatException(
                    $"PNAMES lump too small: expected {expectedSize} bytes for {count} patches, got {data.Length}");
            }

            PatchNames = new string[count];
            for (int i = 0; i < count; i++)
            {
                var nameSpan = data.Slice(4 + i * NameSize, NameSize);
                PatchNames[i] = ReadNullTerminatedString(nameSpan);
            }
        }

        /// <summary>
        /// Gets the patch name at the specified index.
        /// </summary>
        public string this[int index] => PatchNames[index];

        /// <summary>
        /// Finds the index of a patch by name.
        /// </summary>
        /// <returns>The index, or -1 if not found.</returns>
        public int IndexOf(string name)
        {
            for (int i = 0; i < PatchNames.Length; i++)
            {
                if (PatchNames[i].Equals(name, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        private static string ReadNullTerminatedString(ReadOnlySpan<byte> data)
        {
            int length = data.IndexOf((byte)0);
            if (length < 0) length = data.Length;
            return Encoding.ASCII.GetString(data.Slice(0, length)).TrimEnd('\0').ToUpperInvariant();
        }
    }
}
