using System;
using System.IO;
using Xunit;
using WAD.NET.Concrete;
using WAD.NET.Definitions;
using WAD.NET.Export;
using WAD.NET.Sprites;

namespace WAD.NET.Tests
{
    public class ResourceParsingTests
    {
        #region DoomPicture Tests

        [Fact]
        public void DoomPicture_ShouldStoreProperties()
        {
            var pixels = new byte[64 * 64];
            Array.Fill(pixels, (byte)100);

            var picture = new DoomPicture(64, 64, -32, -56, pixels);

            Assert.Equal(64, picture.Width);
            Assert.Equal(64, picture.Height);
            Assert.Equal(-32, picture.LeftOffset);
            Assert.Equal(-56, picture.TopOffset);
            Assert.Equal(4096, picture.Pixels.Length);
        }

        [Fact]
        public void DoomPicture_GetPixel_ShouldReturnCorrectValue()
        {
            var pixels = new byte[4 * 4];
            pixels[0] = 10;   // (0,0)
            pixels[3] = 20;   // (3,0)
            pixels[12] = 30;  // (0,3)
            pixels[15] = 40;  // (3,3)

            var picture = new DoomPicture(4, 4, 0, 0, pixels);

            Assert.Equal(10, picture.GetPixel(0, 0));
            Assert.Equal(20, picture.GetPixel(3, 0));
            Assert.Equal(30, picture.GetPixel(0, 3));
            Assert.Equal(40, picture.GetPixel(3, 3));
        }

        [Fact]
        public void DoomPicture_GetPixel_ShouldReturnTransparentForOutOfBounds()
        {
            var pixels = new byte[4 * 4];
            var picture = new DoomPicture(4, 4, 0, 0, pixels);

            Assert.Equal(DoomPicture.TransparentIndex, picture.GetPixel(-1, 0));
            Assert.Equal(DoomPicture.TransparentIndex, picture.GetPixel(4, 0));
            Assert.Equal(DoomPicture.TransparentIndex, picture.GetPixel(0, -1));
            Assert.Equal(DoomPicture.TransparentIndex, picture.GetPixel(0, 4));
        }

        [Fact]
        public void DoomPicture_IsTransparent_ShouldDetectTransparency()
        {
            var pixels = new byte[4];
            pixels[0] = 100;
            pixels[1] = DoomPicture.TransparentIndex;

            var picture = new DoomPicture(2, 2, 0, 0, pixels);

            Assert.False(picture.IsTransparent(0, 0));
            Assert.True(picture.IsTransparent(1, 0));
        }

        [Fact]
        public void DoomPicture_ToRgba_ShouldConvertWithPalette()
        {
            var pixels = new byte[] { 0, 1, DoomPicture.TransparentIndex, 2 };

            var paletteData = new byte[Palette.ByteSize];
            paletteData[0] = 255; paletteData[1] = 0; paletteData[2] = 0;    // Index 0 = Red
            paletteData[3] = 0; paletteData[4] = 255; paletteData[5] = 0;    // Index 1 = Green
            paletteData[6] = 0; paletteData[7] = 0; paletteData[8] = 255;    // Index 2 = Blue

            var picture = new DoomPicture(2, 2, 0, 0, pixels);
            var palette = new Palette(paletteData);
            var rgba = picture.ToRgba(palette);

            // Pixel 0 = Red, opaque
            Assert.Equal(255, rgba[0]);
            Assert.Equal(0, rgba[1]);
            Assert.Equal(0, rgba[2]);
            Assert.Equal(255, rgba[3]);

            // Pixel 2 = Transparent
            Assert.Equal(0, rgba[8]);
            Assert.Equal(0, rgba[9]);
            Assert.Equal(0, rgba[10]);
            Assert.Equal(0, rgba[11]);
        }

        #endregion

        #region PictureLump Tests

        [Fact]
        public void PictureLump_ShouldParseSimplePicture()
        {
            // Create a 2x4 picture with simple column data
            var data = CreateSimplePictureData(2, 4);

            var lump = new PictureLump("TESTPIC", "TEST.WAD", data);

            Assert.Equal(2, lump.Picture.Width);
            Assert.Equal(4, lump.Picture.Height);
        }

