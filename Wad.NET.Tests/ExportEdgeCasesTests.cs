using System;
using System.IO;
using Xunit;
using WAD.NET.Concrete;
using WAD.NET.Definitions;
using WAD.NET.Export;

namespace WAD.NET.Tests
{
    /// <summary>
    /// Tests for export edge cases: PNG, TGA, BMP export functionality,
    /// file system writes, error handling, invalid palette handling, and corrupt image data.
    /// </summary>
    public class ExportEdgeCasesTests
    {
        #region PNG Export Edge Cases

        [Fact]
        public void ExportPng_MinimalDimensions_ShouldSucceed()
        {
            // 1x1 picture - minimal valid image
            var pixels = new byte[] { 100 };
            var picture = new DoomPicture(1, 1, 0, 0, pixels);

            var paletteData = CreateValidPaletteData();
            paletteData[100 * 3 + 0] = 128;
            paletteData[100 * 3 + 1] = 64;
            paletteData[100 * 3 + 2] = 32;
            var palette = new Palette(paletteData);

            using var output = new MemoryStream();
            ImageExporter.ExportPng(picture, palette, output);

            var pngData = output.ToArray();
            Assert.True(pngData.Length > 8);
            AssertValidPngHeader(pngData);
        }

        [Fact]
        public void ExportPng_LargeDimensions_ShouldSucceed()
        {
            // 256x256 picture
            var pixels = new byte[256 * 256];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = (byte)(i % 256);

            var picture = new DoomPicture(256, 256, 0, 0, pixels);
            var palette = new Palette(CreateValidPaletteData());

            using var output = new MemoryStream();
            ImageExporter.ExportPng(picture, palette, output);

            var pngData = output.ToArray();
            AssertValidPngHeader(pngData);
        }

        [Fact]
        public void ExportPng_NonSquareDimensions_ShouldSucceed()
        {
            // 32x8 non-square picture
            var pixels = new byte[32 * 8];
            var picture = new DoomPicture(32, 8, 0, 0, pixels);
            var palette = new Palette(CreateValidPaletteData());

            using var output = new MemoryStream();
            ImageExporter.ExportPng(picture, palette, output);

            var pngData = output.ToArray();
            AssertValidPngHeader(pngData);
        }

        [Fact]
        public void ExportPng_AllTransparentPixels_ShouldSucceed()
        {
            var pixels = new byte[4];
            Array.Fill(pixels, DoomPicture.TransparentIndex);
            var picture = new DoomPicture(2, 2, 0, 0, pixels);
            var palette = new Palette(CreateValidPaletteData());

            using var output = new MemoryStream();
            ImageExporter.ExportPng(picture, palette, output);

            var pngData = output.ToArray();
            AssertValidPngHeader(pngData);
        }

        [Fact]
        public void ExportPng_AllOpaqueSameColor_ShouldSucceed()
        {
            var pixels = new byte[16];
            Array.Fill(pixels, (byte)42); // All same palette index
            var picture = new DoomPicture(4, 4, 0, 0, pixels);
            var palette = new Palette(CreateValidPaletteData());

            using var output = new MemoryStream();
            ImageExporter.ExportPng(picture, palette, output);

            var pngData = output.ToArray();
            AssertValidPngHeader(pngData);
        }

        [Fact]
        public void ExportPng_CheckerboardPattern_ShouldSucceed()
        {
            var pixels = new byte[64];
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    pixels[y * 8 + x] = ((x + y) % 2 == 0) ? (byte)0 : DoomPicture.TransparentIndex;
                }
            }
            var picture = new DoomPicture(8, 8, 0, 0, pixels);
            var palette = new Palette(CreateValidPaletteData());

            using var output = new MemoryStream();
            ImageExporter.ExportPng(picture, palette, output);

