using System.IO;
using System.Linq;
using Xunit;
using WAD.NET.Archives;
using WAD.NET.Concrete;
using WAD.NET.Definitions;
using WAD.NET.Enums;
using WAD.NET.Export;
using WAD.NET.Maps;
using WAD.NET.Resources;
using WAD.NET.Tests.Infrastructure;

namespace WAD.NET.Tests.RealWorld
{
    /// <summary>
    /// Tests against real PWAD files (community megawads) to validate parsing correctness.
    /// These tests are skipped if the WAD files are not configured.
    /// </summary>
    public class PwadTests : RealWorldWadTestBase
    {
        private static WadPaths Paths => WadTestConfiguration.Paths;

        #region Eviternity Tests

        [SkippableFact]
        public void Eviternity_ShouldBeDetectedAsPwad()
        {
            var path = RequireWad("Eviternity", Paths.Eviternity);

            using var reader = new WadReader(path);
            var wad = reader.ReadWad();

            Assert.Equal(WadType.PWAD, wad.WadType);
        }

        [SkippableFact]
        public void Eviternity_ShouldHave32Maps()
        {
            var path = RequireWad("Eviternity", Paths.Eviternity);

            using var reader = new WadReader(path);
            var wad = reader.ReadWad();
            var mapReader = new MapReader();

            var mapNames = mapReader.GetMapNames(wad).ToList();

            // Eviternity has 32 maps (standard megawad)
            Assert.InRange(mapNames.Count, 32, 36); // May have bonus maps
        }

        [SkippableFact]
        public void Eviternity_MAP01_ShouldParse()
        {
            var path = RequireWad("Eviternity", Paths.Eviternity);

            using var reader = new WadReader(path);
            var wad = reader.ReadWad();
            var mapReader = new MapReader();

            var map = mapReader.ReadMap(wad, "MAP01");

            Assert.NotNull(map);
            Assert.Equal(MapFormat.Doom, map.Format);
            Assert.True(map.ThingCount > 0);
            Assert.True(map.LinedefCount > 0);
        }

        [SkippableFact]
        public void Eviternity_ShouldHaveCustomTextures()
        {
            var path = RequireWad("Eviternity", Paths.Eviternity);

            using var reader = new WadArchiveReader(path);

            // Eviternity includes custom texture definitions
            Assert.True(reader.Contains("TEXTURE1") || reader.Contains("TEXTURE2"));
        }

        #endregion

        #region Ancient Aliens Tests

        [SkippableFact]
        public void AncientAliens_ShouldBeDetectedAsPwad()
        {
            var path = RequireWad("AncientAliens", Paths.AncientAliens);

            using var reader = new WadReader(path);
            var wad = reader.ReadWad();

            Assert.Equal(WadType.PWAD, wad.WadType);
        }

        [SkippableFact]
        public void AncientAliens_ShouldHaveMaps()
        {
            var path = RequireWad("AncientAliens", Paths.AncientAliens);

            using var reader = new WadReader(path);
            var wad = reader.ReadWad();
            var mapReader = new MapReader();

            var mapNames = mapReader.GetMapNames(wad).ToList();

            Assert.NotEmpty(mapNames);
            Assert.Contains("MAP01", mapNames);
        }

        [SkippableFact]
        public void AncientAliens_ShouldHaveCustomColormap()
        {
            var path = RequireWad("AncientAliens", Paths.AncientAliens);

            using var reader = new WadArchiveReader(path);

            // Ancient Aliens has custom palette/colormap for its unique look
            bool hasColormap = reader.Contains("COLORMAP");
            bool hasPlaypal = reader.Contains("PLAYPAL");

            Assert.True(hasColormap || hasPlaypal, "Should have custom color data");
        }

        #endregion

        #region Sunlust Tests

        [SkippableFact]
        public void Sunlust_ShouldBeDetectedAsPwad()
        {
            var path = RequireWad("Sunlust", Paths.Sunlust);

            using var reader = new WadReader(path);
            var wad = reader.ReadWad();

            Assert.Equal(WadType.PWAD, wad.WadType);
        }

        [SkippableFact]
        public void Sunlust_ShouldHave32Maps()
        {
            var path = RequireWad("Sunlust", Paths.Sunlust);

            using var reader = new WadReader(path);
            var wad = reader.ReadWad();
            var mapReader = new MapReader();

            var mapNames = mapReader.GetMapNames(wad).ToList();

            // Sunlust is a 32-map megawad
            Assert.InRange(mapNames.Count, 32, 35);
        }

