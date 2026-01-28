using System;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using WAD.NET.Archives;
using WAD.NET.Authoring;
using WAD.NET.Composite;
using WAD.NET.Enums;
using WAD.NET.Validation;

namespace WAD.NET.Tests
{
    public class ResourceDependencyValidatorTests
    {
        private readonly ResourceDependencyValidator _validator = new ResourceDependencyValidator();

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

        private static byte[] MakeDecorate(string content) => Encoding.UTF8.GetBytes(content);

        private static byte[] MakeSndInfo(string content) => Encoding.UTF8.GetBytes(content);

        private static byte[] MakePnames(params string[] names)
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            writer.Write(names.Length);
            foreach (var name in names)
            {
                var padded = new byte[8];
                var bytes = Encoding.ASCII.GetBytes(name.ToUpperInvariant());
                Array.Copy(bytes, padded, Math.Min(bytes.Length, 8));
                writer.Write(padded);
            }
            return ms.ToArray();
        }

        private static byte[] MakeTexture1(int pnamesCount, params (string Name, short[] PatchIndices)[] textures)
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            // Texture count
            writer.Write(textures.Length);

            // Reserve space for offsets
            var offsetsPosition = ms.Position;
            for (int i = 0; i < textures.Length; i++)
                writer.Write(0); // placeholder

            var offsets = new int[textures.Length];
            for (int t = 0; t < textures.Length; t++)
            {
                offsets[t] = (int)ms.Position;
                var tex = textures[t];

                // Name (8 bytes)
                var nameBytes = new byte[8];
                var nb = Encoding.ASCII.GetBytes(tex.Name.ToUpperInvariant());
                Array.Copy(nb, nameBytes, Math.Min(nb.Length, 8));
                writer.Write(nameBytes);

                // masked (4), width (2), height (2), columnDirectory (4)
                writer.Write(0);       // masked
                writer.Write((short)64); // width
                writer.Write((short)64); // height
                writer.Write(0);       // columnDirectory

                // patchCount
                writer.Write((short)tex.PatchIndices.Length);

                // patches: originX(2), originY(2), patchIndex(2), stepDir(2), colorMap(2)
                foreach (var patchIndex in tex.PatchIndices)
                {
                    writer.Write((short)0);          // originX
                    writer.Write((short)0);          // originY
                    writer.Write(patchIndex);        // patchIndex
                    writer.Write((short)0);          // stepDir
                    writer.Write((short)0);          // colorMap
                }
            }

            // Patch offsets back
            var end = ms.Position;
            ms.Position = offsetsPosition;
            foreach (var offset in offsets)
                writer.Write(offset);
            ms.Position = end;