        [Fact]
        public void PictureLump_LooksLikePicture_ShouldDetectValidPictures()
        {
            var validData = CreateSimplePictureData(8, 8);
            Assert.True(PictureLump.LooksLikePicture(validData));

            // Too small
            var tooSmall = new byte[4];
            Assert.False(PictureLump.LooksLikePicture(tooSmall));

            // Zero dimensions
            var zeroDims = new byte[16];
            Assert.False(PictureLump.LooksLikePicture(zeroDims));
        }

        [Fact]
        public void PictureLump_ShouldThrowOnTruncatedData()
        {
            var data = new byte[4]; // Too small

            Assert.Throws<FormatException>(() => new PictureLump("TEST", "TEST.WAD", data));
        }

        private static byte[] CreateSimplePictureData(ushort width, ushort height)
        {
            // Header: width(2) + height(2) + leftoff(2) + topoff(2) + columnOffsets(4*width)
            int headerSize = 8 + width * 4;

            // Each column: topDelta(1) + length(1) + padding(1) + pixels(height) + padding(1) + endmarker(1)
            int columnSize = 5 + height;
            int totalSize = headerSize + width * columnSize;

            var data = new byte[totalSize];
            int pos = 0;

            // Write header
            BitConverter.GetBytes(width).CopyTo(data, pos); pos += 2;
            BitConverter.GetBytes(height).CopyTo(data, pos); pos += 2;
            BitConverter.GetBytes((short)0).CopyTo(data, pos); pos += 2;
            BitConverter.GetBytes((short)0).CopyTo(data, pos); pos += 2;

            // Write column offsets
            for (int i = 0; i < width; i++)
            {
                uint offset = (uint)(headerSize + i * columnSize);
                BitConverter.GetBytes(offset).CopyTo(data, pos);
                pos += 4;
            }

            // Write column data
            for (int col = 0; col < width; col++)
            {
                data[pos++] = 0;                  // Top delta (start at row 0)
                data[pos++] = (byte)height;       // Length
                data[pos++] = 0;                  // Pre-padding

                for (int row = 0; row < height; row++)
                {
                    data[pos++] = (byte)((col + row) % 256);  // Pixel data
                }

                data[pos++] = 0;                  // Post-padding
                data[pos++] = 255;                // End of column marker
            }

            return data;
        }

        #endregion

        #region SpriteFrame Tests

        [Fact]
        public void SpriteFrame_ShouldStoreProperties()
        {
            var pixels = new byte[16];
            var picture = new DoomPicture(4, 4, -2, -4, pixels);

            var frame = new SpriteFrame("POSS", 'A', 1, false, picture);

            Assert.Equal("POSS", frame.SpriteName);
            Assert.Equal('A', frame.Frame);
            Assert.Equal(1, frame.Rotation);
            Assert.False(frame.IsMirrored);
            Assert.Same(picture, frame.Picture);
        }

        #endregion

        #region SpriteDefinition Tests

        [Fact]
        public void SpriteDefinition_ShouldStoreFrames()
        {
            var pixels = new byte[16];
            var picture = new DoomPicture(4, 4, 0, 0, pixels);
            var frame1 = new SpriteFrame("POSS", 'A', 1, false, picture);
            var frame2 = new SpriteFrame("POSS", 'A', 5, false, picture);

            var definition = new SpriteDefinition("POSS");
            definition.AddFrame(frame1);
            definition.AddFrame(frame2);

            Assert.True(definition.HasFrame('A'));
            Assert.Same(frame1, definition.GetFrame('A', 1));
            Assert.Same(frame2, definition.GetFrame('A', 5));
        }

