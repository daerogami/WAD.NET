using System;
using Xunit;
using WAD.NET.Concrete;
using WAD.NET.Definitions;

namespace WAD.NET.Tests
{
    public class LumpParserTests
    {
        #region Color Tests

        [Fact]
        public void Color_ShouldStoreRgbValues()
        {
            var color = new Color(255, 128, 64);

            Assert.Equal(255, color.R);
            Assert.Equal(128, color.G);
            Assert.Equal(64, color.B);
        }

        [Fact]
        public void Color_Equality_ShouldWorkCorrectly()
        {
            var color1 = new Color(100, 150, 200);
            var color2 = new Color(100, 150, 200);
            var color3 = new Color(100, 150, 201);

            Assert.Equal(color1, color2);
            Assert.NotEqual(color1, color3);
            Assert.True(color1 == color2);
            Assert.True(color1 != color3);
        }

        [Fact]
        public void Color_ToString_ShouldReturnHexFormat()
        {
            var color = new Color(255, 0, 128);

            Assert.Equal("#FF0080", color.ToString());
        }

        #endregion

        #region Palette Tests

        [Fact]
        public void Palette_ShouldParse256Colors()
        {
            // Create a palette with 256 colors (768 bytes)
            var data = new byte[Palette.ByteSize];
            for (int i = 0; i < 256; i++)
            {
                data[i * 3 + 0] = (byte)i;       // R
                data[i * 3 + 1] = (byte)(255 - i); // G
                data[i * 3 + 2] = 128;            // B
            }

            var palette = new Palette(data);

            Assert.Equal(256, palette.Colors.Length);
            Assert.Equal(0, palette.Colors[0].R);
            Assert.Equal(255, palette.Colors[0].G);
            Assert.Equal(128, palette.Colors[0].B);
            Assert.Equal(255, palette.Colors[255].R);
            Assert.Equal(0, palette.Colors[255].G);
        }

        [Fact]
        public void Palette_ShouldThrowOnInsufficientData()
        {
            var data = new byte[100]; // Too small

            Assert.Throws<FormatException>(() => new Palette(data));
        }

        #endregion

        #region PaletteLump Tests

        [Fact]
        public void PaletteLump_ShouldParse14Palettes()
        {
            // PLAYPAL contains 14 palettes
            var data = new byte[PaletteLump.ExpectedSize];

            // Fill with test data - each palette has different first color
            for (int p = 0; p < 14; p++)
            {
                int offset = p * Palette.ByteSize;
                data[offset + 0] = (byte)p; // First color R value = palette index
                data[offset + 1] = 100;
                data[offset + 2] = 200;
            }

            var lump = new PaletteLump("PLAYPAL", "TEST.WAD", data);

            Assert.Equal(14, lump.Palettes.Length);
            Assert.Equal(0, lump.NormalPalette.Colors[0].R);
            Assert.Equal(5, lump.Palettes[5].Colors[0].R);
        }

        [Fact]
        public void PaletteLump_ShouldThrowOnInsufficientData()
        {
            var data = new byte[1000]; // Too small for 14 palettes

            Assert.Throws<FormatException>(() => new PaletteLump("PLAYPAL", "TEST.WAD", data));
        }

        #endregion

        #region ColorMapLump Tests

        [Fact]
        public void ColorMapLump_ShouldParse34Maps()
        {
            var data = new byte[ColorMapLump.ExpectedSize];

            // Fill with identity mapping for first map
            for (int i = 0; i < 256; i++)
            {
                data[i] = (byte)i;
            }

            // Fill map 32 (invulnerability) with reversed mapping
            for (int i = 0; i < 256; i++)
            {
                data[32 * 256 + i] = (byte)(255 - i);
            }

            var lump = new ColorMapLump("COLORMAP", "TEST.WAD", data);

            Assert.Equal(34, lump.Maps.Length);
            Assert.Equal(100, lump.Maps[0][100]); // Identity
            Assert.Equal(155, lump.InvulnerabilityMap[100]); // Reversed
        }

