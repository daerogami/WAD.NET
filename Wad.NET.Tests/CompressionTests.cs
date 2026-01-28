using System;
using Xunit;
using WAD.NET.Compression;

namespace WAD.NET.Tests
{
    public class CompressionTests
    {
        #region Decompress - Basic Decompression Tests

        [Fact]
        public void Decompress_AllLiterals_ShouldDecompressCorrectly()
        {
            // Flag byte 0xFF means all 8 bits are 1, so all 8 chunks are literals
            byte[] compressed = { 0xFF, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48 };
            // Expected output: "ABCDEFGH"
            byte[] expected = { 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48 };

            var result = LzssDecompressor.Decompress(compressed, expected.Length);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void Decompress_MultipleLiteralGroups_ShouldDecompressCorrectly()
        {
            // Two groups of 8 literals each
            byte[] compressed = {
                0xFF, 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x2C, 0x20, 0x57, // "Hello, W"
                0xFF, 0x6F, 0x72, 0x6C, 0x64, 0x21, 0x00, 0x00, 0x00  // "orld!\0\0\0"
            };
            byte[] expected = {
                0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x2C, 0x20, 0x57,
                0x6F, 0x72, 0x6C, 0x64, 0x21, 0x00, 0x00, 0x00
            };

            var result = LzssDecompressor.Decompress(compressed, expected.Length);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void Decompress_PartialLiteralGroup_ShouldDecompressCorrectly()
        {
            // Flag 0x0F = 0b00001111 means first 4 bits are literals, last 4 are back-references
            // But we only need 4 bytes output, so only first 4 literals are read
            byte[] compressed = { 0x0F, 0x41, 0x42, 0x43, 0x44 };
            byte[] expected = { 0x41, 0x42, 0x43, 0x44 };

            var result = LzssDecompressor.Decompress(compressed, expected.Length);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void Decompress_WithBackReference_ShouldDecompressCorrectly()
        {
            // Test: "ABCDABCD" where second "ABCD" is a back-reference
            // First we output 4 literals "ABCD", then reference offset to copy them
            // Window is initialized with spaces (0x20) at position InitialWindowPosition = 4078
            // After 4 literals, window position is 4082
            // We need a back-reference to offset 4078 with length 4 (min length 3, so encoded length = 1)

            // Flag byte layout: bits 0-3 are literals (1), bits 4-7 are back-references (0)
            // 0x0F = 0b00001111
            // Literal bytes: A, B, C, D
            // Back-reference: offset 4078 = 0xFEE (12 bits), length = 4 (encoded as 4-3=1)
            // lo byte = 0xEE, hi byte = 0xF1 (offset bits 8-11 = 0xF, length bits 0-3 = 0x1)

            byte[] compressed = {
                0x0F,       // Flag: first 4 are literals, next 4 are back-refs (only need 1)
                0x41, 0x42, 0x43, 0x44,  // Literals: A, B, C, D
                0xEE, 0xF1  // Back-reference: offset=4078, length=4
            };

            var result = LzssDecompressor.Decompress(compressed, 8);

            Assert.Equal(8, result.Length);
            Assert.Equal((byte)'A', result[0]);
            Assert.Equal((byte)'B', result[1]);
            Assert.Equal((byte)'C', result[2]);
            Assert.Equal((byte)'D', result[3]);
            Assert.Equal((byte)'A', result[4]);
            Assert.Equal((byte)'B', result[5]);
            Assert.Equal((byte)'C', result[6]);
            Assert.Equal((byte)'D', result[7]);
        }

        [Fact]
        public void Decompress_MixedLiteralsAndReferences_ShouldDecompressCorrectly()
        {
            // Create a pattern: literal A, back-ref (gets space from window), literal B
            // Flag: bit 0 = 1 (literal), bit 1 = 0 (back-ref), bit 2 = 1 (literal)
            // 0b00000101 = 0x05

            // Back-reference offset 0, length 3 (minimum)
            // lo = 0x00, hi = 0x00 (offset 0, length 3-3=0)

            byte[] compressed = {
                0x05,       // Flag: bits 0,2 = literal, bit 1 = back-ref
                0x41,       // Literal: A
                0x00, 0x00, // Back-reference: offset=0, length=3 (copies 3 spaces from window[0])
                0x42        // Literal: B
            };

            var result = LzssDecompressor.Decompress(compressed, 5);

            Assert.Equal(5, result.Length);
            Assert.Equal((byte)'A', result[0]);
            // Back-reference copies from window[0..2], which are spaces (0x20)
            Assert.Equal(0x20, result[1]);
            Assert.Equal(0x20, result[2]);
            Assert.Equal(0x20, result[3]);
            Assert.Equal((byte)'B', result[4]);
        }

        [Fact]
        public void Decompress_BackReferenceToRecentData_ShouldDecompressCorrectly()
        {
            // Compress "AAAA" - output A, then back-reference to copy it 3 times
            // Window starts at position 4078
            // After first literal A at position 4078, we can reference it

            // Flag: bit 0 = literal, bit 1 = back-ref
            // 0x01 = 0b00000001

            // Back-reference: offset 4078, length 3 (copies the A three times)
            // offset 4078 = 0xFEE
            // lo = 0xEE, hi = 0xF0 (offset bits 8-11 = 0xF = 15, length = 0)

            byte[] compressed = {
                0x01,       // Flag: bit 0 = literal, bit 1 = back-ref
                0x41,       // Literal: A
                0xEE, 0xF0  // Back-reference: offset=4078, length=3
            };

            var result = LzssDecompressor.Decompress(compressed, 4);

            Assert.Equal(4, result.Length);
            Assert.All(result, b => Assert.Equal((byte)'A', b));
        }

        #endregion

        #region DecompressWithHeader Tests

        [Fact]
        public void DecompressWithHeader_ShouldReadSizeAndDecompress()
        {
            // Header: 8 bytes uncompressed size (little-endian)
            // Data: all literals "ABCDEFGH"
            byte[] compressedWithHeader = {
                0x08, 0x00, 0x00, 0x00, // Uncompressed size = 8
                0xFF,                    // Flag: all literals
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48
            };

            var result = LzssDecompressor.DecompressWithHeader(compressedWithHeader);

            Assert.Equal(8, result.Length);
            Assert.Equal("ABCDEFGH", System.Text.Encoding.ASCII.GetString(result));
        }

        [Fact]
        public void DecompressWithHeader_LargerSize_ShouldDecompressCorrectly()
        {
            // Test with 16 bytes uncompressed
            byte[] compressedWithHeader = {
                0x10, 0x00, 0x00, 0x00,  // Uncompressed size = 16
                0xFF, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
                0xFF, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F
            };

            var result = LzssDecompressor.DecompressWithHeader(compressedWithHeader);

            Assert.Equal(16, result.Length);
            for (int i = 0; i < 16; i++)
            {
                Assert.Equal((byte)i, result[i]);
            }
        }

        [Fact]
        public void DecompressWithHeader_ZeroSize_ShouldReturnEmpty()
        {
            byte[] compressedWithHeader = { 0x00, 0x00, 0x00, 0x00 };

            var result = LzssDecompressor.DecompressWithHeader(compressedWithHeader);

            Assert.Empty(result);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void Decompress_ZeroUncompressedSize_ShouldReturnEmpty()
        {
            byte[] compressed = { 0xFF, 0x41, 0x42 }; // Has data but we request 0 bytes

            var result = LzssDecompressor.Decompress(compressed, 0);

            Assert.Empty(result);
        }

        [Fact]
        public void Decompress_SingleByte_ShouldDecompressCorrectly()
        {
            // Flag 0x01 = bit 0 is literal
            byte[] compressed = { 0x01, 0x42 };

            var result = LzssDecompressor.Decompress(compressed, 1);

            Assert.Single(result);
            Assert.Equal((byte)'B', result[0]);
        }

        [Fact]
        public void Decompress_EmptyCompressedData_WithZeroSize_ShouldReturnEmpty()
        {
            byte[] compressed = Array.Empty<byte>();

            var result = LzssDecompressor.Decompress(compressed, 0);

            Assert.Empty(result);
        }

        [Fact]
        public void Decompress_LargeOutput_ShouldFillSlidingWindow()
        {
            // Create compressed data that will fill and wrap the sliding window
            // Window size is 4096, so we need more than that in output
            const int outputSize = 5000;

            // Build compressed data with all literals (simple but large)
            // Each group: 1 flag byte + 8 literal bytes = 9 bytes -> 8 output bytes
            int groups = (outputSize + 7) / 8;
            byte[] compressed = new byte[groups * 9];

            for (int g = 0; g < groups; g++)
            {
                compressed[g * 9] = 0xFF; // All literals flag
                for (int i = 0; i < 8; i++)
                {
                    compressed[g * 9 + 1 + i] = (byte)((g * 8 + i) % 256);
                }
            }

            var result = LzssDecompressor.Decompress(compressed, outputSize);

            Assert.Equal(outputSize, result.Length);
            // Verify data integrity
            for (int i = 0; i < outputSize; i++)
            {
                Assert.Equal((byte)(i % 256), result[i]);
            }
        }

        [Fact]
        public void Decompress_MaxBackReferenceLength_ShouldWorkCorrectly()
        {
            // Maximum length is 15 + 3 = 18 bytes
            // First output one literal, then back-reference to window with max length

            // Flag: bit 0 = literal, bit 1 = back-ref
            // Back-reference: offset 0, length 18 (encoded as 15 in low nibble of hi byte)
            // lo = 0x00, hi = 0x0F

            byte[] compressed = {
                0x01,       // Flag
                0x58,       // Literal: 'X'
                0x00, 0x0F  // Back-reference: offset=0, length=18 (copies spaces)
            };

            var result = LzssDecompressor.Decompress(compressed, 19);

            Assert.Equal(19, result.Length);
            Assert.Equal((byte)'X', result[0]);
            // Remaining 18 bytes should be spaces (0x20) from the initialized window
            for (int i = 1; i < 19; i++)
            {
                Assert.Equal(0x20, result[i]);
            }
        }

        #endregion

        #region Error Handling - Decompress

        [Fact]
        public void Decompress_NullInput_ShouldThrowArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => LzssDecompressor.Decompress(null!, 100));

            Assert.Equal("compressedData", ex.ParamName);
        }

        [Fact]
        public void Decompress_NegativeUncompressedSize_ShouldThrowArgumentOutOfRangeException()
        {
            byte[] compressed = { 0xFF, 0x00 };

            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => LzssDecompressor.Decompress(compressed, -1));

            Assert.Equal("uncompressedSize", ex.ParamName);
            Assert.Equal(-1, ex.ActualValue);
        }

        [Fact]
        public void Decompress_TruncatedData_NoFlagByte_ShouldThrowFormatException()
        {
            byte[] compressed = Array.Empty<byte>();

            var ex = Assert.Throws<FormatException>(
                () => LzssDecompressor.Decompress(compressed, 10));

            Assert.Contains("unexpected end of compressed data", ex.Message);
        }

        [Fact]
        public void Decompress_TruncatedData_MissingLiteral_ShouldThrowFormatException()
        {
            // Flag says first chunk is literal, but no literal byte follows
            byte[] compressed = { 0x01 }; // Flag only, no literal byte

            var ex = Assert.Throws<FormatException>(
                () => LzssDecompressor.Decompress(compressed, 1));

            Assert.Contains("unexpected end of data reading literal", ex.Message);
        }

        [Fact]
        public void Decompress_TruncatedData_MissingBackReference_ShouldThrowFormatException()
        {
            // Flag says first chunk is back-reference, but only 1 byte follows (need 2)
            byte[] compressed = { 0x00, 0x00 }; // Flag + only 1 byte of back-ref

            var ex = Assert.Throws<FormatException>(
                () => LzssDecompressor.Decompress(compressed, 3));

            Assert.Contains("unexpected end of data reading back-reference", ex.Message);
        }

        [Fact]
        public void Decompress_TruncatedData_PartialBackReference_ShouldThrowFormatException()
        {
            // Back-reference needs 2 bytes, only have 1
            byte[] compressed = { 0x00, 0xAB }; // Flag + only first byte of back-ref pair

            var ex = Assert.Throws<FormatException>(
                () => LzssDecompressor.Decompress(compressed, 10));

            Assert.Contains("unexpected end of data reading back-reference", ex.Message);
        }

        [Fact]
        public void Decompress_InsufficientDataForRequestedOutput_ShouldThrowFormatException()
        {
            // Only 1 literal provided, but requesting 10 bytes of output
            byte[] compressed = { 0x01, 0x41 };

            var ex = Assert.Throws<FormatException>(
                () => LzssDecompressor.Decompress(compressed, 10));

            Assert.Contains("unexpected end", ex.Message);
        }

        #endregion

        #region Error Handling - DecompressWithHeader

        [Fact]
        public void DecompressWithHeader_NullInput_ShouldThrowArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => LzssDecompressor.DecompressWithHeader(null!));

