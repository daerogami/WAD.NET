using System.Linq;
using Xunit;
using WAD.NET.Archives;
using WAD.NET.Concrete;
using WAD.NET.Enums;
using WAD.NET.Maps;
using WAD.NET.Tests.Infrastructure;

namespace WAD.NET.Tests.RealWorld
{
    /// <summary>
    /// Tests against real IWAD files to validate parsing correctness.
    /// These tests are skipped if the WAD files are not configured.
    /// </summary>
    public class IwadTests : RealWorldWadTestBase
    {
        private static WadPaths Paths => WadTestConfiguration.Paths;

        #region DOOM.WAD Tests

        [SkippableFact]
        public void Doom_ShouldBeDetectedAsIwad()
        {
            var path = RequireWad("Doom", Paths.Doom);

            using var reader = new WadReader(path);
            var wad = reader.ReadWad();

            Assert.Equal(WadType.IWAD, wad.WadType);
        }

        [SkippableFact]
        public void Doom_ShouldHaveExpectedLumpCount()
        {
            var path = RequireWad("Doom", Paths.Doom);

            using var reader = new WadArchiveReader(path);
            var entries = reader.GetEntries().ToList();

            // DOOM.WAD has approximately 2194-2306 lumps depending on version
            // (Original: ~2194, Unity port: ~2306)
            Assert.InRange(entries.Count, 2100, 2400);
        }

        [SkippableFact]
        public void Doom_ShouldContainE1M1()
        {
            var path = RequireWad("Doom", Paths.Doom);

            using var reader = new WadReader(path);
            var wad = reader.ReadWad();
            var mapReader = new MapReader();

            var mapNames = mapReader.GetMapNames(wad);

            Assert.Contains("E1M1", mapNames);
        }

        [SkippableFact]
        public void Doom_ShouldHaveAllEpisode1Maps()
        {
            var path = RequireWad("Doom", Paths.Doom);

            using var reader = new WadReader(path);
            var wad = reader.ReadWad();
            var mapReader = new MapReader();

            var mapNames = mapReader.GetMapNames(wad).ToList();

            for (int m = 1; m <= 9; m++)
            {
                Assert.Contains($"E1M{m}", mapNames);
            }
        }

        [SkippableFact]
        public void Doom_E1M1_ShouldHaveValidMapData()
        {
            var path = RequireWad("Doom", Paths.Doom);

            using var reader = new WadReader(path);
            var wad = reader.ReadWad();
            var mapReader = new MapReader();

            var map = mapReader.ReadMap(wad, "E1M1");

            Assert.NotNull(map);
            Assert.Equal(MapFormat.Doom, map.Format);
            Assert.True(map.ThingCount > 0, "E1M1 should have things");
            Assert.True(map.VertexCount > 0, "E1M1 should have vertices");
            Assert.True(map.LinedefCount > 0, "E1M1 should have linedefs");
            Assert.True(map.SectorCount > 0, "E1M1 should have sectors");
        }

        [SkippableFact]
        public void Doom_ShouldContainPlaypal()
        {
            var path = RequireWad("Doom", Paths.Doom);

            using var reader = new WadArchiveReader(path);

            Assert.True(reader.Contains("PLAYPAL"));
            var entry = reader.GetEntry("PLAYPAL");
            Assert.NotNull(entry);
            Assert.Equal(10752, entry!.Size); // 14 palettes * 768 bytes
        }

        [SkippableFact]
        public void Doom_ShouldContainColormap()
        {
            var path = RequireWad("Doom", Paths.Doom);

            using var reader = new WadArchiveReader(path);

            Assert.True(reader.Contains("COLORMAP"));
            var entry = reader.GetEntry("COLORMAP");
            Assert.NotNull(entry);
            Assert.Equal(8704, entry!.Size); // 34 colormaps * 256 bytes
        }

        [SkippableFact]
        public void Doom_ShouldContainTextureLumps()
        {
            var path = RequireWad("Doom", Paths.Doom);

            using var reader = new WadArchiveReader(path);

            Assert.True(reader.Contains("TEXTURE1"));
            Assert.True(reader.Contains("PNAMES"));
        }