            var pngData = output.ToArray();
            AssertValidPngHeader(pngData);
        }

        [Fact]
        public void ExportPng_WithNegativeOffsets_ShouldSucceed()
        {
            // Offsets don't affect PNG export, but ensure they don't cause issues
            var pixels = new byte[4];
            var picture = new DoomPicture(2, 2, -100, -200, pixels);
            var palette = new Palette(CreateValidPaletteData());

            using var output = new MemoryStream();
            ImageExporter.ExportPng(picture, palette, output);

            var pngData = output.ToArray();
            AssertValidPngHeader(pngData);
        }

        [Fact]
        public void ExportPng_FlatLump_ValidSize_ShouldSucceed()
        {
            var flatData = new byte[FlatLump.Size];
            for (int i = 0; i < flatData.Length; i++)
                flatData[i] = (byte)(i % 256);

            var flat = new FlatLump("TESTFLAT", "TEST.WAD", flatData);
            var palette = new Palette(CreateValidPaletteData());

            using var output = new MemoryStream();
            ImageExporter.ExportPng(flat, palette, output);

            var pngData = output.ToArray();
            AssertValidPngHeader(pngData);
        }

        [Fact]
        public void ExportPng_RawRgba_MaxPaletteValues_ShouldSucceed()
        {
            // Use all 256 palette colors
            var rgba = new byte[256 * 4];
            for (int i = 0; i < 256; i++)
            {
                rgba[i * 4 + 0] = (byte)i;       // R
                rgba[i * 4 + 1] = (byte)(255 - i); // G
                rgba[i * 4 + 2] = (byte)(i / 2);  // B
                rgba[i * 4 + 3] = 255;            // A
            }

            using var output = new MemoryStream();
            ImageExporter.ExportPng(16, 16, rgba, output);

            var pngData = output.ToArray();
            AssertValidPngHeader(pngData);
        }

        #endregion

        #region TGA Export Edge Cases

        [Fact]
        public void ExportTga_MinimalDimensions_ShouldSucceed()
        {
            var pixels = new byte[] { 100 };
            var picture = new DoomPicture(1, 1, 0, 0, pixels);
            var palette = new Palette(CreateValidPaletteData());

            using var output = new MemoryStream();
            ImageExporter.ExportTga(picture, palette, output);

            var tgaData = output.ToArray();
            Assert.Equal(18 + 4, tgaData.Length); // Header + 1 BGRA pixel
            AssertValidTgaHeader(tgaData, 1, 1);
        }

        [Fact]
        public void ExportTga_LargeDimensions_ShouldSucceed()
        {
            var pixels = new byte[128 * 128];
            var picture = new DoomPicture(128, 128, 0, 0, pixels);
            var palette = new Palette(CreateValidPaletteData());

            using var output = new MemoryStream();
            ImageExporter.ExportTga(picture, palette, output);

            var tgaData = output.ToArray();
            AssertValidTgaHeader(tgaData, 128, 128);
        }

        [Fact]
        public void ExportTga_TransparentPixels_ShouldHaveZeroAlpha()
        {
            var pixels = new byte[] { DoomPicture.TransparentIndex, 0, DoomPicture.TransparentIndex, 0 };
            var picture = new DoomPicture(2, 2, 0, 0, pixels);
            var palette = new Palette(CreateValidPaletteData());

            using var output = new MemoryStream();
            ImageExporter.ExportTga(picture, palette, output);

            var tgaData = output.ToArray();
            // Check first pixel (transparent) - BGRA at offset 18
            Assert.Equal(0, tgaData[18 + 3]); // Alpha should be 0
            // Check second pixel (opaque) - should have alpha 255
            Assert.Equal(255, tgaData[22 + 3]); // Alpha should be 255
        }

        [Fact]
        public void ExportTga_RawRgba_InvalidLength_ShouldThrow()
        {
            var rgba = new byte[10]; // Invalid: not width * height * 4

            using var output = new MemoryStream();
            Assert.Throws<ArgumentException>(() => ImageExporter.ExportTga(2, 2, rgba, output));
        }

        [Fact]
        public void ExportTga_FlatLump_ShouldSucceed()
        {
            var flatData = new byte[FlatLump.Size];
            var flat = new FlatLump("FLOOR1", "TEST.WAD", flatData);
            var palette = new Palette(CreateValidPaletteData());

            using var output = new MemoryStream();
            ImageExporter.ExportTga(flat, palette, output);

            var tgaData = output.ToArray();
            AssertValidTgaHeader(tgaData, 64, 64);
        }

        #endregion

        #region BMP Export Edge Cases

        [Fact]
        public void ExportBmp_MinimalDimensions_ShouldSucceed()
        {
            var pixels = new byte[] { 100 };
            var picture = new DoomPicture(1, 1, 0, 0, pixels);
            var palette = new Palette(CreateValidPaletteData());

            using var output = new MemoryStream();
            ImageExporter.ExportBmp(picture, palette, output);

            var bmpData = output.ToArray();
            AssertValidBmpHeader(bmpData);
        }

        [Fact]
        public void ExportBmp_TransparentPixels_ShouldBeMagenta()
        {
            var pixels = new byte[] { DoomPicture.TransparentIndex };
            var picture = new DoomPicture(1, 1, 0, 0, pixels);
            var palette = new Palette(CreateValidPaletteData());

            using var output = new MemoryStream();
            ImageExporter.ExportBmp(picture, palette, output);

            var bmpData = output.ToArray();
            // BMP pixel data starts at offset 54
            // BGR format, with row padding
            // Transparent should be magenta (255, 0, 255)
            Assert.Equal(255, bmpData[54]); // B
            Assert.Equal(0, bmpData[55]);   // G
            Assert.Equal(255, bmpData[56]); // R
        }

        [Fact]
        public void ExportBmp_RowPadding_ShouldBeCorrect()
        {
            // BMP rows must be aligned to 4 bytes
            // 3-pixel wide image: 3 * 3 = 9 bytes, needs 3 bytes padding to reach 12
            var pixels = new byte[3 * 2]; // 3x2 image
            var picture = new DoomPicture(3, 2, 0, 0, pixels);
            var palette = new Palette(CreateValidPaletteData());

            using var output = new MemoryStream();
            ImageExporter.ExportBmp(picture, palette, output);

            var bmpData = output.ToArray();
            AssertValidBmpHeader(bmpData);
            // Row stride should be (3 * 3 + 3) & ~3 = 12
            int expectedSize = 54 + 12 * 2; // Header + 2 rows of 12 bytes each
            Assert.Equal(expectedSize, bmpData.Length);
        }

        [Fact]
        public void ExportBmp_FlatLump_ShouldSucceed()
        {
            var flatData = new byte[FlatLump.Size];
            var flat = new FlatLump("FLOOR1", "TEST.WAD", flatData);
            var palette = new Palette(CreateValidPaletteData());

            using var output = new MemoryStream();
            ImageExporter.ExportBmp(flat, palette, output);

            var bmpData = output.ToArray();
            AssertValidBmpHeader(bmpData);
        }

        [Fact]
        public void ExportBmp_RawRgba_InvalidLength_ShouldThrow()
        {
            var rgba = new byte[10]; // Invalid size

            using var output = new MemoryStream();
            Assert.Throws<ArgumentException>(() => ImageExporter.ExportBmp(2, 2, rgba, output));
        }

        #endregion

        #region File System Write Tests

        [Fact]
        public void ExportPng_ToTempFile_ShouldSucceed()
        {
            var pixels = new byte[4];
            var picture = new DoomPicture(2, 2, 0, 0, pixels);
            var palette = new Palette(CreateValidPaletteData());

            var tempPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.png");
            try
            {
                ImageExporter.ExportPng(picture, palette, tempPath);

                Assert.True(File.Exists(tempPath));
                var fileData = File.ReadAllBytes(tempPath);
                AssertValidPngHeader(fileData);
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }

        [Fact]
        public void ExportTga_ToTempFile_ShouldSucceed()
        {
            var pixels = new byte[4];
            var picture = new DoomPicture(2, 2, 0, 0, pixels);
            var palette = new Palette(CreateValidPaletteData());

            var tempPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.tga");
            try
            {
                ImageExporter.ExportTga(picture, palette, tempPath);

                Assert.True(File.Exists(tempPath));
                var fileData = File.ReadAllBytes(tempPath);
                AssertValidTgaHeader(fileData, 2, 2);
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }

        [Fact]
        public void ExportBmp_ToTempFile_ShouldSucceed()
        {
            var pixels = new byte[4];
            var picture = new DoomPicture(2, 2, 0, 0, pixels);
            var palette = new Palette(CreateValidPaletteData());

            var tempPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.bmp");
            try
            {
                ImageExporter.ExportBmp(picture, palette, tempPath);

                Assert.True(File.Exists(tempPath));
                var fileData = File.ReadAllBytes(tempPath);
                AssertValidBmpHeader(fileData);
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }

        [Fact]
        public void ExportPng_FlatToTempFile_ShouldSucceed()
        {
            var flatData = new byte[FlatLump.Size];
            var flat = new FlatLump("FLOOR1", "TEST.WAD", flatData);
            var palette = new Palette(CreateValidPaletteData());

            var tempPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.png");
            try
            {
                ImageExporter.ExportPng(flat, palette, tempPath);

                Assert.True(File.Exists(tempPath));
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }

        [Fact]
        public void ExportPng_RawRgbaToTempFile_ShouldSucceed()
        {
            var rgba = new byte[16]; // 2x2 RGBA
            var tempPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.png");
            try
            {
                ImageExporter.ExportPng(2, 2, rgba, tempPath);

                Assert.True(File.Exists(tempPath));
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }

        #endregion

        #region Error Handling During Export

        [Fact]
        public void ExportPng_NullPicture_ShouldThrow()
        {
            var palette = new Palette(CreateValidPaletteData());
            using var output = new MemoryStream();

            Assert.Throws<ArgumentNullException>(() =>
                ImageExporter.ExportPng((DoomPicture)null!, palette, output));
        }

        [Fact]
        public void ExportPng_NullPalette_ShouldThrow()
        {
            var picture = new DoomPicture(2, 2, 0, 0, new byte[4]);
            using var output = new MemoryStream();

            Assert.Throws<ArgumentNullException>(() =>
                ImageExporter.ExportPng(picture, null!, output));
        }

        [Fact]
        public void ExportPng_NullStream_ShouldThrow()
        {
            var picture = new DoomPicture(2, 2, 0, 0, new byte[4]);
            var palette = new Palette(CreateValidPaletteData());

            Assert.Throws<ArgumentNullException>(() =>
                ImageExporter.ExportPng(picture, palette, (Stream)null!));
        }

        [Fact]
        public void ExportTga_NullPicture_ShouldThrow()
        {
            var palette = new Palette(CreateValidPaletteData());
            using var output = new MemoryStream();

            Assert.Throws<ArgumentNullException>(() =>
                ImageExporter.ExportTga((DoomPicture)null!, palette, output));
        }

        [Fact]
        public void ExportTga_NullPalette_ShouldThrow()
        {
            var picture = new DoomPicture(2, 2, 0, 0, new byte[4]);
            using var output = new MemoryStream();

            Assert.Throws<ArgumentNullException>(() =>
                ImageExporter.ExportTga(picture, null!, output));
        }

        [Fact]
        public void ExportTga_NullStream_ShouldThrow()
        {
            var picture = new DoomPicture(2, 2, 0, 0, new byte[4]);
            var palette = new Palette(CreateValidPaletteData());

            Assert.Throws<ArgumentNullException>(() =>
                ImageExporter.ExportTga(picture, palette, (Stream)null!));
        }

        [Fact]
        public void ExportBmp_NullPicture_ShouldThrow()
        {
            var palette = new Palette(CreateValidPaletteData());
            using var output = new MemoryStream();

            Assert.Throws<ArgumentNullException>(() =>
                ImageExporter.ExportBmp((DoomPicture)null!, palette, output));
        }

        [Fact]
        public void ExportBmp_NullPalette_ShouldThrow()
        {
            var picture = new DoomPicture(2, 2, 0, 0, new byte[4]);
            using var output = new MemoryStream();

            Assert.Throws<ArgumentNullException>(() =>
                ImageExporter.ExportBmp(picture, null!, output));
        }

        [Fact]
        public void ExportBmp_NullStream_ShouldThrow()
        {
            var picture = new DoomPicture(2, 2, 0, 0, new byte[4]);
            var palette = new Palette(CreateValidPaletteData());

            Assert.Throws<ArgumentNullException>(() =>
                ImageExporter.ExportBmp(picture, palette, (Stream)null!));
        }

        [Fact]
        public void ExportPng_NullFlatLump_ShouldThrow()
        {
            var palette = new Palette(CreateValidPaletteData());
            using var output = new MemoryStream();

            Assert.Throws<ArgumentNullException>(() =>
                ImageExporter.ExportPng((FlatLump)null!, palette, output));
        }

        [Fact]
        public void ExportTga_NullFlatLump_ShouldThrow()
        {
            var palette = new Palette(CreateValidPaletteData());
            using var output = new MemoryStream();

            Assert.Throws<ArgumentNullException>(() =>
                ImageExporter.ExportTga((FlatLump)null!, palette, output));
        }

        [Fact]
        public void ExportBmp_NullFlatLump_ShouldThrow()
        {
            var palette = new Palette(CreateValidPaletteData());
            using var output = new MemoryStream();

            Assert.Throws<ArgumentNullException>(() =>
                ImageExporter.ExportBmp((FlatLump)null!, palette, output));
        }

        [Fact]
        public void ExportPng_NullRgbaArray_ShouldThrow()
        {
            using var output = new MemoryStream();

            Assert.Throws<ArgumentNullException>(() =>
                ImageExporter.ExportPng(2, 2, null!, output));
        }

        [Fact]
        public void ExportTga_NullRgbaArray_ShouldThrow()
        {
            using var output = new MemoryStream();

            Assert.Throws<ArgumentNullException>(() =>
                ImageExporter.ExportTga(2, 2, null!, output));
        }

        [Fact]
        public void ExportBmp_NullRgbaArray_ShouldThrow()
        {
            using var output = new MemoryStream();

            Assert.Throws<ArgumentNullException>(() =>
                ImageExporter.ExportBmp(2, 2, null!, output));
        }

        #endregion

        #region Palette Edge Cases

        [Fact]
        public void ExportPng_AllBlackPalette_ShouldSucceed()
        {
            var pixels = new byte[4] { 0, 1, 2, 3 };
            var picture = new DoomPicture(2, 2, 0, 0, pixels);

            // All-black palette
            var paletteData = new byte[Palette.ByteSize];
            var palette = new Palette(paletteData);

            using var output = new MemoryStream();
            ImageExporter.ExportPng(picture, palette, output);

            var pngData = output.ToArray();
            AssertValidPngHeader(pngData);
        }

        [Fact]
        public void ExportPng_AllWhitePalette_ShouldSucceed()
        {
            var pixels = new byte[4] { 0, 1, 2, 3 };
            var picture = new DoomPicture(2, 2, 0, 0, pixels);

            // All-white palette
            var paletteData = new byte[Palette.ByteSize];
            for (int i = 0; i < paletteData.Length; i++)
                paletteData[i] = 255;
            var palette = new Palette(paletteData);

            using var output = new MemoryStream();
            ImageExporter.ExportPng(picture, palette, output);

            var pngData = output.ToArray();
            AssertValidPngHeader(pngData);
        }

        [Fact]
        public void ExportPng_GrayscalePalette_ShouldSucceed()
        {
            var pixels = new byte[256];
            for (int i = 0; i < 256; i++)
                pixels[i] = (byte)i;
            var picture = new DoomPicture(16, 16, 0, 0, pixels);

            // Grayscale palette
            var paletteData = new byte[Palette.ByteSize];
            for (int i = 0; i < 256; i++)
            {
                paletteData[i * 3 + 0] = (byte)i;
                paletteData[i * 3 + 1] = (byte)i;
                paletteData[i * 3 + 2] = (byte)i;
            }
            var palette = new Palette(paletteData);

            using var output = new MemoryStream();
            ImageExporter.ExportPng(picture, palette, output);

            var pngData = output.ToArray();
            AssertValidPngHeader(pngData);
        }

        [Fact]
        public void ExportPng_RandomPalette_ShouldSucceed()
        {
            var pixels = new byte[256];
            var picture = new DoomPicture(16, 16, 0, 0, pixels);

            // Random-ish palette using deterministic values
            var paletteData = new byte[Palette.ByteSize];
            for (int i = 0; i < 256; i++)
            {
                paletteData[i * 3 + 0] = (byte)((i * 7) % 256);
                paletteData[i * 3 + 1] = (byte)((i * 13) % 256);
                paletteData[i * 3 + 2] = (byte)((i * 23) % 256);
            }
            var palette = new Palette(paletteData);

            using var output = new MemoryStream();
            ImageExporter.ExportPng(picture, palette, output);

            var pngData = output.ToArray();
            AssertValidPngHeader(pngData);
        }

        #endregion

        #region Helper Methods

        private static byte[] CreateValidPaletteData()
        {
            var data = new byte[Palette.ByteSize];
            // Create a simple gradient palette
            for (int i = 0; i < 256; i++)
            {
                data[i * 3 + 0] = (byte)i;       // R
                data[i * 3 + 1] = (byte)(i / 2); // G
                data[i * 3 + 2] = (byte)(i / 4); // B
            }
            return data;
        }

        private static void AssertValidPngHeader(byte[] data)
        {
            Assert.True(data.Length >= 8, "PNG data too short");
            Assert.Equal(0x89, data[0]);
            Assert.Equal((byte)'P', data[1]);
            Assert.Equal((byte)'N', data[2]);
            Assert.Equal((byte)'G', data[3]);
            Assert.Equal(0x0D, data[4]);
            Assert.Equal(0x0A, data[5]);
            Assert.Equal(0x1A, data[6]);
            Assert.Equal(0x0A, data[7]);
        }

        private static void AssertValidTgaHeader(byte[] data, int expectedWidth, int expectedHeight)
        {
            Assert.True(data.Length >= 18, "TGA data too short");
            Assert.Equal(0, data[0]);   // ID length
            Assert.Equal(0, data[1]);   // Color map type
            Assert.Equal(2, data[2]);   // Image type (uncompressed true-color)
            Assert.Equal(expectedWidth, BitConverter.ToInt16(data, 12));
            Assert.Equal(expectedHeight, BitConverter.ToInt16(data, 14));
            Assert.Equal(32, data[16]); // Bits per pixel
        }

        private static void AssertValidBmpHeader(byte[] data)
        {
            Assert.True(data.Length >= 54, "BMP data too short");
            Assert.Equal((byte)'B', data[0]);
            Assert.Equal((byte)'M', data[1]);
            Assert.Equal(40, BitConverter.ToInt32(data, 14)); // DIB header size
            Assert.Equal(24, BitConverter.ToInt16(data, 28)); // Bits per pixel
        }

        #endregion
    }
}
