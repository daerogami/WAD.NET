using System.IO;
using System.Linq;
using Xunit;
using WAD.NET.Archives;
using WAD.NET.Authoring;
using WAD.NET.Composite;
using WAD.NET.Enums;

namespace WAD.NET.Tests
{
    public class CompositeArchiveTests
    {
        private static MemoryStream BuildSimpleWad(WadType type, params (string Name, byte[] Data)[] lumps)
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
        public void SingleArchive_AllLumpsAreBase()
        {
            using var stream = BuildSimpleWad(WadType.IWAD,
                ("PLAYPAL", new byte[] { 1, 2, 3 }),
                ("COLORMAP", new byte[] { 4, 5, 6 }));
            using var reader = new WadArchiveReader(stream, leaveOpen: true, wadName: "base.wad");

            var composite = CompositeArchive.Load(reader);

            Assert.All(composite.EffectiveLumps.Values,
                r => Assert.Equal(LumpResolutionType.Base, r.Resolution));
            Assert.Empty(composite.Overrides);
            Assert.Empty(composite.Additions);
        }

        [Fact]
        public void TwoArchives_OverridesAndAdditionsCorrect()
        {
            using var baseStream = BuildSimpleWad(WadType.IWAD,
                ("PLAYPAL", new byte[] { 1, 2, 3 }),
                ("COLORMAP", new byte[] { 4, 5, 6 }));
            using var patchStream = BuildSimpleWad(WadType.PWAD,
                ("PLAYPAL", new byte[] { 7, 8, 9 }),
                ("NEWLUMP", new byte[] { 10, 11 }));

            using var baseReader = new WadArchiveReader(baseStream, leaveOpen: true, wadName: "base.wad");
            using var patchReader = new WadArchiveReader(patchStream, leaveOpen: true, wadName: "patch.wad");

            var composite = CompositeArchive.Load(baseReader, patchReader);

            Assert.Equal(LumpResolutionType.Override, composite.EffectiveLumps["PLAYPAL"].Resolution);
            Assert.Equal(1, composite.EffectiveLumps["PLAYPAL"].SourceIndex);

            Assert.Equal(LumpResolutionType.Base, composite.EffectiveLumps["COLORMAP"].Resolution);

            Assert.Equal(LumpResolutionType.Added, composite.EffectiveLumps["NEWLUMP"].Resolution);
            Assert.Equal(1, composite.EffectiveLumps["NEWLUMP"].SourceIndex);

            Assert.Single(composite.Overrides);
            Assert.Single(composite.Additions);
        }

        [Fact]
        public void GetOverrideChain_ReturnsFullHistory()
        {
            using var s1 = BuildSimpleWad(WadType.IWAD, ("PLAYPAL", new byte[] { 1 }));
            using var s2 = BuildSimpleWad(WadType.PWAD, ("PLAYPAL", new byte[] { 2 }));
            using var s3 = BuildSimpleWad(WadType.PWAD, ("PLAYPAL", new byte[] { 3 }));

            using var r1 = new WadArchiveReader(s1, leaveOpen: true, wadName: "base.wad");
            using var r2 = new WadArchiveReader(s2, leaveOpen: true, wadName: "p1.wad");
            using var r3 = new WadArchiveReader(s3, leaveOpen: true, wadName: "p2.wad");

            var composite = CompositeArchive.Load(r1, r2, r3);
            var chain = composite.GetOverrideChain("PLAYPAL");

            Assert.Equal(3, chain.Count);
        }

        [Fact]
        public void GetOverrideChain_NonExistentLump_ReturnsEmpty()
        {
            using var s1 = BuildSimpleWad(WadType.IWAD, ("PLAYPAL", new byte[] { 1 }));
            using var r1 = new WadArchiveReader(s1, leaveOpen: true, wadName: "base.wad");

            var composite = CompositeArchive.Load(r1);
            var chain = composite.GetOverrideChain("NONEXIST");

            Assert.Empty(chain);
        }

        [Fact]
        public void GetLumpsFromSource_FiltersCorrectly()
        {
            using var s1 = BuildSimpleWad(WadType.IWAD,
                ("PLAYPAL", new byte[] { 1 }),
                ("COLORMAP", new byte[] { 2 }));
            using var s2 = BuildSimpleWad(WadType.PWAD,
                ("NEWLUMP", new byte[] { 3 }));

            using var r1 = new WadArchiveReader(s1, leaveOpen: true, wadName: "base.wad");
            using var r2 = new WadArchiveReader(s2, leaveOpen: true, wadName: "patch.wad");

            var composite = CompositeArchive.Load(r1, r2);

            var fromBase = composite.GetLumpsFromSource(0).ToList();
            var fromPatch = composite.GetLumpsFromSource(1).ToList();

            Assert.Equal(2, fromBase.Count);
            Assert.Single(fromPatch);
            Assert.Equal("NEWLUMP", fromPatch[0].Lump.Name);
        }

        [Fact]
        public void CaseInsensitiveLumpNameMatching()
        {
            // WadBuilder uppercases names, but the dictionary should be case-insensitive
            using var s1 = BuildSimpleWad(WadType.IWAD, ("PLAYPAL", new byte[] { 1 }));
            using var r1 = new WadArchiveReader(s1, leaveOpen: true, wadName: "base.wad");

            var composite = CompositeArchive.Load(r1);

            Assert.True(composite.EffectiveLumps.ContainsKey("playpal"));
            Assert.True(composite.EffectiveLumps.ContainsKey("PLAYPAL"));
            Assert.True(composite.EffectiveLumps.ContainsKey("Playpal"));
        }

        [Fact]
        public void LoadOrder_ReflectsArchivePaths()
        {
            using var s1 = BuildSimpleWad(WadType.IWAD, ("TEST", new byte[] { 1 }));
            using var s2 = BuildSimpleWad(WadType.PWAD, ("TEST2", new byte[] { 2 }));

            using var r1 = new WadArchiveReader(s1, leaveOpen: true, wadName: "base.wad");
            using var r2 = new WadArchiveReader(s2, leaveOpen: true, wadName: "patch.wad");

            var composite = CompositeArchive.Load(r1, r2);

            Assert.Equal(2, composite.LoadOrder.Count);
            Assert.Equal("base.wad", composite.LoadOrder[0]);
            Assert.Equal("patch.wad", composite.LoadOrder[1]);
        }

        [Fact]
        public void OverriddenLump_HasReference()
        {
            using var s1 = BuildSimpleWad(WadType.IWAD, ("PLAYPAL", new byte[] { 1 }));
            using var s2 = BuildSimpleWad(WadType.PWAD, ("PLAYPAL", new byte[] { 2 }));

            using var r1 = new WadArchiveReader(s1, leaveOpen: true, wadName: "base.wad");
            using var r2 = new WadArchiveReader(s2, leaveOpen: true, wadName: "patch.wad");

            var composite = CompositeArchive.Load(r1, r2);

            var resolved = composite.EffectiveLumps["PLAYPAL"];
            Assert.NotNull(resolved.OverriddenLump);
            Assert.Equal("PLAYPAL", resolved.OverriddenLump!.Name);
        }
    }
}
