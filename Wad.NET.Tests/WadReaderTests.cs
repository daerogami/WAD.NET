using System;
using System.IO;
using System.Text;
using Xunit;
using WAD.NET.Concrete;
using WAD.NET.Enums;

namespace WAD.NET.Tests
{
    public class WadReaderTests
    {
        #region WAD Type Detection Tests

        [Fact]
        public void WadReader_ShouldIdentifyIWAD()
        {
            var wadData = CreateMinimalWad("IWAD");

            using var stream = new MemoryStream(wadData);
            using var reader = new WadReader(stream, wadName: "TEST.WAD");

            var wad = reader.ReadWad();

            Assert.Equal(WadType.IWAD, wad.WadType);
        }

        [Fact]
        public void WadReader_ShouldIdentifyPWAD()
        {
            var wadData = CreateMinimalWad("PWAD");

            using var stream = new MemoryStream(wadData);
            using var reader = new WadReader(stream, wadName: "TEST.WAD");

            var wad = reader.ReadWad();

            Assert.Equal(WadType.PWAD, wad.WadType);
        }

        [Fact]
        public void WadReader_ShouldThrowOnUnknownMagic()
        {
            var wadData = CreateMinimalWad("XWAD");

            using var stream = new MemoryStream(wadData);
            using var reader = new WadReader(stream, wadName: "TEST.WAD");

            Assert.Throws<FormatException>(() => reader.ReadWad());
        }

        [Fact]
        public void WadReader_ShouldThrowOnPK3Magic()
        {
            // PK3/ZIP magic: PK\x03\x04
            var data = new byte[12];
            data[0] = (byte)'P';
            data[1] = (byte)'K';
            data[2] = 0x03;
            data[3] = 0x04;

            using var stream = new MemoryStream(data);
            using var reader = new WadReader(stream, wadName: "TEST.PK3");

            var ex = Assert.Throws<FormatException>(() => reader.ReadWad());
            Assert.Contains("PK3", ex.Message);
        }

        #endregion

        #region Validation Tests

        [Fact]
        public void WadReader_ShouldThrowOnFileTooSmall()
        {
            var data = new byte[8]; // Less than 12 byte header

            using var stream = new MemoryStream(data);
            using var reader = new WadReader(stream, wadName: "TEST.WAD");

            Assert.Throws<FormatException>(() => reader.ReadWad());
        }

        [Fact]
        public void WadReader_ShouldThrowOnInvalidDirectoryOffset()
        {
            var data = new byte[12];
            Encoding.ASCII.GetBytes("IWAD").CopyTo(data, 0);
            BitConverter.GetBytes(1).CopyTo(data, 4);    // 1 lump
            BitConverter.GetBytes(1000).CopyTo(data, 8); // Invalid offset (beyond file)

            using var stream = new MemoryStream(data);
            using var reader = new WadReader(stream, wadName: "TEST.WAD");

            Assert.Throws<FormatException>(() => reader.ReadWad());
        }

        [Fact]
        public void WadReader_ShouldThrowOnDirectoryExtendsBeyondFile()
        {
            // Directory at offset 12, but 100 lumps would extend beyond file
            var data = new byte[20];
            Encoding.ASCII.GetBytes("IWAD").CopyTo(data, 0);
            BitConverter.GetBytes(100).CopyTo(data, 4);  // 100 lumps
            BitConverter.GetBytes(12).CopyTo(data, 8);   // Directory at offset 12

            using var stream = new MemoryStream(data);
            using var reader = new WadReader(stream, wadName: "TEST.WAD");

            Assert.Throws<FormatException>(() => reader.ReadWad());
        }

        [Fact]
        public void WadReader_ShouldThrowOnLumpExtendsBeyondFile()
        {
            // Create WAD with one lump that points beyond file
            var data = new byte[12 + 16]; // Header + 1 directory entry
            Encoding.ASCII.GetBytes("IWAD").CopyTo(data, 0);
            BitConverter.GetBytes(1).CopyTo(data, 4);    // 1 lump
            BitConverter.GetBytes(12).CopyTo(data, 8);   // Directory at 12

            // Directory entry: offset=100, size=1000 (invalid)
            BitConverter.GetBytes(100).CopyTo(data, 12);
            BitConverter.GetBytes(1000).CopyTo(data, 16);
            Encoding.ASCII.GetBytes("BADLUMP\0").CopyTo(data, 20);

            using var stream = new MemoryStream(data);
            using var reader = new WadReader(stream, wadName: "TEST.WAD");

            Assert.Throws<FormatException>(() => reader.ReadWad());
        }

        #endregion

