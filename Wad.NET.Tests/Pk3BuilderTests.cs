using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Xunit;
using WAD.NET.Authoring;

namespace WAD.NET.Tests
{
    public class Pk3BuilderTests
    {
        [Fact]
        public void CreatePk3_WritesValidZip()
        {
            var builder = new Pk3Builder();
            builder.AddEntry("maps/MAP01.wad", new byte[] { 1, 2, 3 });
            builder.AddEntry("sprites/PLAYA1.lmp", new byte[] { 4, 5 });

            using var ms = new MemoryStream();
            builder.WriteTo(ms);
            ms.Position = 0;

            // Verify it's a valid ZIP by opening it
            using var zip = new ZipArchive(ms, ZipArchiveMode.Read);
            Assert.Equal(2, zip.Entries.Count);
            var entryNames = zip.Entries.Select(e => e.FullName).OrderBy(n => n).ToList();
            Assert.Contains("maps/MAP01.wad", entryNames);
            Assert.Contains("sprites/PLAYA1.lmp", entryNames);
        }

        [Fact]
        public void AddEntry_DuplicatePath_Throws()
        {
            var builder = new Pk3Builder();
            builder.AddEntry("maps/MAP01.wad", new byte[] { 1 });

            Assert.Throws<ArgumentException>(() =>
                builder.AddEntry("maps/MAP01.wad", new byte[] { 2 }));
        }

        [Fact]
        public void ReplaceEntry_ChangesData()
        {
            var builder = new Pk3Builder();
            builder.AddEntry("test.txt", new byte[] { 1, 2, 3 });
            builder.ReplaceEntry("test.txt", new byte[] { 4, 5 });

            var entries = builder.GetEntries();
            Assert.Single(entries);
            Assert.Equal(2, entries[0].Size);
        }

        [Fact]
        public void ReplaceEntry_MissingEntry_ThrowsKeyNotFoundException()
        {
            var builder = new Pk3Builder();
            Assert.Throws<KeyNotFoundException>(() =>
                builder.ReplaceEntry("nonexist.txt", new byte[] { 1 }));
        }

        [Fact]
        public void RemoveEntry_RemovesFromEntries()
        {
            var builder = new Pk3Builder();
            builder.AddEntry("keep.txt", new byte[] { 1 });
            builder.AddEntry("remove.txt", new byte[] { 2 });
            builder.RemoveEntry("remove.txt");

            var entries = builder.GetEntries();
            Assert.Single(entries);
            Assert.Equal("keep.txt", entries[0].Path);
        }

        [Fact]
        public void PathNormalization_BackslashesToForwardSlashes()
        {
            var builder = new Pk3Builder();
            builder.AddEntry(@"maps\MAP01.wad", new byte[] { 1 });

            var entries = builder.GetEntries();
            Assert.Equal("maps/MAP01.wad", entries[0].Path);
        }

        [Fact]
        public void PathNormalization_LeadingSlashTrimmed()
        {
            var builder = new Pk3Builder();
            builder.AddEntry("/maps/MAP01.wad", new byte[] { 1 });

            var entries = builder.GetEntries();
            Assert.Equal("maps/MAP01.wad", entries[0].Path);
        }

        [Fact]
        public void GetEntries_ReflectsState()
        {
            var builder = new Pk3Builder();
            Assert.Empty(builder.GetEntries());

            builder.AddEntry("a.txt", new byte[] { 1 });
            Assert.Single(builder.GetEntries());

            builder.AddEntry("b.txt", new byte[] { 2 });
            Assert.Equal(2, builder.GetEntries().Count);

            builder.RemoveEntry("a.txt");
            Assert.Single(builder.GetEntries());
        }

        [Fact]
        public void ReplaceEntry_NormalizedPath_Works()
        {
            var builder = new Pk3Builder();
            builder.AddEntry("maps/MAP01.wad", new byte[] { 1 });
            // Replace using backslash path
            builder.ReplaceEntry(@"maps\MAP01.wad", new byte[] { 2, 3 });

            var entries = builder.GetEntries();
            Assert.Single(entries);
            Assert.Equal(2, entries[0].Size);
        }

        [Fact]
        public void WrittenZip_ContainsCorrectData()
        {
            var data = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
            var builder = new Pk3Builder();
            builder.AddEntry("test.bin", data);

            using var ms = new MemoryStream();
            builder.WriteTo(ms);
            ms.Position = 0;

            using var zip = new ZipArchive(ms, ZipArchiveMode.Read);
            var entry = zip.Entries[0];
            using var entryStream = entry.Open();
            using var readMs = new MemoryStream();
            entryStream.CopyTo(readMs);
            Assert.Equal(data, readMs.ToArray());
        }
    }
}