            Assert.Equal("compressedDataWithHeader", ex.ParamName);
        }

        [Fact]
        public void DecompressWithHeader_TooShort_ShouldThrowFormatException()
        {
            byte[] tooShort = { 0x01, 0x02, 0x03 }; // Only 3 bytes, need at least 4

            var ex = Assert.Throws<FormatException>(
                () => LzssDecompressor.DecompressWithHeader(tooShort));

            Assert.Contains("too short", ex.Message);
            Assert.Contains("4 bytes", ex.Message);
        }

        [Fact]
        public void DecompressWithHeader_EmptyInput_ShouldThrowFormatException()
        {
            byte[] empty = Array.Empty<byte>();

            var ex = Assert.Throws<FormatException>(
                () => LzssDecompressor.DecompressWithHeader(empty));

            Assert.Contains("too short", ex.Message);
        }

        [Fact]
        public void DecompressWithHeader_NegativeSizeInHeader_ShouldThrowFormatException()
        {
            // -1 in little-endian = 0xFF 0xFF 0xFF 0xFF
            byte[] invalidHeader = { 0xFF, 0xFF, 0xFF, 0xFF };

            var ex = Assert.Throws<FormatException>(
                () => LzssDecompressor.DecompressWithHeader(invalidHeader));

            Assert.Contains("invalid uncompressed size", ex.Message);
        }