        [Fact]
        public void SpriteDefinition_Rotation0_ShouldApplyToAllAngles()
        {
            var pixels = new byte[16];
            var picture = new DoomPicture(4, 4, 0, 0, pixels);
            var frame = new SpriteFrame("POSS", 'A', 0, false, picture);

            var definition = new SpriteDefinition("POSS");
            definition.AddFrame(frame);

            // Rotation 0 should return the same frame for all angles
            Assert.Same(frame, definition.GetFrame('A', 1));
            Assert.Same(frame, definition.GetFrame('A', 4));
            Assert.Same(frame, definition.GetFrame('A', 8));
        }

        #endregion

        #region SpriteParser Tests

        [Fact]
        public void SpriteParser_IsSpriteLumpName_ShouldValidateNames()
        {
            Assert.True(SpriteParser.IsSpriteLumpName("POSSA1"));
            Assert.True(SpriteParser.IsSpriteLumpName("PLAYA2A8"));
            Assert.True(SpriteParser.IsSpriteLumpName("TROOA0"));  // Rotation 0 (all angles)
            Assert.False(SpriteParser.IsSpriteLumpName("POSS"));   // Too short (4 chars)
            Assert.False(SpriteParser.IsSpriteLumpName("POSSA"));  // Too short (5 chars)
            Assert.False(SpriteParser.IsSpriteLumpName("POSS99")); // Invalid rotation (9 > 8)
        }

        [Fact]
        public void SpriteParser_GetSpriteName_ShouldExtractName()
        {
            Assert.Equal("POSS", SpriteParser.GetSpriteName("POSSA1"));
            Assert.Equal("PLAY", SpriteParser.GetSpriteName("PLAYA2A8"));
            Assert.Null(SpriteParser.GetSpriteName("POS")); // Too short
        }

        [Fact]
        public void SpriteParser_GetFrame_ShouldExtractFrame()
        {
            Assert.Equal('A', SpriteParser.GetFrame("POSSA1"));
            Assert.Equal('B', SpriteParser.GetFrame("POSSB3"));
            Assert.Null(SpriteParser.GetFrame("POSS")); // Too short
        }

        [Fact]
        public void SpriteParser_GetRotation_ShouldExtractRotation()
        {
            Assert.Equal(1, SpriteParser.GetRotation("POSSA1"));
            Assert.Equal(0, SpriteParser.GetRotation("POSSA0"));
            Assert.Equal(8, SpriteParser.GetRotation("POSSA8"));
            Assert.Null(SpriteParser.GetRotation("POSS")); // Too short
        }

        #endregion

        #region TextureBuilder Tests

        [Fact]
        public void TextureBuilder_BuildIndexed_ShouldCompositePatches()
        {
            // Create a 64x64 texture from a single 64x64 patch
            var patchPixels = new byte[64 * 64];
            Array.Fill(patchPixels, (byte)100);
            var patch = new DoomPicture(64, 64, 0, 0, patchPixels);

            var patchNames = new[] { "PATCH1" };
            var patches = new System.Collections.Generic.Dictionary<string, DoomPicture>
            {
                ["PATCH1"] = patch
            };

            var texturePatch = new TexturePatch(0, 0, 0);
            var definition = new TextureDefinition("TESTTEX", 64, 64, new[] { texturePatch });

            var builder = new TextureBuilder(definition, patchNames, patches);
            var result = builder.BuildIndexed();

            Assert.Equal(64 * 64, result.Length);
            Assert.All(result, pixel => Assert.Equal(100, pixel));
        }

        [Fact]
        public void TextureBuilder_BuildIndexed_ShouldHandleOffsets()
        {
            // Create a 64x64 texture with a 32x32 patch at offset (16, 16)
            var patchPixels = new byte[32 * 32];
            Array.Fill(patchPixels, (byte)50);
            var patch = new DoomPicture(32, 32, 0, 0, patchPixels);

            var patchNames = new[] { "PATCH1" };
            var patches = new System.Collections.Generic.Dictionary<string, DoomPicture>
            {
                ["PATCH1"] = patch
            };

            var texturePatch = new TexturePatch(16, 16, 0);
            var definition = new TextureDefinition("TESTTEX", 64, 64, new[] { texturePatch });

            var builder = new TextureBuilder(definition, patchNames, patches);
            var result = builder.BuildIndexed();

            // Pixel at (0,0) should be transparent
            Assert.Equal(DoomPicture.TransparentIndex, result[0]);

            // Pixel at (20, 20) should be filled
            Assert.Equal(50, result[20 * 64 + 20]);
        }