        #endregion

        #region DOOM2.WAD Tests

        [SkippableFact]
        public void Doom2_ShouldBeDetectedAsIwad()
        {
            var path = RequireWad("Doom2", Paths.Doom2);

            using var reader = new WadReader(path);
            var wad = reader.ReadWad();

            Assert.Equal(WadType.IWAD, wad.WadType);
        }

        [SkippableFact]
        public void Doom2_ShouldHaveExpectedLumpCount()
        {
            var path = RequireWad("Doom2", Paths.Doom2);

            using var reader = new WadArchiveReader(path);
            var entries = reader.GetEntries().ToList();

            // DOOM2.WAD has approximately 2919 lumps
            Assert.InRange(entries.Count, 2800, 3100);
        }

        [SkippableFact]
        public void Doom2_ShouldContainMAP01()
        {
            var path = RequireWad("Doom2", Paths.Doom2);

            using var reader = new WadReader(path);
            var wad = reader.ReadWad();
            var mapReader = new MapReader();

            var mapNames = mapReader.GetMapNames(wad);

            Assert.Contains("MAP01", mapNames);
        }

        [SkippableFact]
        public void Doom2_ShouldHaveAll32Maps()
        {
            var path = RequireWad("Doom2", Paths.Doom2);

            using var reader = new WadReader(path);
            var wad = reader.ReadWad();
            var mapReader = new MapReader();

            var mapNames = mapReader.GetMapNames(wad).ToList();

            // DOOM 2 has 32 maps (MAP01-MAP32)
            Assert.Equal(32, mapNames.Count);

            for (int m = 1; m <= 32; m++)
            {
                var expectedName = $"MAP{m:D2}";
                Assert.Contains(expectedName, mapNames);
            }
        }

        [SkippableFact]
        public void Doom2_MAP01_ShouldHaveValidMapData()
        {
            var path = RequireWad("Doom2", Paths.Doom2);

            using var reader = new WadReader(path);
            var wad = reader.ReadWad();
            var mapReader = new MapReader();

            var map = mapReader.ReadMap(wad, "MAP01");

            Assert.NotNull(map);
            Assert.Equal(MapFormat.Doom, map.Format);
            Assert.True(map.ThingCount > 0, "MAP01 should have things");
        }

        [SkippableFact]
        public void Doom2_ShouldContainSuperShotgunSound()
        {
            var path = RequireWad("Doom2", Paths.Doom2);

            using var reader = new WadArchiveReader(path);

            // DOOM 2 adds the super shotgun
            Assert.True(reader.Contains("DSDBOPN") || reader.Contains("DSDSHTGN"));
        }

        #endregion

        #region PLUTONIA.WAD Tests

        [SkippableFact]
        public void Plutonia_ShouldBeDetectedAsIwad()
        {
            var path = RequireWad("Plutonia", Paths.Plutonia);

            using var reader = new WadReader(path);
            var wad = reader.ReadWad();

            Assert.Equal(WadType.IWAD, wad.WadType);
        }

        [SkippableFact]
        public void Plutonia_ShouldHave32Maps()
        {
            var path = RequireWad("Plutonia", Paths.Plutonia);

            using var reader = new WadReader(path);
            var wad = reader.ReadWad();
            var mapReader = new MapReader();

            var mapNames = mapReader.GetMapNames(wad).ToList();

            Assert.Equal(32, mapNames.Count);
        }

        #endregion

        #region TNT.WAD Tests

        [SkippableFact]
        public void Tnt_ShouldBeDetectedAsIwad()
        {
            var path = RequireWad("TNT", Paths.Tnt);

            using var reader = new WadReader(path);
            var wad = reader.ReadWad();

            Assert.Equal(WadType.IWAD, wad.WadType);
        }

        [SkippableFact]
        public void Tnt_ShouldHave32Maps()
        {
            var path = RequireWad("TNT", Paths.Tnt);

            using var reader = new WadReader(path);
            var wad = reader.ReadWad();
            var mapReader = new MapReader();

            var mapNames = mapReader.GetMapNames(wad).ToList();

            Assert.Equal(32, mapNames.Count);
        }