        [Fact]
        public void DecompressWithHeader_TruncatedCompressedData_ShouldThrowFormatException()
        {
            // Header says 100 bytes, but no compressed data follows
            byte[] truncated = { 0x64, 0x00, 0x00, 0x00 }; // Size = 100

            var ex = Assert.Throws<FormatException>(
                () => LzssDecompressor.DecompressWithHeader(truncated));

            Assert.Contains("unexpected end", ex.Message);
        }

        #endregion

        #region Constants Verification

        [Fact]
        public void WindowSize_ShouldBe4096()
        {
            Assert.Equal(4096, LzssDecompressor.WindowSize);
        }

        #endregion

        #region Realistic Compression Scenarios

        [Fact]
        public void Decompress_RepeatingPattern_ShouldDecompressCorrectly()
        {
            // Simulate a repeating pattern like "ABAB" where we can use back-references
            // Output: "ABABAB" (6 bytes)
            // Literal: A, B, then back-reference to copy "ABAB" (4 bytes)

            // Window position starts at 4078
            // After A (4078) and B (4079), we reference offset 4078 with length 4

            // Flag: 0x03 = 0b00000011 (bits 0,1 are literals, bit 2 is back-ref)
            byte[] compressed = {
                0x03,       // Flag
                0x41,       // A
                0x42,       // B
                0xEE, 0xF1  // Back-ref: offset=4078, length=4
            };

            var result = LzssDecompressor.Decompress(compressed, 6);

            Assert.Equal(6, result.Length);
            Assert.Equal("ABABAB", System.Text.Encoding.ASCII.GetString(result));
        }