        [Fact]
        public void TextureBuilder_ShouldExposeProperties()
        {
            var definition = new TextureDefinition("MYWALL", 128, 64, Array.Empty<TexturePatch>());
            var builder = new TextureBuilder(definition, Array.Empty<string>(),
                new System.Collections.Generic.Dictionary<string, DoomPicture>());

            Assert.Equal("MYWALL", builder.Name);
            Assert.Equal(128, builder.Width);
            Assert.Equal(64, builder.Height);
        }

        #endregion

        #region MusicLump Tests

        [Fact]
        public void MusicLump_ShouldDetectMusFormat()
        {
            var data = new byte[32];
            data[0] = (byte)'M';
            data[1] = (byte)'U';
            data[2] = (byte)'S';
            data[3] = 0x1A;
            BitConverter.GetBytes((ushort)100).CopyTo(data, 4);  // Score length
            BitConverter.GetBytes((ushort)16).CopyTo(data, 6);   // Score start
            BitConverter.GetBytes((ushort)2).CopyTo(data, 12);   // Instrument count
            BitConverter.GetBytes((ushort)0).CopyTo(data, 16);   // Instrument 0
            BitConverter.GetBytes((ushort)40).CopyTo(data, 18);  // Instrument 1

            var lump = new MusicLump("D_E1M1", "TEST.WAD", data);

            Assert.True(lump.IsMus);
            Assert.False(lump.IsMidi);
            Assert.Equal(100, lump.ScoreLength);
            Assert.Equal(16, lump.ScoreStartOffset);
            Assert.Equal(2, lump.Instruments.Length);
            Assert.Equal(0, lump.Instruments[0]);
            Assert.Equal(40, lump.Instruments[1]);
        }

        [Fact]
        public void MusicLump_ShouldDetectMidiFormat()
        {
            var data = new byte[16];
            data[0] = (byte)'M';
            data[1] = (byte)'T';
            data[2] = (byte)'h';
            data[3] = (byte)'d';

            var lump = new MusicLump("D_INTER", "TEST.WAD", data);

            Assert.False(lump.IsMus);
            Assert.True(lump.IsMidi);
        }

        [Fact]
        public void MusicLump_ShouldDetectUnknownFormat()
        {
            var data = new byte[16];
            data[0] = 0xFF;
            data[1] = 0xFE;

            var lump = new MusicLump("D_RUNNIN", "TEST.WAD", data);

            Assert.False(lump.IsMus);
            Assert.False(lump.IsMidi);
        }

        [Fact]
        public void MusicLump_MusicData_ShouldBeBackwardCompatible()
        {
            var data = new byte[] { 1, 2, 3, 4, 5 };

            var lump = new MusicLump("D_TEST", "TEST.WAD", data);

            Assert.Same(lump.RawData, lump.MusicData);
        }

        #endregion

        #region MusToMidiConverter Tests

        [Fact]
        public void MusToMidiConverter_ShouldCreateValidMidiHeader()
        {
            // Minimal MUS file
            var musData = new byte[32];
            musData[0] = (byte)'M';
            musData[1] = (byte)'U';
            musData[2] = (byte)'S';
            musData[3] = 0x1A;
            BitConverter.GetBytes((ushort)1).CopyTo(musData, 4);   // Score length
            BitConverter.GetBytes((ushort)16).CopyTo(musData, 6);  // Score start
            BitConverter.GetBytes((ushort)0).CopyTo(musData, 12);  // Instrument count
            musData[16] = 0x60; // End of track event (type 6, channel 0)

            var midi = MusToMidiConverter.Convert(musData);

            // Check MIDI header
            Assert.Equal((byte)'M', midi[0]);
            Assert.Equal((byte)'T', midi[1]);
            Assert.Equal((byte)'h', midi[2]);
            Assert.Equal((byte)'d', midi[3]);

            // Check track header
            Assert.Equal((byte)'M', midi[14]);
            Assert.Equal((byte)'T', midi[15]);
            Assert.Equal((byte)'r', midi[16]);
            Assert.Equal((byte)'k', midi[17]);
        }