        #region Lump Reading Tests

        [Fact]
        public void WadReader_ShouldReadBinaryLump()
        {
            var lumpData = new byte[] { 1, 2, 3, 4, 5 };
            var wadData = CreateWadWithLump("TESTLUMP", lumpData);

            using var stream = new MemoryStream(wadData);
            using var reader = new WadReader(stream, wadName: "TEST.WAD");

            var wad = reader.ReadWad();

            Assert.Single(wad.Lumps);
            var lump = wad.Lumps.Dequeue() as BinaryLump;
            Assert.NotNull(lump);
            Assert.Equal("TESTLUMP", lump.Name);
            Assert.Equal(lumpData, lump.Data);
        }

        [Fact]
        public void WadReader_ShouldReadEmptyWad()
        {
            var wadData = CreateMinimalWad("PWAD");

            using var stream = new MemoryStream(wadData);
            using var reader = new WadReader(stream, wadName: "EMPTY.WAD");

            var wad = reader.ReadWad();

            Assert.Empty(wad.Lumps);
        }

        [Fact]
        public void WadReader_ShouldSkipMarkerLumps()
        {
            // Create WAD with F_START and F_END markers (size 0)
            var data = new byte[12 + 32]; // Header + 2 directory entries
            Encoding.ASCII.GetBytes("PWAD").CopyTo(data, 0);
            BitConverter.GetBytes(2).CopyTo(data, 4);
            BitConverter.GetBytes(12).CopyTo(data, 8);

            // F_START marker (size 0)
            BitConverter.GetBytes(0).CopyTo(data, 12); // offset (unused)
            BitConverter.GetBytes(0).CopyTo(data, 16); // size = 0
            Encoding.ASCII.GetBytes("F_START\0").CopyTo(data, 20);

            // F_END marker (size 0)
            BitConverter.GetBytes(0).CopyTo(data, 28);
            BitConverter.GetBytes(0).CopyTo(data, 32);
            Encoding.ASCII.GetBytes("F_END\0\0\0").CopyTo(data, 36);

            using var stream = new MemoryStream(data);
            using var reader = new WadReader(stream, wadName: "TEST.WAD");

            var wad = reader.ReadWad();

            Assert.Empty(wad.Lumps); // Markers are not added to lumps
        }

        [Fact]
        public void WadReader_ShouldParseFlatsBetweenMarkers()
        {
            // Create WAD: F_START, flat data, F_END
            var flatData = new byte[4096]; // 64x64 flat
            for (int i = 0; i < flatData.Length; i++)
                flatData[i] = (byte)(i % 256);

            var wadData = CreateWadWithFlatSection("FLOOR1", flatData);

            using var stream = new MemoryStream(wadData);
            using var reader = new WadReader(stream, wadName: "TEST.WAD");

            var wad = reader.ReadWad();

            Assert.Single(wad.Lumps);
            var flat = wad.Lumps.Dequeue() as FlatLump;
            Assert.NotNull(flat);
            Assert.Equal("FLOOR1", flat.Name);
        }

        #endregion

        #region Lump Type Detection Tests

        [Fact]
        public void WadReader_ShouldCreateMusicLumpForD_Prefix()
        {
            var musicData = new byte[] { 0x4D, 0x55, 0x53, 0x1A }; // MUS magic
            var wadData = CreateWadWithLump("D_E1M1", musicData);

            using var stream = new MemoryStream(wadData);
            using var reader = new WadReader(stream, wadName: "TEST.WAD");

            var wad = reader.ReadWad();

            var lump = wad.Lumps.Dequeue();
            Assert.IsType<MusicLump>(lump);
            Assert.Equal("D_E1M1", lump.Name);
        }

        [Fact]
        public void WadReader_ShouldCreatePaletteLumpForPLAYPAL()
        {
            var paletteData = new byte[PaletteLump.ExpectedSize];
            var wadData = CreateWadWithLump("PLAYPAL", paletteData);

            using var stream = new MemoryStream(wadData);
            using var reader = new WadReader(stream, wadName: "TEST.WAD");

            var wad = reader.ReadWad();

            var lump = wad.Lumps.Dequeue();
            Assert.IsType<PaletteLump>(lump);
        }

        [Fact]
        public void WadReader_ShouldCreateColorMapLumpForCOLORMAP()
        {
            var colormapData = new byte[ColorMapLump.ExpectedSize];
            var wadData = CreateWadWithLump("COLORMAP", colormapData);

            using var stream = new MemoryStream(wadData);
            using var reader = new WadReader(stream, wadName: "TEST.WAD");

            var wad = reader.ReadWad();

            var lump = wad.Lumps.Dequeue();
            Assert.IsType<ColorMapLump>(lump);
        }