            return ms.ToArray();
        }

        [Fact]
        public void MissingDecorateSprites_ProduceWarnings()
        {
            var decorate = MakeDecorate(@"
                Actor TestMonster : DoomImp
                {
                    States
                    {
                        Spawn:
                            POSS A 10 A_Look
                            Loop
                    }
                }");

            using var stream = BuildWad(WadType.PWAD, ("DECORATE", decorate));
            using var reader = new WadArchiveReader(stream, leaveOpen: true, wadName: "test.wad");

            var result = _validator.Validate(reader);

            Assert.Contains(result.Issues, i =>
                i.Category == "Sprite" &&
                i.Severity == IssueSeverity.Warning &&
                i.ReferencedName == "POSS");
        }

        [Fact]
        public void PresentDecorateSprites_ProduceNoIssues()
        {
            var decorate = MakeDecorate(@"
                Actor TestMonster : DoomImp
                {
                    States
                    {
                        Spawn:
                            POSS A 10 A_Look
                            Loop
                    }
                }");

            using var stream = BuildWad(WadType.PWAD,
                ("DECORATE", decorate),
                ("POSSA0", new byte[] { 1, 2, 3 }));
            using var reader = new WadArchiveReader(stream, leaveOpen: true, wadName: "test.wad");

            var result = _validator.Validate(reader);

            Assert.DoesNotContain(result.Issues, i =>
                i.Category == "Sprite" && i.ReferencedName == "POSS");
        }

        [Fact]
        public void MissingSndInfoSounds_ProduceWarnings()
        {
            var sndinfo = MakeSndInfo("weapons/pistol DSPISTOL");

            using var stream = BuildWad(WadType.PWAD, ("SNDINFO", sndinfo));
            using var reader = new WadArchiveReader(stream, leaveOpen: true, wadName: "test.wad");

            var result = _validator.Validate(reader);

            Assert.Contains(result.Issues, i =>
                i.Category == "Sound" &&
                i.Severity == IssueSeverity.Warning &&
                i.ReferencedName == "DSPISTOL");
        }

        [Fact]
        public void MissingPnamesPatches_ProduceErrors()
        {
            var pnames = MakePnames("WALL01", "WALL02");

            using var stream = BuildWad(WadType.PWAD, ("PNAMES", pnames));
            using var reader = new WadArchiveReader(stream, leaveOpen: true, wadName: "test.wad");

            var result = _validator.Validate(reader);

            Assert.Contains(result.Issues, i =>
                i.Category == "Patch" &&
                i.Severity == IssueSeverity.Error &&
                i.ReferencedName == "WALL01");
            Assert.Contains(result.Issues, i =>
                i.Category == "Patch" &&
                i.Severity == IssueSeverity.Error &&
                i.ReferencedName == "WALL02");
            Assert.False(result.IsValid);
        }

        [Fact]
        public void OutOfRangeTexture1PatchIndices_ProduceErrors()
        {
            var pnames = MakePnames("WALL01");
            var texture1 = MakeTexture1(1,
                ("BRICK1", new short[] { 0, 5 })); // index 5 is out of range (only 1 pname)

            using var stream = BuildWad(WadType.PWAD,
                ("PNAMES", pnames),
                ("TEXTURE1", texture1));
            using var reader = new WadArchiveReader(stream, leaveOpen: true, wadName: "test.wad");

            var result = _validator.Validate(reader);

            Assert.Contains(result.Issues, i =>
                i.Category == "Patch" &&
                i.Severity == IssueSeverity.Error &&
                i.Source.Contains("TEXTURE1") &&
                i.ReferencedName.Contains("5"));
            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidArchive_AllDependenciesPresent_IsValid()
        {
            var pnames = MakePnames("WALL01");
            var texture1 = MakeTexture1(1, ("BRICK1", new short[] { 0 }));

            using var stream = BuildWad(WadType.PWAD,
                ("PNAMES", pnames),
                ("TEXTURE1", texture1),
                ("WALL01", new byte[] { 1, 2, 3 }));
            using var reader = new WadArchiveReader(stream, leaveOpen: true, wadName: "test.wad");

            var result = _validator.Validate(reader);

            Assert.True(result.IsValid);
            // No patch errors
            Assert.DoesNotContain(result.Issues, i =>
                i.Category == "Patch" && i.Severity == IssueSeverity.Error);
        }

        [Fact]
        public void Validate_WorksWithCompositeArchive()
        {
            var decorate = MakeDecorate(@"
                Actor MyGuy : DoomImp
                {
                    States
                    {
                        Spawn:
                            XYZQ A 10
                            Loop
                    }
                }");

            using var s1 = BuildWad(WadType.IWAD, ("PLAYPAL", new byte[] { 1 }));
            using var s2 = BuildWad(WadType.PWAD, ("DECORATE", decorate));
            using var r1 = new WadArchiveReader(s1, leaveOpen: true, wadName: "base.wad");
            using var r2 = new WadArchiveReader(s2, leaveOpen: true, wadName: "mod.wad");

            using var composite = CompositeArchive.Load(r1, r2);
            var result = _validator.Validate(composite);

            Assert.Contains(result.Issues, i =>
                i.Category == "Sprite" &&
                i.ReferencedName == "XYZQ");
        }

        [Fact]
        public void NoResourceLumps_ProducesNoIssues()
        {
            using var stream = BuildWad(WadType.PWAD,
                ("PLAYPAL", new byte[] { 1, 2, 3 }));
            using var reader = new WadArchiveReader(stream, leaveOpen: true, wadName: "test.wad");

            var result = _validator.Validate(reader);

            Assert.Empty(result.Issues);
            Assert.True(result.IsValid);
        }
    }
}