        [Fact]
        public void MusToMidiConverter_ShouldThrowOnInvalidMagic()
        {
            // Need at least 16 bytes to pass size check, but with invalid magic
            var data = new byte[16];
            data[0] = 0;  // Invalid magic

            Assert.Throws<FormatException>(() => MusToMidiConverter.Convert(data));
        }

        [Fact]
        public void MusToMidiConverter_ShouldThrowOnTooShortData()
        {
            var data = new byte[] { (byte)'M', (byte)'U', (byte)'S', 0x1A };

            Assert.Throws<ArgumentException>(() => MusToMidiConverter.Convert(data));
        }

        #endregion

        #region ImageExporter Tests

        [Fact]
        public void ImageExporter_ExportTga_ShouldCreateValidHeader()
        {
            var pixels = new byte[4]; // 2x2
            var picture = new DoomPicture(2, 2, 0, 0, pixels);

            var paletteData = new byte[Palette.ByteSize];
            var palette = new Palette(paletteData);

            using var output = new MemoryStream();
            ImageExporter.ExportTga(picture, palette, output);

            output.Position = 0;
            var tgaData = output.ToArray();

            // Check TGA header
            Assert.Equal(0, tgaData[0]);   // ID length
            Assert.Equal(0, tgaData[1]);   // Color map type
            Assert.Equal(2, tgaData[2]);   // Image type (uncompressed true-color)
            Assert.Equal(2, BitConverter.ToInt16(tgaData, 12));  // Width
            Assert.Equal(2, BitConverter.ToInt16(tgaData, 14));  // Height
            Assert.Equal(32, tgaData[16]); // Bits per pixel
        }

        [Fact]
        public void ImageExporter_ExportBmp_ShouldCreateValidHeader()
        {
            var flatData = new byte[FlatLump.Size];
            var flat = new FlatLump("FLOOR", "TEST.WAD", flatData);

            var paletteData = new byte[Palette.ByteSize];
            var palette = new Palette(paletteData);

            using var output = new MemoryStream();
            ImageExporter.ExportBmp(flat, palette, output);

            output.Position = 0;
            var bmpData = output.ToArray();

            // Check BMP header
            Assert.Equal((byte)'B', bmpData[0]);
            Assert.Equal((byte)'M', bmpData[1]);

            // Check DIB header
            Assert.Equal(40, BitConverter.ToInt32(bmpData, 14)); // Header size
            Assert.Equal(64, BitConverter.ToInt32(bmpData, 18)); // Width
            Assert.Equal(64, BitConverter.ToInt32(bmpData, 22)); // Height
            Assert.Equal(24, BitConverter.ToInt16(bmpData, 28)); // Bits per pixel
        }

        [Fact]
        public void ImageExporter_ExportTga_ShouldHandleTransparency()
        {
            var pixels = new byte[] { 0, DoomPicture.TransparentIndex, 0, 0 };
            var picture = new DoomPicture(2, 2, 0, 0, pixels);

            var paletteData = new byte[Palette.ByteSize];
            paletteData[0] = 255; // Red for index 0
            var palette = new Palette(paletteData);

            using var output = new MemoryStream();
            ImageExporter.ExportTga(picture, palette, output);

            var data = output.ToArray();

            // TGA header is 18 bytes, then pixel data starts
            // Pixel 0: BGRA
            Assert.Equal(0, data[18]);   // B
            Assert.Equal(0, data[19]);   // G
            Assert.Equal(255, data[20]); // R
            Assert.Equal(255, data[21]); // A (opaque)

            // Pixel 1 (transparent)
            Assert.Equal(0, data[22]);   // B
            Assert.Equal(0, data[23]);   // G
            Assert.Equal(0, data[24]);   // R
            Assert.Equal(0, data[25]);   // A (transparent)
        }

