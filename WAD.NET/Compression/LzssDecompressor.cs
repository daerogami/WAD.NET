using System;

namespace WAD.NET.Compression
{
    /// <summary>
    /// LZSS decompression support for Jaguar DOOM WAD files.
    /// </summary>
    /// <remarks>
    /// LZSS (Lempel-Ziv-Storer-Szymanski) is a compression algorithm used in
    /// Jaguar DOOM WADs. Compressed lumps are identified by having the high bit
    /// (0x80) set in the first byte of the lump name in the directory entry.
    ///
    /// The Jaguar DOOM LZSS variant uses:
    /// - 4096 byte sliding window
    /// - Flag byte where each bit indicates literal (1) or back-reference (0)
    /// - Back-references: 12-bit offset + 4-bit length (minimum length = 3)
    /// - 8 chunks processed per flag byte
    ///
    /// See: https://doomwiki.org/wiki/WAD#Compression
    /// </remarks>
    public static class LzssDecompressor
    {
        /// <summary>
        /// Size of the sliding window buffer used for decompression.
        /// </summary>
        public const int WindowSize = 4096;

        /// <summary>
        /// Mask for window position wraparound (WindowSize - 1).
        /// </summary>
        private const int WindowMask = WindowSize - 1;

        /// <summary>
        /// Initial position in the sliding window.
        /// Standard LZSS uses WindowSize - MaxMatchLength (4096 - 18 = 4078).
        /// </summary>
        private const int InitialWindowPosition = WindowSize - 18;

        /// <summary>
        /// Minimum match length for back-references.
        /// </summary>
        private const int MinMatchLength = 3;

        /// <summary>
        /// Number of bits processed per flag byte.
        /// </summary>
        private const int BitsPerFlagByte = 8;

        /// <summary>
        /// Decompresses LZSS-compressed data using the Jaguar DOOM format.
        /// </summary>
        /// <param name="compressedData">The compressed data bytes.</param>
        /// <param name="uncompressedSize">Expected uncompressed size in bytes.</param>
        /// <returns>The decompressed data.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="compressedData"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="uncompressedSize"/> is negative.
        /// </exception>
        /// <exception cref="FormatException">
        /// Thrown when the compressed data is malformed or truncated.
        /// </exception>
        public static byte[] Decompress(byte[] compressedData, int uncompressedSize)
        {
            if (compressedData == null)
            {
                throw new ArgumentNullException(nameof(compressedData));
            }

            if (uncompressedSize < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(uncompressedSize),
                    uncompressedSize,
                    "Uncompressed size cannot be negative.");
            }

            if (uncompressedSize == 0)
            {
                return Array.Empty<byte>();
            }

            var output = new byte[uncompressedSize];
            var window = new byte[WindowSize];
            int windowPos = InitialWindowPosition;
            int srcPos = 0;
            int dstPos = 0;

            // Initialize sliding window with spaces (common LZSS initialization)
            // Some implementations use zeros, but Jaguar DOOM uses space (0x20)
            for (int i = 0; i < WindowSize; i++)
            {
                window[i] = 0x20;
            }

            while (dstPos < uncompressedSize)
            {
                // Read flag byte - each bit indicates literal (1) or reference (0)
                if (srcPos >= compressedData.Length)
                {
                    throw new FormatException(
                        $"LZSS decompression failed: unexpected end of compressed data at position {srcPos}. " +
                        $"Expected {uncompressedSize} bytes of output, got {dstPos}.");
                }

                byte flags = compressedData[srcPos++];

                // Process 8 chunks per flag byte
                for (int bit = 0; bit < BitsPerFlagByte && dstPos < uncompressedSize; bit++)
                {
                    if ((flags & (1 << bit)) != 0)
                    {
                        // Literal byte - copy directly to output and window
                        if (srcPos >= compressedData.Length)
                        {
                            throw new FormatException(
                                $"LZSS decompression failed: unexpected end of data reading literal at position {srcPos}. " +
                                $"Output position: {dstPos}/{uncompressedSize}.");
                        }

                        byte b = compressedData[srcPos++];
                        output[dstPos++] = b;
                        window[windowPos & WindowMask] = b;
                        windowPos++;
                    }
                    else
                    {
                        // Back-reference: read offset and length
                        if (srcPos + 1 >= compressedData.Length)
                        {
                            throw new FormatException(
                                $"LZSS decompression failed: unexpected end of data reading back-reference at position {srcPos}. " +
                                $"Output position: {dstPos}/{uncompressedSize}.");
                        }

                        // Read two bytes for offset/length pair
                        // Format: low byte = offset bits [0-7], high byte = offset bits [8-11] + length [0-3]
                        int lo = compressedData[srcPos++];
                        int hi = compressedData[srcPos++];

                        // Extract 12-bit offset (lo + high 4 bits of hi shifted)
                        int offset = lo | ((hi & 0xF0) << 4);

                        // Extract 4-bit length and add minimum match length
                        int length = (hi & 0x0F) + MinMatchLength;

                        // Copy bytes from window to output
                        for (int i = 0; i < length && dstPos < uncompressedSize; i++)
                        {
                            byte b = window[(offset + i) & WindowMask];
                            output[dstPos++] = b;
                            window[windowPos & WindowMask] = b;
                            windowPos++;
                        }
                    }
                }
            }

            return output;
        }

        /// <summary>
        /// Decompresses LZSS-compressed data, reading the uncompressed size from the data header.
        /// </summary>
        /// <param name="compressedDataWithHeader">
        /// The compressed data with a 4-byte little-endian uncompressed size header.
        /// </param>
        /// <returns>The decompressed data.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="compressedDataWithHeader"/> is null.
        /// </exception>
        /// <exception cref="FormatException">
        /// Thrown when the data is too short to contain a valid header or is malformed.
        /// </exception>
        public static byte[] DecompressWithHeader(byte[] compressedDataWithHeader)
        {
            if (compressedDataWithHeader == null)
            {
                throw new ArgumentNullException(nameof(compressedDataWithHeader));
            }

            if (compressedDataWithHeader.Length < 4)
            {
                throw new FormatException(
                    $"LZSS compressed data too short: expected at least 4 bytes for header, got {compressedDataWithHeader.Length}.");
            }

            // Read uncompressed size from first 4 bytes (little-endian)
            int uncompressedSize = BitConverter.ToInt32(compressedDataWithHeader, 0);

            if (uncompressedSize < 0)
            {
                throw new FormatException(
                    $"LZSS header contains invalid uncompressed size: {uncompressedSize}");
            }

            // Extract compressed data (skip 4-byte header)
            var compressedData = new byte[compressedDataWithHeader.Length - 4];
            Array.Copy(compressedDataWithHeader, 4, compressedData, 0, compressedData.Length);

            return Decompress(compressedData, uncompressedSize);
        }
    }
}