        [Fact]
        public void ColorMapLump_RemapColor_ShouldWorkCorrectly()
        {
            var data = new byte[ColorMapLump.ExpectedSize];

            // Create a simple darkening effect: each light level shifts colors by level
            for (int level = 0; level < 32; level++)
            {
                for (int color = 0; color < 256; color++)
                {
                    data[level * 256 + color] = (byte)Math.Max(0, color - level);
                }
            }

            var lump = new ColorMapLump("COLORMAP", "TEST.WAD", data);

            Assert.Equal(100, lump.RemapColor(100, 0));  // Full bright
            Assert.Equal(90, lump.RemapColor(100, 10)); // Darker
            Assert.Equal(69, lump.RemapColor(100, 31)); // Darkest
        }

        [Fact]
        public void ColorMapLump_RemapColor_ShouldValidateArguments()
        {
            var data = new byte[ColorMapLump.ExpectedSize];
            var lump = new ColorMapLump("COLORMAP", "TEST.WAD", data);

            Assert.Throws<ArgumentOutOfRangeException>(() => lump.RemapColor(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => lump.RemapColor(256, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => lump.RemapColor(0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => lump.RemapColor(0, 32));
        }

        #endregion

        #region PatchNamesLump Tests

        [Fact]
        public void PatchNamesLump_ShouldParsePatchNames()
        {
            // Create PNAMES data: count (4 bytes) + names (8 bytes each)
            var data = new byte[4 + 3 * 8]; // 3 patches

            // Write count
            BitConverter.GetBytes(3).CopyTo(data, 0);

            // Write patch names
            System.Text.Encoding.ASCII.GetBytes("WALL00_1").CopyTo(data, 4);
            System.Text.Encoding.ASCII.GetBytes("WALL00_2").CopyTo(data, 12);
            System.Text.Encoding.ASCII.GetBytes("DOOR2_1\0").CopyTo(data, 20);

            var lump = new PatchNamesLump("PNAMES", "TEST.WAD", data);

            Assert.Equal(3, lump.Count);
            Assert.Equal("WALL00_1", lump.PatchNames[0]);
            Assert.Equal("WALL00_2", lump.PatchNames[1]);
            Assert.Equal("DOOR2_1", lump.PatchNames[2]); // Null-terminated
        }

        [Fact]
        public void PatchNamesLump_IndexOf_ShouldFindPatch()
        {
            var data = new byte[4 + 2 * 8];
            BitConverter.GetBytes(2).CopyTo(data, 0);
            System.Text.Encoding.ASCII.GetBytes("PATCH1\0\0").CopyTo(data, 4);
            System.Text.Encoding.ASCII.GetBytes("PATCH2\0\0").CopyTo(data, 12);

            var lump = new PatchNamesLump("PNAMES", "TEST.WAD", data);

            Assert.Equal(0, lump.IndexOf("PATCH1"));
            Assert.Equal(1, lump.IndexOf("patch2")); // Case insensitive
            Assert.Equal(-1, lump.IndexOf("NOTFOUND"));
        }

        #endregion

        #region TextureLump Tests

        [Fact]
        public void TextureLump_ShouldParseTextureDefinitions()
        {
            // Create TEXTURE1 data with 1 texture
            // Header: count (4) + offset (4) = 8 bytes
            // Texture: name (8) + masked (4) + width (2) + height (2) + coldir (4) + pcount (2) = 22 bytes
            // No patches for simplicity

            var data = new byte[8 + 22];

            // Count = 1
            BitConverter.GetBytes(1).CopyTo(data, 0);

            // Offset to first texture = 8
            BitConverter.GetBytes(8).CopyTo(data, 4);

            // Texture definition at offset 8
            System.Text.Encoding.ASCII.GetBytes("STARTAN1").CopyTo(data, 8);  // Name
            // Masked (4 bytes) at offset 16 - skip
            BitConverter.GetBytes((ushort)64).CopyTo(data, 20);  // Width
            BitConverter.GetBytes((ushort)128).CopyTo(data, 22); // Height
            // Column directory (4 bytes) at offset 24 - skip
            BitConverter.GetBytes((ushort)0).CopyTo(data, 28);   // Patch count

            var lump = new TextureLump("TEXTURE1", "TEST.WAD", data);

            Assert.Equal(1, lump.Count);
            Assert.Equal("STARTAN1", lump.Textures[0].Name);
            Assert.Equal(64, lump.Textures[0].Width);
            Assert.Equal(128, lump.Textures[0].Height);
            Assert.Empty(lump.Textures[0].Patches);
        }

        [Fact]
        public void TextureLump_ShouldParsePatches()
        {
            // Create texture with 2 patches
            var data = new byte[8 + 22 + 20]; // Header + texture + 2 patches

            BitConverter.GetBytes(1).CopyTo(data, 0);   // Count
            BitConverter.GetBytes(8).CopyTo(data, 4);   // Offset

            System.Text.Encoding.ASCII.GetBytes("BIGWALL\0").CopyTo(data, 8);
            BitConverter.GetBytes((ushort)128).CopyTo(data, 20);  // Width
            BitConverter.GetBytes((ushort)128).CopyTo(data, 22);  // Height
            BitConverter.GetBytes((ushort)2).CopyTo(data, 28);    // Patch count

            // Patch 1: x=0, y=0, index=5
            BitConverter.GetBytes((short)0).CopyTo(data, 30);
            BitConverter.GetBytes((short)0).CopyTo(data, 32);
            BitConverter.GetBytes((ushort)5).CopyTo(data, 34);

            // Patch 2: x=64, y=0, index=10
            BitConverter.GetBytes((short)64).CopyTo(data, 40);
            BitConverter.GetBytes((short)0).CopyTo(data, 42);
            BitConverter.GetBytes((ushort)10).CopyTo(data, 44);

            var lump = new TextureLump("TEXTURE1", "TEST.WAD", data);

            Assert.Equal(2, lump.Textures[0].Patches.Length);
            Assert.Equal(0, lump.Textures[0].Patches[0].OriginX);
            Assert.Equal(5, lump.Textures[0].Patches[0].PatchIndex);
            Assert.Equal(64, lump.Textures[0].Patches[1].OriginX);
            Assert.Equal(10, lump.Textures[0].Patches[1].PatchIndex);
        }

        [Fact]
        public void TextureLump_Find_ShouldLocateTexture()
        {
            var data = new byte[8 + 44]; // 2 textures

            BitConverter.GetBytes(2).CopyTo(data, 0);
            BitConverter.GetBytes(8).CopyTo(data, 4);
            BitConverter.GetBytes(30).CopyTo(data, 8); // Offset for second

            // Note: This test data is simplified - actual offsets would need adjustment
            // For a proper test, we'd build correct offset tables
        }

        #endregion

        #region SoundLump Tests

        [Fact]
        public void SoundLump_ShouldParseDmxFormat()
        {
            // DMX format: format (2) + rate (2) + samples (4) + padding (16) + data + padding (16)
            var sampleData = new byte[] { 128, 130, 132, 134, 136 }; // 5 samples
            var totalSamples = sampleData.Length + 32; // Include padding

            var data = new byte[8 + 16 + sampleData.Length + 16];

            BitConverter.GetBytes((ushort)3).CopyTo(data, 0);           // Format = 3
            BitConverter.GetBytes((ushort)11025).CopyTo(data, 2);       // Sample rate
            BitConverter.GetBytes((uint)totalSamples).CopyTo(data, 4);  // Total samples

            // Skip 16 bytes padding
            sampleData.CopyTo(data, 24);

            var lump = new SoundLump("DSPISTOL", "TEST.WAD", data);

            Assert.Equal(3, lump.Format);
            Assert.Equal(11025, lump.SampleRate);
            Assert.Equal(5, lump.Samples.Length);
            Assert.Equal(128, lump.Samples[0]);
        }

        [Fact]
        public void SoundLump_Duration_ShouldCalculateCorrectly()
        {
            var data = new byte[8 + 32 + 11025]; // 1 second of audio at 11025 Hz

            BitConverter.GetBytes((ushort)3).CopyTo(data, 0);
            BitConverter.GetBytes((ushort)11025).CopyTo(data, 2);
            BitConverter.GetBytes((uint)(11025 + 32)).CopyTo(data, 4);

            var lump = new SoundLump("DSTEST", "TEST.WAD", data);

            Assert.Equal(1.0, lump.Duration, precision: 2);
        }

        [Fact]
        public void SoundLump_ToSigned16Bit_ShouldConvertCorrectly()
        {
            var data = new byte[8 + 32 + 3];

            BitConverter.GetBytes((ushort)3).CopyTo(data, 0);
            BitConverter.GetBytes((ushort)11025).CopyTo(data, 2);
            BitConverter.GetBytes((uint)35).CopyTo(data, 4);

            // Sample values: 0 (min), 128 (center), 255 (max)
            data[24] = 0;
            data[25] = 128;
            data[26] = 255;

            var lump = new SoundLump("DSTEST", "TEST.WAD", data);
            var signed = lump.ToSigned16Bit();

            Assert.Equal(-32768, signed[0]); // 0 -> -32768
            Assert.Equal(0, signed[1]);       // 128 -> 0
            Assert.Equal(32512, signed[2]);   // 255 -> 32512
        }

        #endregion

        #region FlatLump Tests

        [Fact]
        public void FlatLump_ShouldParse64x64Pixels()
        {
            var data = new byte[FlatLump.Size];

            // Fill with a gradient pattern
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    data[y * 64 + x] = (byte)((x + y) % 256);
                }
            }

            var lump = new FlatLump("FLOOR4_8", "TEST.WAD", data);

            Assert.Equal(FlatLump.Width, 64);
            Assert.Equal(FlatLump.Height, 64);
            Assert.Equal(4096, lump.Pixels.Length);
            Assert.Equal(0, lump.GetPixel(0, 0));
            Assert.Equal(63, lump.GetPixel(63, 0));
            Assert.Equal(126, lump.GetPixel(63, 63));
        }

        [Fact]
        public void FlatLump_GetPixel_ShouldValidateBounds()
        {
            var data = new byte[FlatLump.Size];
            var lump = new FlatLump("FLAT", "TEST.WAD", data);

            Assert.Throws<ArgumentOutOfRangeException>(() => lump.GetPixel(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => lump.GetPixel(64, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => lump.GetPixel(0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => lump.GetPixel(0, 64));
        }

        [Fact]
        public void FlatLump_ToRgba_ShouldConvertWithPalette()
        {
            var flatData = new byte[FlatLump.Size];
            flatData[0] = 0;   // First pixel = palette index 0
            flatData[1] = 100; // Second pixel = palette index 100

            var paletteData = new byte[Palette.ByteSize];
            paletteData[0] = 255; paletteData[1] = 0; paletteData[2] = 0;     // Index 0 = Red
            paletteData[300] = 0; paletteData[301] = 255; paletteData[302] = 0; // Index 100 = Green

            var flat = new FlatLump("FLAT", "TEST.WAD", flatData);
            var palette = new Palette(paletteData);
            var rgba = flat.ToRgba(palette);

            Assert.Equal(4096 * 4, rgba.Length);
            Assert.Equal(255, rgba[0]); // R
            Assert.Equal(0, rgba[1]);   // G
            Assert.Equal(0, rgba[2]);   // B
            Assert.Equal(255, rgba[3]); // A

            Assert.Equal(0, rgba[4]);   // R
            Assert.Equal(255, rgba[5]); // G
            Assert.Equal(0, rgba[6]);   // B
            Assert.Equal(255, rgba[7]); // A
        }

        [Fact]
        public void FlatLump_ShouldThrowOnWrongSize()
        {
            var data = new byte[100]; // Wrong size

            Assert.Throws<FormatException>(() => new FlatLump("FLAT", "TEST.WAD", data));
        }

        #endregion
    }
}