        [Fact]
        public void WadReader_ShouldCreatePatchNamesLumpForPNAMES()
        {
            // Minimal PNAMES: count = 0
            var pnamesData = new byte[4];
            BitConverter.GetBytes(0).CopyTo(pnamesData, 0);
            var wadData = CreateWadWithLump("PNAMES", pnamesData);

            using var stream = new MemoryStream(wadData);
            using var reader = new WadReader(stream, wadName: "TEST.WAD");

            var wad = reader.ReadWad();

            var lump = wad.Lumps.Dequeue();
            Assert.IsType<PatchNamesLump>(lump);
        }

        [Fact]
        public void WadReader_ShouldCreateTextureLumpForTEXTURE1()
        {
            // Minimal TEXTURE1: count = 0
            var textureData = new byte[4];
            BitConverter.GetBytes(0).CopyTo(textureData, 0);
            var wadData = CreateWadWithLump("TEXTURE1", textureData);

            using var stream = new MemoryStream(wadData);
            using var reader = new WadReader(stream, wadName: "TEST.WAD");

            var wad = reader.ReadWad();

            var lump = wad.Lumps.Dequeue();
            Assert.IsType<TextureLump>(lump);
        }

        #endregion

        #region Helper Methods

        private static byte[] CreateMinimalWad(string magic)
        {
            var data = new byte[12];
            Encoding.ASCII.GetBytes(magic.Substring(0, 4)).CopyTo(data, 0);
            BitConverter.GetBytes(0).CopyTo(data, 4);    // 0 lumps
            BitConverter.GetBytes(12).CopyTo(data, 8);   // Directory at end
            return data;
        }

        private static byte[] CreateWadWithLump(string lumpName, byte[] lumpData)
        {
            // Header (12) + Lump data + Directory entry (16)
            var data = new byte[12 + lumpData.Length + 16];

            // Header
            Encoding.ASCII.GetBytes("PWAD").CopyTo(data, 0);
            BitConverter.GetBytes(1).CopyTo(data, 4);                         // 1 lump
            BitConverter.GetBytes(12 + lumpData.Length).CopyTo(data, 8);     // Directory offset

            // Lump data
            lumpData.CopyTo(data, 12);

            // Directory entry
            int dirOffset = 12 + lumpData.Length;
            BitConverter.GetBytes(12).CopyTo(data, dirOffset);               // Lump offset
            BitConverter.GetBytes(lumpData.Length).CopyTo(data, dirOffset + 4); // Lump size

            var nameBytes = new byte[8];
            Encoding.ASCII.GetBytes(lumpName).CopyTo(nameBytes, 0);
            nameBytes.CopyTo(data, dirOffset + 8);

            return data;
        }

        private static byte[] CreateWadWithFlatSection(string flatName, byte[] flatData)
        {
            // Header + F_START marker entry + flat data + flat entry + F_END marker entry
            var data = new byte[12 + 4096 + 48]; // Header + flat + 3 dir entries

            // Header
            Encoding.ASCII.GetBytes("PWAD").CopyTo(data, 0);
            BitConverter.GetBytes(3).CopyTo(data, 4);                // 3 lumps
            BitConverter.GetBytes(12 + flatData.Length).CopyTo(data, 8); // Directory offset

            // Flat data at offset 12
            flatData.CopyTo(data, 12);

            // Directory entries at offset 12 + 4096
            int dirOffset = 12 + flatData.Length;

            // F_START (marker, size 0)
            BitConverter.GetBytes(0).CopyTo(data, dirOffset);
            BitConverter.GetBytes(0).CopyTo(data, dirOffset + 4);
            Encoding.ASCII.GetBytes("F_START\0").CopyTo(data, dirOffset + 8);

            // Flat
            BitConverter.GetBytes(12).CopyTo(data, dirOffset + 16);
            BitConverter.GetBytes(flatData.Length).CopyTo(data, dirOffset + 20);
            var nameBytes = new byte[8];
            Encoding.ASCII.GetBytes(flatName).CopyTo(nameBytes, 0);
            nameBytes.CopyTo(data, dirOffset + 24);

            // F_END (marker, size 0)
            BitConverter.GetBytes(0).CopyTo(data, dirOffset + 32);
            BitConverter.GetBytes(0).CopyTo(data, dirOffset + 36);
            Encoding.ASCII.GetBytes("F_END\0\0\0").CopyTo(data, dirOffset + 40);

            return data;
        }

        #endregion
    }
}