        [Fact]
        public void ImageExporter_ExportPng_ShouldCreateValidPngHeader()
        {
            var pixels = new byte[4]; // 2x2
            var picture = new DoomPicture(2, 2, 0, 0, pixels);

            var paletteData = new byte[Palette.ByteSize];
            var palette = new Palette(paletteData);

            using var output = new MemoryStream();
            ImageExporter.ExportPng(picture, palette, output);

            output.Position = 0;
            var pngData = output.ToArray();

            // Check PNG signature (8 bytes)
            Assert.Equal(0x89, pngData[0]);
            Assert.Equal((byte)'P', pngData[1]);
            Assert.Equal((byte)'N', pngData[2]);
            Assert.Equal((byte)'G', pngData[3]);
            Assert.Equal(0x0D, pngData[4]);
            Assert.Equal(0x0A, pngData[5]);
            Assert.Equal(0x1A, pngData[6]);
            Assert.Equal(0x0A, pngData[7]);
        }

        [Fact]
        public void ImageExporter_ExportPng_ShouldExportFlatLump()
        {
            var flatData = new byte[FlatLump.Size];
            var flat = new FlatLump("FLOOR", "TEST.WAD", flatData);

            var paletteData = new byte[Palette.ByteSize];
            var palette = new Palette(paletteData);

            using var output = new MemoryStream();
            ImageExporter.ExportPng(flat, palette, output);

            output.Position = 0;
            var pngData = output.ToArray();

            // Check PNG signature
            Assert.Equal(0x89, pngData[0]);
            Assert.Equal((byte)'P', pngData[1]);
            Assert.Equal((byte)'N', pngData[2]);
            Assert.Equal((byte)'G', pngData[3]);
        }

        [Fact]
        public void ImageExporter_ExportPng_ShouldExportRawRgba()
        {
            var rgba = new byte[4 * 4]; // 2x2 with RGBA
            rgba[0] = 255; rgba[1] = 0; rgba[2] = 0; rgba[3] = 255;   // Red
            rgba[4] = 0; rgba[5] = 255; rgba[6] = 0; rgba[7] = 255;   // Green
            rgba[8] = 0; rgba[9] = 0; rgba[10] = 255; rgba[11] = 255; // Blue
            rgba[12] = 0; rgba[13] = 0; rgba[14] = 0; rgba[15] = 0;   // Transparent

            using var output = new MemoryStream();
            ImageExporter.ExportPng(2, 2, rgba, output);

            output.Position = 0;
            var pngData = output.ToArray();

            // Check PNG signature
            Assert.Equal(0x89, pngData[0]);
            Assert.Equal((byte)'P', pngData[1]);
            Assert.Equal((byte)'N', pngData[2]);
            Assert.Equal((byte)'G', pngData[3]);

            // PNG should be larger than header alone (compressed pixel data + chunks)
            Assert.True(pngData.Length > 8);
        }

        [Fact]
        public void ImageExporter_ExportPng_ShouldThrowOnInvalidRgbaLength()
        {
            var rgba = new byte[10]; // Invalid size for any dimension

            using var output = new MemoryStream();
            Assert.Throws<ArgumentException>(() => ImageExporter.ExportPng(2, 2, rgba, output));
        }

        [Fact]
        public void ImageExporter_ExportPng_ShouldThrowOnNullArguments()
        {
            var paletteData = new byte[Palette.ByteSize];
            var palette = new Palette(paletteData);
            var pixels = new byte[4];
            var picture = new DoomPicture(2, 2, 0, 0, pixels);

            using var output = new MemoryStream();

            Assert.Throws<ArgumentNullException>(() => ImageExporter.ExportPng((DoomPicture)null!, palette, output));
            Assert.Throws<ArgumentNullException>(() => ImageExporter.ExportPng(picture, null!, output));
            Assert.Throws<ArgumentNullException>(() => ImageExporter.ExportPng(picture, palette, (Stream)null!));
        }

        #endregion
    }
}
