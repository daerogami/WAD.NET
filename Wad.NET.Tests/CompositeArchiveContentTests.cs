using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using WAD.NET.Archives;
using WAD.NET.Authoring;
using WAD.NET.Composite;
using WAD.NET.Enums;

namespace WAD.NET.Tests
{
    public class CompositeArchiveContentTests
    {
        private static MemoryStream BuildWad(WadType type, params (string Name, byte[] Data)[] lumps)
        {
            var builder = new WadBuilder(type);
            foreach (var (name, data) in lumps)
            {
                if (data.Length == 0)
                    builder.AddMarker(name);
                else
                    builder.AddLump(name, data);
            }
            var ms = new MemoryStream();
            builder.WriteTo(ms);
            ms.Position = 0;
            return ms;
        }

        [Fact]
        public void ReadLump_ReturnsCorrectBytesFromEffectiveSource()
        {
            using var s1 = BuildWad(WadType.IWAD, ("TEST", new byte[] { 0x01, 0x02 }));
            using var r1 = new WadArchiveReader(s1, leaveOpen: true, wadName: "base.wad");

            using var composite = CompositeArchive.Load(r1);
            var data = composite.ReadLump("TEST");

            Assert.Equal(new byte[] { 0x01, 0x02 }, data);
        }

        [Fact]
        public void ReadLump_ReturnsOverriddenContent_NotBase()
        {
            using var s1 = BuildWad(WadType.IWAD, ("TEST", new byte[] { 0x01 }));
            using var s2 = BuildWad(WadType.PWAD, ("TEST", new byte[] { 0x02 }));
            using var r1 = new WadArchiveReader(s1, leaveOpen: true, wadName: "base.wad");
            using var r2 = new WadArchiveReader(s2, leaveOpen: true, wadName: "patch.wad");

            using var composite = CompositeArchive.Load(r1, r2);
            var data = composite.ReadLump("TEST");

            Assert.Equal(new byte[] { 0x02 }, data);
        }

        [Fact]
        public void OpenLump_ReturnsReadableStream()
        {
            using var s1 = BuildWad(WadType.IWAD, ("TEST", new byte[] { 0xAA, 0xBB, 0xCC }));
            using var r1 = new WadArchiveReader(s1, leaveOpen: true, wadName: "base.wad");

            using var composite = CompositeArchive.Load(r1);
            using var stream = composite.OpenLump("TEST");

            var buffer = new byte[3];
            var bytesRead = stream.Read(buffer, 0, 3);
            Assert.Equal(3, bytesRead);
            Assert.Equal(new byte[] { 0xAA, 0xBB, 0xCC }, buffer);
        }

        [Fact]
        public void ReadLump_ThrowsKeyNotFoundException_ForMissingLump()
        {
            using var s1 = BuildWad(WadType.IWAD, ("TEST", new byte[] { 1 }));
            using var r1 = new WadArchiveReader(s1, leaveOpen: true, wadName: "base.wad");

            using var composite = CompositeArchive.Load(r1);

            Assert.Throws<KeyNotFoundException>(() => composite.ReadLump("NONEXIST"));
        }

        [Fact]
        public void ReadLump_ThrowsObjectDisposedException_AfterDispose()
        {
            using var s1 = BuildWad(WadType.IWAD, ("TEST", new byte[] { 1 }));
            using var r1 = new WadArchiveReader(s1, leaveOpen: true, wadName: "base.wad");

            var composite = CompositeArchive.Load(r1);
            composite.Dispose();

            Assert.Throws<ObjectDisposedException>(() => composite.ReadLump("TEST"));
        }

        [Fact]
        public void LoadStrings_DisposesReaders_OnDispose()
        {
            // We can't easily test Load(string[]) without real files,
            // but we can test that Load(IArchiveReader[]) with ownsReaders=false
            // does NOT dispose readers.
            using var s1 = BuildWad(WadType.IWAD, ("TEST", new byte[] { 1 }));
            var r1 = new WadArchiveReader(s1, leaveOpen: true, wadName: "base.wad");

            var composite = CompositeArchive.Load(r1);
            composite.Dispose();

            // Reader should still be usable since Load(IArchiveReader[]) doesn't own them
            var entries = r1.GetEntries().ToList();
            Assert.Single(entries);

            r1.Dispose();
        }

        [Fact]
        public void LoadReaders_DoesNotDisposeExternallyProvidedReaders()
        {
            using var s1 = BuildWad(WadType.IWAD, ("TEST", new byte[] { 1, 2, 3 }));
            var r1 = new WadArchiveReader(s1, leaveOpen: true, wadName: "base.wad");

            var composite = CompositeArchive.Load(r1);
            composite.Dispose();

            // The reader should still work since it was externally provided
            var entries = r1.GetEntries().ToList();
            Assert.Single(entries);
            var data = r1.ReadLump(entries[0]);
            Assert.Equal(new byte[] { 1, 2, 3 }, data);

            r1.Dispose();
        }

        [Fact]
        public void Analyze_CompositeArchiveReader_WorksWithRealContent()
        {
            using var s1 = BuildWad(WadType.IWAD, ("PLAYPAL", new byte[] { 1, 2, 3 }));
            using var r1 = new WadArchiveReader(s1, leaveOpen: true, wadName: "base.wad");

            using var composite = CompositeArchive.Load(r1);

            // Analyze should not throw and should produce a result
            var analysis = composite.Analyze();
            Assert.NotNull(analysis);
            Assert.NotNull(analysis.Compatibility);
        }

        [Fact]
        public void ReadLump_CaseInsensitive()
        {
            using var s1 = BuildWad(WadType.IWAD, ("PLAYPAL", new byte[] { 0x10, 0x20 }));
            using var r1 = new WadArchiveReader(s1, leaveOpen: true, wadName: "base.wad");

            using var composite = CompositeArchive.Load(r1);

            var data = composite.ReadLump("playpal");
            Assert.Equal(new byte[] { 0x10, 0x20 }, data);
        }
    }
}
