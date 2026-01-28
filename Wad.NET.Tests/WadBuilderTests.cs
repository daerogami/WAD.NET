using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using WAD.NET.Archives;
using WAD.NET.Authoring;
using WAD.NET.Enums;

namespace WAD.NET.Tests
{
    public class WadBuilderTests
    {
        [Fact]
        public void RoundTrip_CreateAndReadBack()
        {
            var builder = new WadBuilder(WadType.PWAD);
            builder.AddLump("TEST", new byte[] { 0x01, 0x02, 0x03 });
            builder.AddMarker("F_START");
            builder.AddLump("FLAT1", new byte[] { 0xAA, 0xBB });
            builder.AddMarker("F_END");

            using var ms = new MemoryStream();
            builder.WriteTo(ms);
            ms.Position = 0;

            using var reader = new WadArchiveReader(ms, leaveOpen: true, wadName: "test.wad");
            var entries = reader.GetEntries().ToList();

            Assert.Equal(4, entries.Count);
            Assert.Equal("TEST", entries[0].Name);
            Assert.Equal(3, entries[0].Size);
            Assert.Equal("F_START", entries[1].Name);
            Assert.Equal(0, entries[1].Size);
            Assert.Equal("FLAT1", entries[2].Name);
            Assert.Equal(2, entries[2].Size);
            Assert.Equal("F_END", entries[3].Name);

            var testData = reader.ReadLump(entries[0]);
            Assert.Equal(new byte[] { 0x01, 0x02, 0x03 }, testData);
        }

        [Fact]
        public void AddLump_AddsToDirectory()
        {
            var builder = new WadBuilder();
            builder.AddLump("LUMP1", new byte[] { 1 });
            builder.AddLump("LUMP2", new byte[] { 2, 3 });

            var dir = builder.GetDirectory();
            Assert.Equal(2, dir.Count);
            Assert.Equal("LUMP1", dir[0].Name);
            Assert.Equal(1, dir[0].Size);
            Assert.Equal("LUMP2", dir[1].Name);
            Assert.Equal(2, dir[1].Size);
        }

        [Fact]
        public void AddMarker_ZeroSize()
        {
            var builder = new WadBuilder();
            builder.AddMarker("F_START");

            var dir = builder.GetDirectory();
            Assert.Single(dir);
            Assert.Equal("F_START", dir[0].Name);
            Assert.Equal(0, dir[0].Size);
        }

        [Fact]
        public void ReplaceLump_ChangesData()
        {
            var builder = new WadBuilder();
            builder.AddLump("TEST", new byte[] { 1, 2, 3 });
            builder.ReplaceLump("TEST", new byte[] { 4, 5 });

            var dir = builder.GetDirectory();
            Assert.Single(dir);
            Assert.Equal(2, dir[0].Size);

            // Verify by round-trip
            using var ms = new MemoryStream();
            builder.WriteTo(ms);
            ms.Position = 0;
            using var reader = new WadArchiveReader(ms, leaveOpen: true);
            var data = reader.ReadLump(reader.GetEntries().First());
            Assert.Equal(new byte[] { 4, 5 }, data);
        }

        [Fact]
        public void ReplaceLump_MissingLump_ThrowsKeyNotFoundException()
        {
            var builder = new WadBuilder();
            Assert.Throws<KeyNotFoundException>(() =>
                builder.ReplaceLump("MISSING", new byte[] { 1 }));
        }

        [Fact]
        public void RemoveLump_RemovesFromDirectory()
        {
            var builder = new WadBuilder();
            builder.AddLump("KEEP", new byte[] { 1 });
            builder.AddLump("REMOVE", new byte[] { 2 });
            builder.RemoveLump("REMOVE");

            var dir = builder.GetDirectory();
            Assert.Single(dir);
            Assert.Equal("KEEP", dir[0].Name);
        }