        [Fact]
        public void Decompress_RunLengthLikePattern_ShouldDecompressCorrectly()
        {
            // Output "XXXXXXXX" (8 X's) using one literal and back-references
            // Literal X, then back-ref to self for remaining 7

            // First literal X at window pos 4078
            // Back-reference offset 4078, length 7 (encoded as 4)

            // Flag: 0x01 = bit 0 literal, bit 1 back-ref
            byte[] compressed = {
                0x01,       // Flag
                0x58,       // X
                0xEE, 0xF4  // Back-ref: offset=4078, length=7 (4+3)
            };

            var result = LzssDecompressor.Decompress(compressed, 8);

            Assert.Equal(8, result.Length);
            Assert.All(result, b => Assert.Equal((byte)'X', b));
        }

        [Fact]
        public void Decompress_OverlappingBackReference_ShouldDecompressCorrectly()
        {
            // Test overlapping copy: output "ABCABCABC" (9 bytes)
            // Output ABC as literals, then back-reference to copy ABC twice (6 bytes)

            // Window position starts at 4078
            // After A,B,C at positions 4078-4080
            // Back-reference offset 4078, length 6

            // Flag: 0x07 = bits 0,1,2 literals, bit 3 back-ref
            byte[] compressed = {
                0x07,             // Flag
                0x41, 0x42, 0x43, // ABC
                0xEE, 0xF3        // Back-ref: offset=4078, length=6 (3+3)
            };

            var result = LzssDecompressor.Decompress(compressed, 9);

            Assert.Equal(9, result.Length);
            Assert.Equal("ABCABCABC", System.Text.Encoding.ASCII.GetString(result));
        }

        #endregion
    }
}