        [SkippableFact]
        public void Sunlust_MAP29_ShouldHaveHighThingCount()
        {
            var path = RequireWad("Sunlust", Paths.Sunlust);

            using var reader = new WadReader(path);
            var wad = reader.ReadWad();
            var mapReader = new MapReader();

            var map = mapReader.ReadMap(wad, "MAP29");

            if (map != null)
            {
                // Sunlust MAP29 (Go Fuck Yourself) is notorious for high monster count
                Assert.True(map.ThingCount > 500, "MAP29 should have many things");
            }
        }

        #endregion

        #region Scythe 2 Tests

        [SkippableFact]
        public void Scythe2_ShouldBeDetectedAsPwad()
        {
            var path = RequireWad("Scythe2", Paths.Scythe2);

            using var reader = new WadReader(path);
            var wad = reader.ReadWad();

            Assert.Equal(WadType.PWAD, wad.WadType);
        }

        [SkippableFact]
        public void Scythe2_ShouldHaveMaps()
        {
            var path = RequireWad("Scythe2", Paths.Scythe2);

            using var reader = new WadReader(path);
            var wad = reader.ReadWad();
            var mapReader = new MapReader();

            var mapNames = mapReader.GetMapNames(wad).ToList();

            Assert.NotEmpty(mapNames);
        }

        #endregion

        #region Generic PWAD Tests

        [SkippableFact]
        public void AllConfiguredPwads_ShouldParseWithoutError()
        {
            var pwads = Paths.GetPwads();
            int testedCount = 0;

            foreach (var (name, path) in pwads)
            {
                if (!WadTestConfiguration.IsAvailable(path))
                    continue;

                testedCount++;

                using var reader = new WadReader(path);
                var wad = reader.ReadWad();

                // Should not throw and should be a PWAD
                Assert.Equal(WadType.PWAD, wad.WadType);
                Assert.NotEmpty(wad.Lumps);
            }

            Skip.If(testedCount == 0, "No PWADs configured for testing");
        }

        [SkippableFact]
        public void AllConfiguredPwads_ShouldHaveValidMaps()
        {
            var pwads = Paths.GetPwads();
            int testedCount = 0;

            foreach (var (name, path) in pwads)
            {
                if (!WadTestConfiguration.IsAvailable(path))
                    continue;

                testedCount++;

                using var reader = new WadReader(path);
                var wad = reader.ReadWad();
                var mapReader = new MapReader();

                var maps = mapReader.ReadMaps(wad);

                Assert.NotEmpty(maps);

                // Verify at least one map parses completely
                var firstMap = maps.First();
                Assert.NotNull(firstMap.Name);
                Assert.True(firstMap.ThingCount > 0, $"{name}: First map should have things");
            }

            Skip.If(testedCount == 0, "No PWADs configured for testing");
        }

        #endregion

        #region Archive Reader Tests

        [SkippableFact]
        public void WadArchiveReader_ShouldWorkWithPwads()
        {
            var path = RequireWad("Eviternity", Paths.Eviternity);

            using var reader = new WadArchiveReader(path);

            Assert.Equal(WadType.PWAD, reader.WadType);
            Assert.NotEmpty(reader.GetEntries());
        }

        [SkippableFact]
        public void ArchiveFactory_ShouldAutoDetectPwad()
        {
            var path = RequireWad("Eviternity", Paths.Eviternity);

            using var reader = ArchiveReaderFactory.Open(path);

            Assert.Equal(ArchiveType.WAD, reader.Type);
            Assert.IsType<WadArchiveReader>(reader);
        }

        #endregion

        #region Phase 4: Resource Parsing Tests

        [SkippableFact]
        public void Pwad_ShouldParseCustomFlats()
        {
            var pwads = Paths.GetPwads();
            int testedCount = 0;

            foreach (var (name, path) in pwads)
            {
                if (!WadTestConfiguration.IsAvailable(path))
                    continue;

                testedCount++;

                using var reader = new WadReader(path);
                var wad = reader.ReadWad();

                var flats = wad.Lumps.OfType<FlatLump>().ToList();

                // If this PWAD has custom flats, verify they parse correctly
                if (flats.Count > 0)
                {
                    foreach (var flat in flats.Take(5))
                    {
                        Assert.Equal(4096, flat.Pixels.Length);
                    }
                    return; // Found and tested
                }
            }

            Skip.If(testedCount == 0, "No PWADs configured for testing");
        }

