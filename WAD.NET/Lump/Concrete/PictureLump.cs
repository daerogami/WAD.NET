using System;
using System.Buffers.Binary;
using WAD.NET.Abstract;
using WAD.NET.Definitions;

namespace WAD.NET.Concrete
{
    /// <summary>
    /// Represents a DOOM picture lump (sprite, patch, or menu graphic).
    /// Parses the column-based picture format used by DOOM engine.
    /// </summary>
    /// <remarks>
    /// Picture Header (8 + width*4 bytes):
    /// - 2 bytes: Width (uint16)
    /// - 2 bytes: Height (uint16)
    /// - 2 bytes: Left offset (int16, for sprites)
    /// - 2 bytes: Top offset (int16, for sprites)
    /// - width*4 bytes: Column offsets (uint32 per column)
    ///
    /// Each column consists of posts:
    /// - 1 byte: Top delta (row to start, 255 = end of column)
    /// - 1 byte: Length (number of pixels)
    /// - 1 byte: Padding (unused)
    /// - N bytes: Pixel data (palette indices)
    /// - 1 byte: Padding (unused)
    /// </remarks>
    public sealed class PictureLump : Lump
    {
        /// <summary>
        /// Minimum size for a valid picture (header only, 0x0 picture).
        /// </summary>
        private const int MinHeaderSize = 8;

        /// <summary>
        /// Marker byte indicating end of a column.
        /// </summary>
        private const byte EndOfColumn = 255;

        /// <summary>
        /// The parsed picture data.
        /// </summary>
        public DoomPicture Picture { get; }

        public PictureLump(string lumpName, string sourceWadFileName, ReadOnlySpan<byte> data)
            : base(lumpName, sourceWadFileName)
        {
            Picture = ParsePicture(lumpName, data);
        }

        /// <summary>
        /// Parses picture data from raw bytes.
        /// </summary>
        public static DoomPicture ParsePicture(string name, ReadOnlySpan<byte> data)
        {
            if (data.Length < MinHeaderSize)
            {
                throw new FormatException(
                    $"Picture '{name}' must be at least {MinHeaderSize} bytes, got {data.Length}");
            }

            var width = BinaryPrimitives.ReadUInt16LittleEndian(data);
            var height = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(2));
            var leftOffset = BinaryPrimitives.ReadInt16LittleEndian(data.Slice(4));
            var topOffset = BinaryPrimitives.ReadInt16LittleEndian(data.Slice(6));

            // Validate dimensions
            if (width == 0 || height == 0)
            {
                throw new FormatException(
                    $"Picture '{name}' has invalid dimensions: {width}x{height}");
            }

            // Validate we have enough space for column offsets
            int headerWithOffsets = MinHeaderSize + width * 4;
            if (data.Length < headerWithOffsets)
            {
                throw new FormatException(
                    $"Picture '{name}' truncated: expected at least {headerWithOffsets} bytes for header and column offsets, got {data.Length}");
            }

            // Initialize pixel array with transparency
            var pixels = new byte[width * height];
            Array.Fill(pixels, DoomPicture.TransparentIndex);

            // Read column offsets
            var columnOffsets = new uint[width];
            for (int i = 0; i < width; i++)
            {
                columnOffsets[i] = BinaryPrimitives.ReadUInt32LittleEndian(
                    data.Slice(MinHeaderSize + i * 4, 4));
            }

            // Parse each column
            for (int x = 0; x < width; x++)
            {
                ParseColumn(data, columnOffsets[x], x, width, height, pixels);
            }

            return new DoomPicture(width, height, leftOffset, topOffset, pixels);
        }

        /// <summary>
        /// Parses a single column of picture data.
        /// </summary>
        private static void ParseColumn(
            ReadOnlySpan<byte> data,
            uint offset,
            int x,
            int width,
            int height,
            byte[] pixels)
        {
            int pos = (int)offset;

            // Bounds check
            if (pos >= data.Length)
                return;

            while (pos < data.Length)
            {
                byte topDelta = data[pos++];

                // End of column marker
                if (topDelta == EndOfColumn)
                    break;

                // Need at least length byte and padding bytes
                if (pos + 2 >= data.Length)
                    break;

                byte length = data[pos++];
                pos++; // Skip pre-pixel padding

                // Read pixels
                for (int i = 0; i < length && pos < data.Length; i++)
                {
                    int y = topDelta + i;
                    if (y >= 0 && y < height)
                    {
                        pixels[y * width + x] = data[pos];
                    }
                    pos++;
                }

                // Skip post-pixel padding
                if (pos < data.Length)
                    pos++;
            }
        }

        /// <summary>
        /// Attempts to determine if the given data looks like a valid DOOM picture.
        /// </summary>
        /// <remarks>
        /// This performs basic validation to help distinguish picture lumps from other data.
        /// It checks:
        /// - Minimum size requirements
        /// - Reasonable dimensions (not too large)
        /// - Column offsets point within the data
        /// </remarks>
        public static bool LooksLikePicture(ReadOnlySpan<byte> data)
        {
            if (data.Length < MinHeaderSize)
                return false;

            var width = BinaryPrimitives.ReadUInt16LittleEndian(data);
            var height = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(2));

            // Sanity check dimensions (DOOM max is around 320x200 for most graphics)
            if (width == 0 || height == 0 || width > 2048 || height > 2048)
                return false;

            // Check if we have enough data for header and column offsets
            int expectedMinSize = MinHeaderSize + width * 4;
            if (data.Length < expectedMinSize)
                return false;

            // Verify first column offset is reasonable
            var firstOffset = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(MinHeaderSize, 4));
            if (firstOffset < expectedMinSize || firstOffset >= data.Length)
                return false;

            return true;
        }
    }
}