        #endregion

        #region HERETIC.WAD Tests

        [SkippableFact]
        public void Heretic_ShouldBeDetectedAsIwad()
        {
            var path = RequireWad("Heretic", Paths.Heretic);

            using var reader = new WadReader(path);
            var wad = reader.ReadWad();

            Assert.Equal(WadType.IWAD, wad.WadType);
        }

        [SkippableFact]
        public void Heretic_ShouldUseEpisodeMapFormat()
        {
            var path = RequireWad("Heretic", Paths.Heretic);

            using var reader = new WadReader(path);
            var wad = reader.ReadWad();
            var mapReader = new MapReader();

            var mapNames = mapReader.GetMapNames(wad);

            // Heretic uses ExMy format like DOOM
            Assert.Contains("E1M1", mapNames);
        }

        #endregion

        #region HEXEN.WAD Tests

        [SkippableFact]
        public void Hexen_ShouldBeDetectedAsIwad()
        {
            var path = RequireWad("Hexen", Paths.Hexen);

            using var reader = new WadReader(path);
            var wad = reader.ReadWad();

            Assert.Equal(WadType.IWAD, wad.WadType);
        }

        [SkippableFact]
        public void Hexen_ShouldUseHexenMapFormat()
        {
            var path = RequireWad("Hexen", Paths.Hexen);

            using var reader = new WadReader(path);
            var wad = reader.ReadWad();
            var mapReader = new MapReader();

            var map = mapReader.ReadMap(wad, "MAP01");

            Assert.NotNull(map);
            Assert.Equal(MapFormat.Hexen, map.Format);
        }

        [SkippableFact]
        public void Hexen_ShouldContainBehaviorLump()
        {
            var path = RequireWad("Hexen", Paths.Hexen);

            using var reader = new WadArchiveReader(path);

            // Hexen maps have BEHAVIOR lumps for ACS
            Assert.True(reader.Contains("BEHAVIOR"));
        }

        #endregion

        #region FREEDOOM Tests

        [SkippableFact]
        public void FreeDoom1_ShouldBeValidIwad()
        {
            var path = RequireWad("FreeDoom1", Paths.FreeDoom1);

            using var reader = new WadReader(path);
            var wad = reader.ReadWad();

            Assert.Equal(WadType.IWAD, wad.WadType);

            var mapReader = new MapReader();
            var mapNames = mapReader.GetMapNames(wad);

            // Freedoom Phase 1 uses E1M1 format
            Assert.Contains("E1M1", mapNames);
        }

        [SkippableFact]
        public void FreeDoom2_ShouldBeValidIwad()
        {
            var path = RequireWad("FreeDoom2", Paths.FreeDoom2);

            using var reader = new WadReader(path);
            var wad = reader.ReadWad();

            Assert.Equal(WadType.IWAD, wad.WadType);

            var mapReader = new MapReader();
            var mapNames = mapReader.GetMapNames(wad);

            // Freedoom Phase 2 uses MAP01 format
            Assert.Contains("MAP01", mapNames);
        }

        #endregion

        #region Archive Reader Tests

        [SkippableFact]
        public void ArchiveFactory_ShouldAutoDetectDoom()
        {
            var path = RequireWad("Doom", Paths.Doom);

            using var reader = ArchiveReaderFactory.Open(path);

            Assert.Equal(ArchiveType.WAD, reader.Type);
        }

        [SkippableFact]
        public void WadArchiveReader_ShouldCategorizeLumps()
        {
            var path = RequireWad("Doom2", Paths.Doom2);

            using var reader = new WadArchiveReader(path);

            var mapLumps = reader.GetEntriesByCategory(LumpCategory.Map).ToList();
            var musicLumps = reader.GetEntriesByCategory(LumpCategory.Music).ToList();

            Assert.NotEmpty(mapLumps);
            Assert.NotEmpty(musicLumps);
        }

        #endregion
    }
}