        [SkippableFact]
        public void Pwad_ShouldParseCustomSprites()
        {
            var pwads = Paths.GetPwads();
            int testedCount = 0;

            foreach (var (name, path) in pwads)
            {
                if (!WadTestConfiguration.IsAvailable(path))
                    continue;

                testedCount++;

                using var reader = new WadReader(path);
                var wad = reader.ReadWad();

                var sprites = wad.Lumps.OfType<PictureLump>().ToList();

                // If this PWAD has custom sprites, verify they parse correctly
                if (sprites.Count > 0)
                {
                    foreach (var sprite in sprites.Take(5))
                    {
                        Assert.True(sprite.Picture.Width > 0);
                        Assert.True(sprite.Picture.Height > 0);
                    }
                    return; // Found and tested
                }
            }

            Skip.If(testedCount == 0, "No PWADs configured for testing");
        }

        [SkippableFact]
        public void Pwad_ShouldParseCustomMusic()
        {
            var pwads = Paths.GetPwads();
            int testedCount = 0;

            foreach (var (name, path) in pwads)
            {
                if (!WadTestConfiguration.IsAvailable(path))
                    continue;

                testedCount++;

                using var reader = new WadReader(path);
                var wad = reader.ReadWad();

                var musicLumps = wad.Lumps.OfType<MusicLump>().ToList();

                // If this PWAD has music, verify it parses correctly
                if (musicLumps.Count > 0)
                {
                    foreach (var music in musicLumps.Take(3))
                    {
                        Assert.True(music.IsMus || music.IsMidi || music.RawData.Length > 0,
                            $"Music lump {music.Name} should have valid data");
                    }
                    return; // Found and tested
                }
            }

            Skip.If(testedCount == 0, "No PWADs configured for testing");
        }

        [SkippableFact]
        public void Pwad_WithCustomPalette_ShouldParse()
        {
            var pwads = Paths.GetPwads();
            int testedCount = 0;

            foreach (var (name, path) in pwads)
            {
                if (!WadTestConfiguration.IsAvailable(path))
                    continue;

                testedCount++;

                using var archiveReader = new WadArchiveReader(path);

                var playpalEntry = archiveReader.GetEntry("PLAYPAL");
                if (playpalEntry != null)
                {
                    var data = archiveReader.ReadLump(playpalEntry);
                    var palette = new PaletteLump("PLAYPAL", path, data);

                    Assert.Equal(14, palette.Palettes.Length);
                    return; // Found and tested
                }
            }

            Skip.If(testedCount == 0, "No PWADs with custom palettes configured");
        }

        [SkippableFact]
        public void Pwad_WithCustomTextures_ShouldParse()
        {
            var pwads = Paths.GetPwads();
            int testedCount = 0;

            foreach (var (name, path) in pwads)
            {
                if (!WadTestConfiguration.IsAvailable(path))
                    continue;

                testedCount++;

                using var archiveReader = new WadArchiveReader(path);

                var texture1Entry = archiveReader.GetEntry("TEXTURE1");
                if (texture1Entry != null)
                {
                    var data = archiveReader.ReadLump(texture1Entry);
                    var textures = new TextureLump("TEXTURE1", path, data);

                    Assert.True(textures.Count > 0, $"{name} should have texture definitions");
                    return; // Found and tested
                }
            }

            Skip.If(testedCount == 0, "No PWADs with custom textures configured");
        }

        [SkippableFact]
        public void Pwad_ShouldExportFlatToImage()
        {
            var pwads = Paths.GetPwads();
            int testedCount = 0;

            foreach (var (name, path) in pwads)
            {
                if (!WadTestConfiguration.IsAvailable(path))
                    continue;

                testedCount++;

                using var reader = new WadReader(path);
                var wad = reader.ReadWad();

                var flatLump = wad.Lumps.OfType<FlatLump>().FirstOrDefault();
                var paletteLump = wad.Lumps.OfType<PaletteLump>().FirstOrDefault();

                if (flatLump != null && paletteLump != null)
                {
                    using var output = new MemoryStream();
                    ImageExporter.ExportTga(flatLump, paletteLump.NormalPalette, output);

                    Assert.True(output.Length > 18, "TGA export should produce data");
                    return;
                }
            }

            Skip.If(testedCount == 0, "No PWADs with flats and palettes configured");
        }

        #endregion
    }
}