        [Fact]
        public void InsertLump_AtCorrectPosition()
        {
            var builder = new WadBuilder();
            builder.AddLump("FIRST", new byte[] { 1 });
            builder.AddLump("THIRD", new byte[] { 3 });
            builder.InsertLump(1, "SECOND", new byte[] { 2 });

            var dir = builder.GetDirectory();
            Assert.Equal(3, dir.Count);
            Assert.Equal("FIRST", dir[0].Name);
            Assert.Equal("SECOND", dir[1].Name);
            Assert.Equal("THIRD", dir[2].Name);
        }

        [Fact]
        public void GetDirectory_ReflectsCurrentState()
        {
            var builder = new WadBuilder();
            Assert.Empty(builder.GetDirectory());

            builder.AddLump("A", new byte[] { 1 });
            Assert.Single(builder.GetDirectory());

            builder.AddLump("B", new byte[] { 2 });
            Assert.Equal(2, builder.GetDirectory().Count);

            builder.RemoveLump("A");
            Assert.Single(builder.GetDirectory());
            Assert.Equal("B", builder.GetDirectory()[0].Name);
        }

        [Fact]
        public void LumpNameValidation_TooLong_Throws()
        {
            var builder = new WadBuilder();
            Assert.Throws<ArgumentException>(() =>
                builder.AddLump("TOOLONGNAME", new byte[] { 1 }));
        }

        [Fact]
        public void LumpNameValidation_Empty_Throws()
        {
            var builder = new WadBuilder();
            Assert.Throws<ArgumentException>(() =>
                builder.AddLump("", new byte[] { 1 }));
        }

        [Fact]
        public void LumpNameValidation_Max8Chars_Works()
        {
            var builder = new WadBuilder();
            builder.AddLump("ABCDEFGH", new byte[] { 1 });

            Assert.Equal("ABCDEFGH", builder.GetDirectory()[0].Name);
        }

        [Fact]
        public void PWadType_WritesCorrectHeader()
        {
            var builder = new WadBuilder(WadType.PWAD);
            builder.AddLump("TEST", new byte[] { 1 });

            using var ms = new MemoryStream();
            builder.WriteTo(ms);
            ms.Position = 0;

            var magic = Encoding.ASCII.GetString(ms.ToArray(), 0, 4);
            Assert.Equal("PWAD", magic);
        }

        [Fact]
        public void IWadType_WritesCorrectHeader()
        {
            var builder = new WadBuilder(WadType.IWAD);
            builder.AddLump("TEST", new byte[] { 1 });

            using var ms = new MemoryStream();
            builder.WriteTo(ms);
            ms.Position = 0;

            var magic = Encoding.ASCII.GetString(ms.ToArray(), 0, 4);
            Assert.Equal("IWAD", magic);
        }

        [Fact]
        public void FluentChaining_Works()
        {
            var builder = new WadBuilder()
                .AddLump("A", new byte[] { 1 })
                .AddMarker("B")
                .AddLump("C", new byte[] { 2 });

            Assert.Equal(3, builder.GetDirectory().Count);
        }

        [Fact]
        public void FromArchive_RoundTrips()
        {
            // Build a WAD
            var original = new WadBuilder(WadType.PWAD);
            original.AddLump("LUMP1", new byte[] { 10, 20, 30 });
            original.AddMarker("MARKER");
            original.AddLump("LUMP2", new byte[] { 40, 50 });

            using var ms1 = new MemoryStream();
            original.WriteTo(ms1);
            ms1.Position = 0;

            // Read it back
            using var reader = new WadArchiveReader(ms1, leaveOpen: true, wadName: "test.wad");

            // Build from archive
            var rebuilt = WadBuilder.FromArchive(reader);

            using var ms2 = new MemoryStream();
            rebuilt.WriteTo(ms2);
            ms2.Position = 0;

            using var reader2 = new WadArchiveReader(ms2, leaveOpen: true, wadName: "test2.wad");
            var entries = reader2.GetEntries().ToList();

            Assert.Equal(3, entries.Count);
            Assert.Equal("LUMP1", entries[0].Name);
            Assert.Equal("MARKER", entries[1].Name);
            Assert.Equal("LUMP2", entries[2].Name);

            var data = reader2.ReadLump(entries[0]);
            Assert.Equal(new byte[] { 10, 20, 30 }, data);
        }
    }
}
