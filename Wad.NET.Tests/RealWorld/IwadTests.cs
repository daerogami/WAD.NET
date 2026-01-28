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

        #region Phase 4: Resource Parsing Tests

        [SkippableFact]
        public void Doom_ShouldParsePalette()
        {
            var path = RequireWad("Doom", Paths.Doom);

            using var reader = new WadArchiveReader(path);
            var entry = reader.GetEntry("PLAYPAL");
            Assert.NotNull(entry);

            var data = reader.ReadLump(entry);
            var paletteLump = new PaletteLump("PLAYPAL", path, data);

            Assert.Equal(14, paletteLump.Palettes.Length);
            Assert.NotNull(paletteLump.NormalPalette);
            Assert.Equal(256, paletteLump.NormalPalette.Colors.Length);
        }

        [SkippableFact]
        public void Doom_ShouldParseTextureDefinitions()
        {
            var path = RequireWad("Doom", Paths.Doom);

            using var reader = new WadArchiveReader(path);

            var texture1Entry = reader.GetEntry("TEXTURE1");
            Assert.NotNull(texture1Entry);

            var data = reader.ReadLump(texture1Entry);
            var textureLump = new TextureLump("TEXTURE1", path, data);

            Assert.True(textureLump.Count > 0, "DOOM should have texture definitions");

            // DOOM has well-known textures
            var startan = textureLump.Find("STARTAN1") ?? textureLump.Find("STARTAN2");
            Assert.NotNull(startan);
        }

        [SkippableFact]
        public void Doom_ShouldParsePatchNames()
        {
            var path = RequireWad("Doom", Paths.Doom);

            using var reader = new WadArchiveReader(path);

            var pnamesEntry = reader.GetEntry("PNAMES");
            Assert.NotNull(pnamesEntry);

            var data = reader.ReadLump(pnamesEntry);
            var pnamesLump = new PatchNamesLump("PNAMES", path, data);

            Assert.True(pnamesLump.Count > 0, "DOOM should have patch names");
        }

        [SkippableFact]
        public void Doom_ShouldParseSpriteLumps()
        {
            var path = RequireWad("Doom", Paths.Doom);

            using var reader = new WadReader(path);
            var wad = reader.ReadWad();

            // Find sprite lumps (they should be PictureLumps)
            var spriteLumps = wad.Lumps.OfType<PictureLump>().ToList();

            Assert.NotEmpty(spriteLumps);

            // Verify a known sprite exists and parses correctly
            var playerSprite = spriteLumps.FirstOrDefault(l => l.Name.StartsWith("PLAY"));
            if (playerSprite != null)
            {
                Assert.True(playerSprite.Picture.Width > 0);
                Assert.True(playerSprite.Picture.Height > 0);
            }
        }

        [SkippableFact]
        public void Doom_ShouldParseFlatLumps()
        {
            var path = RequireWad("Doom", Paths.Doom);

            using var reader = new WadReader(path);
            var wad = reader.ReadWad();

            // Find flat lumps
            var flatLumps = wad.Lumps.OfType<FlatLump>().ToList();

            Assert.NotEmpty(flatLumps);

            // All flats should be 64x64
            foreach (var flat in flatLumps.Take(10))
            {
                Assert.Equal(64, FlatLump.Width);
                Assert.Equal(64, FlatLump.Height);
                Assert.Equal(4096, flat.Pixels.Length);
            }
        }

        [SkippableFact]
        public void Doom_ShouldParseMusicLumps()
        {
            var path = RequireWad("Doom", Paths.Doom);

            using var reader = new WadReader(path);
            var wad = reader.ReadWad();

            // Find music lumps
            var musicLumps = wad.Lumps.OfType<MusicLump>().ToList();

            Assert.NotEmpty(musicLumps);

            // D_E1M1 should exist and be MUS format
            var e1m1Music = musicLumps.FirstOrDefault(m => m.Name == "D_E1M1");
            Assert.NotNull(e1m1Music);
            Assert.True(e1m1Music.IsMus, "D_E1M1 should be MUS format");
            Assert.False(e1m1Music.IsMidi);
        }

        [SkippableFact]
        public void Doom_ShouldConvertMusToMidi()
        {
            var path = RequireWad("Doom", Paths.Doom);

            using var reader = new WadReader(path);
            var wad = reader.ReadWad();

            var musicLump = wad.Lumps.OfType<MusicLump>().FirstOrDefault(m => m.IsMus);
            Assert.NotNull(musicLump);

            var midi = musicLump.ToMidi();

            // Verify MIDI header
            Assert.True(midi.Length > 22, "MIDI should have header and track");
            Assert.Equal((byte)'M', midi[0]);
            Assert.Equal((byte)'T', midi[1]);
            Assert.Equal((byte)'h', midi[2]);
            Assert.Equal((byte)'d', midi[3]);
        }

        [SkippableFact]
        public void Doom_ShouldParseSoundLumps()
        {
            var path = RequireWad("Doom", Paths.Doom);

            using var reader = new WadReader(path);
            var wad = reader.ReadWad();

            // Find sound lumps
            var soundLumps = wad.Lumps.OfType<SoundLump>().ToList();

            Assert.NotEmpty(soundLumps);

            // DSPISTOL should exist
            var pistol = soundLumps.FirstOrDefault(s => s.Name == "DSPISTOL");
            Assert.NotNull(pistol);
            Assert.Equal(3, pistol.Format);
            Assert.True(pistol.SampleRate > 0);
            Assert.True(pistol.Samples.Length > 0);
        }

        [SkippableFact]
        public void Doom_ShouldExportSoundToWav()
        {
            var path = RequireWad("Doom", Paths.Doom);

            using var reader = new WadReader(path);
            var wad = reader.ReadWad();

            var soundLump = wad.Lumps.OfType<SoundLump>().FirstOrDefault();
            Assert.NotNull(soundLump);

            using var output = new MemoryStream();
            soundLump.ExportWav(output);

            var wavData = output.ToArray();
            Assert.True(wavData.Length > 44, "WAV should have header and data");
            Assert.Equal((byte)'R', wavData[0]);
            Assert.Equal((byte)'I', wavData[1]);
            Assert.Equal((byte)'F', wavData[2]);
            Assert.Equal((byte)'F', wavData[3]);
        }

        [SkippableFact]
        public void Doom_ResourceManager_ShouldLoadSystemLumps()
        {
            var path = RequireWad("Doom", Paths.Doom);

            using var archiveReader = new WadArchiveReader(path);
            using var resourceManager = new ResourceManager(archiveReader);

            Assert.NotNull(resourceManager.DefaultPalette);
            Assert.NotEmpty(resourceManager.Palettes);
            Assert.NotEmpty(resourceManager.PatchNames);
            Assert.NotEmpty(resourceManager.Textures);
        }

        [SkippableFact]
        public void Doom_ResourceManager_ShouldGetFlat()
        {
            var path = RequireWad("Doom", Paths.Doom);

            using var archiveReader = new WadArchiveReader(path);
            using var resourceManager = new ResourceManager(archiveReader);

            // FLOOR4_8 is a well-known DOOM flat
            var flat = resourceManager.GetFlat("FLOOR4_8");
            Assert.NotNull(flat);
            Assert.Equal(4096, flat.Pixels.Length);
        }

        [SkippableFact]
        public void Doom_ResourceManager_ShouldFindTexture()
        {
            var path = RequireWad("Doom", Paths.Doom);

            using var archiveReader = new WadArchiveReader(path);
            using var resourceManager = new ResourceManager(archiveReader);

            var texture = resourceManager.FindTexture("STARTAN2");
            Assert.NotNull(texture);
            Assert.True(texture.Width > 0);
            Assert.True(texture.Height > 0);
        }

        [SkippableFact]
        public void Doom_ShouldExportFlatToTga()
        {
            var path = RequireWad("Doom", Paths.Doom);

            using var reader = new WadReader(path);
            var wad = reader.ReadWad();

            var paletteLump = wad.Lumps.OfType<PaletteLump>().FirstOrDefault();
            var flatLump = wad.Lumps.OfType<FlatLump>().FirstOrDefault();

            Assert.NotNull(paletteLump);
            Assert.NotNull(flatLump);

            using var output = new MemoryStream();
            ImageExporter.ExportTga(flatLump, paletteLump.NormalPalette, output);

            var tgaData = output.ToArray();

            // TGA header validation
            Assert.True(tgaData.Length > 18, "TGA should have header");
            Assert.Equal(2, tgaData[2]); // Uncompressed true-color
            Assert.Equal(32, tgaData[16]); // 32-bit BGRA
        }

        [SkippableFact]
        public void Doom_ShouldExportSpriteToBmp()
        {
            var path = RequireWad("Doom", Paths.Doom);

            using var reader = new WadReader(path);
            var wad = reader.ReadWad();

            var paletteLump = wad.Lumps.OfType<PaletteLump>().FirstOrDefault();
            var pictureLump = wad.Lumps.OfType<PictureLump>().FirstOrDefault();

            Assert.NotNull(paletteLump);
            Assert.NotNull(pictureLump);

            using var output = new MemoryStream();
            ImageExporter.ExportBmp(pictureLump.Picture, paletteLump.NormalPalette, output);

            var bmpData = output.ToArray();

            // BMP header validation
            Assert.True(bmpData.Length > 54, "BMP should have header");
            Assert.Equal((byte)'B', bmpData[0]);
            Assert.Equal((byte)'M', bmpData[1]);
        }

        #endregion
    }
}
