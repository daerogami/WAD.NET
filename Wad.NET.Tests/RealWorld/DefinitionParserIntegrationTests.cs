using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using WAD.NET.Archives;
using WAD.NET.Concrete;
using WAD.NET.Enums;
using WAD.NET.Maps;
using WAD.NET.Parsers.Dehacked;
using WAD.NET.Parsers.MapInfo;
using WAD.NET.Parsers.SndInfo;
using WAD.NET.Tests.Infrastructure;
using WAD.NET.UDMF;
using Xunit;
using Xunit.Abstractions;

namespace WAD.NET.Tests.RealWorld
{
    /// <summary>
    /// Integration tests that parse MAPINFO, SNDINFO, DEHACKED, and UDMF from real
    /// PK3s and WADs. Exercises the text-definition parsers against real-world data.
    /// Skipped when WAD/PK3 paths are not configured.
    /// </summary>
    public class DefinitionParserIntegrationTests : RealWorldWadTestBase
    {
        private static WadPaths Paths => WadTestConfiguration.Paths;
        private readonly ITestOutputHelper _output;

        public DefinitionParserIntegrationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        #region Helpers

        private static string? ReadEntryAsText(Pk3Reader reader, LumpEntry entry)
        {
            try
            {
                var data = reader.ReadLump(entry);
                var text = Encoding.UTF8.GetString(data);

                // Skip binary files
                if (text.Contains('\0'))
                    return null;

                return text;
            }
            catch
            {
                return null;
            }
        }

        private static string? ReadEntryAsText(WadArchiveReader reader, LumpEntry entry)
        {
            try
            {
                var data = reader.ReadLump(entry);
                var text = Encoding.UTF8.GetString(data);

                if (text.Contains('\0'))
                    return null;

                return text;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Finds MAPINFO or ZMAPINFO entries in a PK3.
        /// Includes root-level MAPINFO lumps and files under mapinfo/ folders
        /// (GZDoom uses include directives pointing to mapinfo/*.txt).
        /// </summary>
        private static List<(string path, string content)> ExtractMapInfoSources(Pk3Reader reader)
        {
            var sources = new List<(string path, string content)>();

            foreach (var entry in reader.GetEntries())
            {
                var nameUpper = Path.GetFileNameWithoutExtension(entry.FullPath).ToUpperInvariant();
                var upperPath = entry.FullPath.ToUpperInvariant().Replace('\\', '/');

                bool isMapInfo =
                    // Root-level MAPINFO/ZMAPINFO lump
                    nameUpper is "MAPINFO" or "ZMAPINFO" or "UMAPINFO" ||
                    // Files under mapinfo/ folder (included by root MAPINFO)
                    upperPath.StartsWith("MAPINFO/");

                if (!isMapInfo || entry.Size == 0)
                    continue;

                var text = ReadEntryAsText(reader, entry);
                if (text != null)
                    sources.Add((entry.FullPath, text));
            }

            return sources;
        }

        /// <summary>
        /// Finds SNDINFO entries in a PK3.
        /// </summary>
        private static List<(string path, string content)> ExtractSndInfoSources(Pk3Reader reader)
        {
            var sources = new List<(string path, string content)>();

            foreach (var entry in reader.GetEntries())
            {
                var nameUpper = Path.GetFileNameWithoutExtension(entry.FullPath).ToUpperInvariant();

                if (nameUpper == "SNDINFO" && entry.Size > 0)
                {
                    var text = ReadEntryAsText(reader, entry);
                    if (text != null)
                        sources.Add((entry.FullPath, text));
                }
            }

            return sources;
        }

        #endregion

        #region MAPINFO — GZDoom PK3

        [SkippableFact]
        public void GzdoomPk3_ShouldContainMapInfo()
        {
            var path = RequireWad("GzdoomPk3", Paths.GzdoomPk3);

            using var reader = new Pk3Reader(path);
            var sources = ExtractMapInfoSources(reader);

            _output.WriteLine($"Found {sources.Count} MAPINFO/ZMAPINFO entries in gzdoom.pk3");
            foreach (var (filePath, content) in sources)
                _output.WriteLine($"  {filePath} ({content.Length} chars)");

            Assert.NotEmpty(sources);
        }

        [SkippableFact]
        public void GzdoomPk3_ShouldParseMapInfoWithoutCrash()
        {
            var path = RequireWad("GzdoomPk3", Paths.GzdoomPk3);

            using var reader = new Pk3Reader(path);
            var sources = ExtractMapInfoSources(reader);
            Skip.If(sources.Count == 0, "No MAPINFO found in gzdoom.pk3");

            var crashes = new List<string>();
            int totalMaps = 0;

            foreach (var (filePath, content) in sources)
            {
                try
                {
                    var parser = new MapInfoParser(content);
                    var mapInfo = parser.Parse();

                    totalMaps += mapInfo.Maps.Count;

                    _output.WriteLine($"  {filePath}: {mapInfo.Maps.Count} maps, " +
                                      $"{mapInfo.Episodes.Count} episodes, " +
                                      $"{mapInfo.Clusters.Count} clusters, " +
                                      $"{mapInfo.Skills.Count} skills");
                }
                catch (Exception ex)
                {
                    crashes.Add($"{filePath}: {ex.GetType().Name}: {ex.Message}");
                }
            }

            _output.WriteLine($"Total maps defined: {totalMaps}");

            foreach (var c in crashes)
                _output.WriteLine($"  CRASH: {c}");

            Assert.Empty(crashes);
        }

        [SkippableFact]
        public void GzdoomPk3_MapInfoShouldHavePopulatedMaps()
        {
            var path = RequireWad("GzdoomPk3", Paths.GzdoomPk3);

            using var reader = new Pk3Reader(path);
            var sources = ExtractMapInfoSources(reader);
            Skip.If(sources.Count == 0, "No MAPINFO found in gzdoom.pk3");

            // Parse all MAPINFO sources, aggregate maps
            int totalMaps = 0;
            foreach (var (_, content) in sources)
            {
                try
                {
                    var parser = new MapInfoParser(content);
                    var mapInfo = parser.Parse();
                    totalMaps += mapInfo.Maps.Count;
                }
                catch
                {
                    // Crashes tracked by separate test
                }
            }

            _output.WriteLine($"Total map definitions across all MAPINFO: {totalMaps}");

            // GZDoom's built-in MAPINFO defines maps for all standard Doom games
            Assert.True(totalMaps > 0, "MAPINFO should define at least some maps");
        }

        #endregion

        #region MAPINFO — Zandronum PK3

        [SkippableFact]
        public void ZandronumPk3_ShouldParseMapInfoWithoutCrash()
        {
            var path = RequireWad("ZandronumPk3", Paths.ZandronumPk3);

            using var reader = new Pk3Reader(path);
            var sources = ExtractMapInfoSources(reader);
            Skip.If(sources.Count == 0, "No MAPINFO found in zandronum.pk3");

            var crashes = new List<string>();
            int totalMaps = 0;

            foreach (var (filePath, content) in sources)
            {
                try
                {
                    var parser = new MapInfoParser(content);
                    var mapInfo = parser.Parse();
                    totalMaps += mapInfo.Maps.Count;

                    _output.WriteLine($"  {filePath}: {mapInfo.Maps.Count} maps, " +
                                      $"{mapInfo.Episodes.Count} episodes");
                }
                catch (Exception ex)
                {
                    crashes.Add($"{filePath}: {ex.GetType().Name}: {ex.Message}");
                }
            }

            _output.WriteLine($"Total maps defined: {totalMaps}");

            foreach (var c in crashes)
                _output.WriteLine($"  CRASH: {c}");

            Assert.Empty(crashes);
        }

        #endregion

        #region MAPINFO — All PK3s

        [SkippableFact]
        public void AllPk3s_ShouldParseMapInfoWithoutCrash()
        {
            var pk3s = Paths.GetPk3s();
            int testedCount = 0;
            int totalMaps = 0;
            var allCrashes = new List<string>();

            foreach (var (name, pk3Path) in pk3s)
            {
                if (!WadTestConfiguration.IsAvailable(pk3Path))
                    continue;

                testedCount++;

                using var reader = new Pk3Reader(pk3Path);
                var sources = ExtractMapInfoSources(reader);

                foreach (var (filePath, content) in sources)
                {
                    try
                    {
                        var parser = new MapInfoParser(content);
                        var mapInfo = parser.Parse();
                        totalMaps += mapInfo.Maps.Count;
                    }
                    catch (Exception ex)
                    {
                        allCrashes.Add($"{name}/{filePath}: {ex.GetType().Name}: {ex.Message}");
                    }
                }
            }

            Skip.If(testedCount == 0, "No PK3s configured for testing");

            _output.WriteLine($"Parsed MAPINFO from {testedCount} PK3s, {totalMaps} map definitions");

            foreach (var c in allCrashes)
                _output.WriteLine($"  CRASH: {c}");

            Assert.Empty(allCrashes);
        }

        #endregion

        #region SNDINFO — GZDoom PK3

        [SkippableFact]
        public void GzdoomPk3_ShouldContainSndInfo()
        {
            var path = RequireWad("GzdoomPk3", Paths.GzdoomPk3);

            using var reader = new Pk3Reader(path);
            var sources = ExtractSndInfoSources(reader);

            _output.WriteLine($"Found {sources.Count} SNDINFO entries in gzdoom.pk3");
            foreach (var (filePath, content) in sources)
                _output.WriteLine($"  {filePath} ({content.Length} chars)");

            Assert.NotEmpty(sources);
        }

        [SkippableFact]
        public void GzdoomPk3_ShouldParseSndInfoWithoutCrash()
        {
            var path = RequireWad("GzdoomPk3", Paths.GzdoomPk3);

            using var reader = new Pk3Reader(path);
            var sources = ExtractSndInfoSources(reader);
            Skip.If(sources.Count == 0, "No SNDINFO found in gzdoom.pk3");

            var crashes = new List<string>();
            int totalSounds = 0;
            int totalAliases = 0;
            int totalRandom = 0;

            foreach (var (filePath, content) in sources)
            {
                try
                {
                    var parser = new SndInfoParser();
                    var sndInfo = parser.Parse(content);

                    totalSounds += sndInfo.Sounds.Count;
                    totalAliases += sndInfo.Aliases.Count;
                    totalRandom += sndInfo.RandomSounds.Count;

                    _output.WriteLine($"  {filePath}: {sndInfo.Sounds.Count} sounds, " +
                                      $"{sndInfo.Aliases.Count} aliases, " +
                                      $"{sndInfo.RandomSounds.Count} random groups");
                }
                catch (Exception ex)
                {
                    crashes.Add($"{filePath}: {ex.GetType().Name}: {ex.Message}");
                }
            }

            _output.WriteLine($"Total: {totalSounds} sounds, {totalAliases} aliases, {totalRandom} random groups");

            foreach (var c in crashes)
                _output.WriteLine($"  CRASH: {c}");

            Assert.Empty(crashes);
        }

        [SkippableFact]
        public void GzdoomPk3_SndInfoShouldHavePopulatedSounds()
        {
            var path = RequireWad("GzdoomPk3", Paths.GzdoomPk3);

            using var reader = new Pk3Reader(path);
            var sources = ExtractSndInfoSources(reader);
            Skip.If(sources.Count == 0, "No SNDINFO found in gzdoom.pk3");

            int totalSounds = 0;
            foreach (var (_, content) in sources)
            {
                try
                {
                    var parser = new SndInfoParser();
                    var sndInfo = parser.Parse(content);
                    totalSounds += sndInfo.Sounds.Count;
                }
                catch
                {
                    // Crashes tracked by separate test
                }
            }

            _output.WriteLine($"Total sound definitions: {totalSounds}");

            // GZDoom's SNDINFO defines hundreds of sound mappings
            Assert.True(totalSounds > 0, "SNDINFO should define at least some sounds");
        }

        #endregion

        #region SNDINFO — All PK3s

        [SkippableFact]
        public void AllPk3s_ShouldParseSndInfoWithoutCrash()
        {
            var pk3s = Paths.GetPk3s();
            int testedCount = 0;
            int totalSounds = 0;
            var allCrashes = new List<string>();

            foreach (var (name, pk3Path) in pk3s)
            {
                if (!WadTestConfiguration.IsAvailable(pk3Path))
                    continue;

                testedCount++;

                using var reader = new Pk3Reader(pk3Path);
                var sources = ExtractSndInfoSources(reader);

                foreach (var (filePath, content) in sources)
                {
                    try
                    {
                        var parser = new SndInfoParser();
                        var sndInfo = parser.Parse(content);
                        totalSounds += sndInfo.Sounds.Count;
                    }
                    catch (Exception ex)
                    {
                        allCrashes.Add($"{name}/{filePath}: {ex.GetType().Name}: {ex.Message}");
                    }
                }
            }

            Skip.If(testedCount == 0, "No PK3s configured for testing");

            _output.WriteLine($"Parsed SNDINFO from {testedCount} PK3s, {totalSounds} sound definitions");

            foreach (var c in allCrashes)
                _output.WriteLine($"  CRASH: {c}");

            Assert.Empty(allCrashes);
        }

        #endregion

        #region DEHACKED — PWADs

        [SkippableFact]
        public void AllPwads_ShouldParseDehackedWithoutCrash()
        {
            var pwads = Paths.GetPwads();
            int testedCount = 0;
            int totalPatches = 0;
            var allCrashes = new List<string>();

            foreach (var (name, wadPath) in pwads)
            {
                if (!WadTestConfiguration.IsAvailable(wadPath))
                    continue;

                testedCount++;

                using var reader = new WadArchiveReader(wadPath);
                var dehEntry = reader.GetEntry("DEHACKED");

                if (dehEntry == null)
                    continue;

                var text = ReadEntryAsText(reader, dehEntry);
                if (text == null)
                    continue;

                try
                {
                    var parser = new DehackedParser(text);
                    var patch = parser.Parse();
                    totalPatches++;

                    _output.WriteLine($"  {name}/DEHACKED: {patch.Things.Count} things, " +
                                      $"{patch.Frames.Count} frames, " +
                                      $"{patch.Weapons.Count} weapons, " +
                                      $"{patch.Strings.Count} strings, " +
                                      $"{patch.Texts.Count} texts");
                }
                catch (Exception ex)
                {
                    allCrashes.Add($"{name}/DEHACKED: {ex.GetType().Name}: {ex.Message}");
                }
            }

            Skip.If(testedCount == 0, "No PWADs configured for testing");

            _output.WriteLine($"Parsed DEHACKED from {totalPatches} PWADs (out of {testedCount} tested)");

            foreach (var c in allCrashes)
                _output.WriteLine($"  CRASH: {c}");

            Assert.Empty(allCrashes);
        }

        [SkippableFact]
        public void Eviternity_ShouldParseDehacked()
        {
            var path = RequireWad("Eviternity", Paths.Eviternity);

            using var reader = new WadArchiveReader(path);
            var dehEntry = reader.GetEntry("DEHACKED");
            Skip.If(dehEntry == null, "Eviternity does not contain a DEHACKED lump");

            var text = ReadEntryAsText(reader, dehEntry!);
            Assert.NotNull(text);

            var parser = new DehackedParser(text!);
            var patch = parser.Parse();

            _output.WriteLine($"Eviternity DEHACKED: {patch.Things.Count} things, " +
                              $"{patch.Frames.Count} frames, " +
                              $"{patch.Weapons.Count} weapons, " +
                              $"{patch.Strings.Count} strings, " +
                              $"{patch.Texts.Count} texts, " +
                              $"{patch.CodePointers.Count} code pointers");

            // Eviternity is a well-known megawad that uses DEHACKED extensively
            // for custom monsters and text changes
            Assert.True(patch.Things.Count > 0 || patch.Frames.Count > 0 || patch.Texts.Count > 0,
                "Eviternity's DEHACKED should define things, frames, or text replacements");
        }

        #endregion

        #region UDMF — Real Maps

        [SkippableFact]
        public void AllPk3s_ShouldParseUdmfMapsWithoutCrash()
        {
            var pk3s = Paths.GetPk3s();
            int testedCount = 0;
            int totalUdmfMaps = 0;
            var allCrashes = new List<string>();

            foreach (var (name, pk3Path) in pk3s)
            {
                if (!WadTestConfiguration.IsAvailable(pk3Path))
                    continue;

                testedCount++;

                using var reader = new Pk3Reader(pk3Path);

                // Look for UDMF maps: they have TEXTMAP lumps inside embedded WADs
                var embeddedWads = reader.GetEmbeddedWads().ToList();

                foreach (var wadPath in embeddedWads)
                {
                    try
                    {
                        using var wadReader = reader.OpenEmbeddedWad(wadPath);
                        var textmapEntry = wadReader.GetEntry("TEXTMAP");

                        if (textmapEntry == null || textmapEntry.Size == 0)
                            continue;

                        var text = ReadEntryAsText(wadReader, textmapEntry);
                        if (text == null)
                            continue;

                        var parser = new UdmfParser();
                        var map = parser.Parse(text);
                        totalUdmfMaps++;

                        _output.WriteLine($"  {name}/{wadPath}: " +
                                          $"{map.Things.Count} things, " +
                                          $"{map.Linedefs.Count} linedefs, " +
                                          $"{map.Sectors.Count} sectors, " +
                                          $"ns={map.Namespace}");
                    }
                    catch (Exception ex)
                    {
                        allCrashes.Add($"{name}/{wadPath}: {ex.GetType().Name}: {ex.Message}");
                    }
                }
            }

            Skip.If(testedCount == 0, "No PK3s configured for testing");

            _output.WriteLine($"Parsed {totalUdmfMaps} UDMF maps from {testedCount} PK3s");

            if (totalUdmfMaps == 0)
                _output.WriteLine("  (No UDMF maps found in configured PK3s — this is OK for engine PK3s)");

            foreach (var c in allCrashes)
                _output.WriteLine($"  CRASH: {c}");

            Assert.Empty(allCrashes);
        }

        [SkippableFact]
        public void AllPwads_ShouldParseUdmfMapsWithoutCrash()
        {
            var pwads = Paths.GetPwads();
            int testedCount = 0;
            int totalUdmfMaps = 0;
            var allCrashes = new List<string>();

            foreach (var (name, wadPath) in pwads)
            {
                if (!WadTestConfiguration.IsAvailable(wadPath))
                    continue;

                testedCount++;

                using var reader = new WadReader(wadPath);
                var wad = reader.ReadWad();
                var mapReader = new MapReader();

                // Check if any maps are UDMF format
                var maps = mapReader.ReadMaps(wad);

                foreach (var map in maps.Where(m => m.Format == MapFormat.UDMF))
                {
                    // Find the TEXTMAP lump for this map
                    using var archiveReader = new WadArchiveReader(wadPath);
                    var textmapEntry = archiveReader.GetEntry("TEXTMAP");

                    if (textmapEntry == null || textmapEntry.Size == 0)
                        continue;

                    var text = ReadEntryAsText(archiveReader, textmapEntry);
                    if (text == null)
                        continue;

                    try
                    {
                        var parser = new UdmfParser();
                        var udmfMap = parser.Parse(text);
                        totalUdmfMaps++;

                        _output.WriteLine($"  {name}/{map.Name}: " +
                                          $"{udmfMap.Things.Count} things, " +
                                          $"{udmfMap.Linedefs.Count} linedefs, " +
                                          $"ns={udmfMap.Namespace}");
                    }
                    catch (Exception ex)
                    {
                        allCrashes.Add($"{name}/{map.Name}: {ex.GetType().Name}: {ex.Message}");
                    }

                    break; // Only test the first UDMF map per WAD
                }
            }

            Skip.If(testedCount == 0, "No PWADs configured for testing");

            _output.WriteLine($"Parsed {totalUdmfMaps} UDMF maps from {testedCount} PWADs");

            if (totalUdmfMaps == 0)
                _output.WriteLine("  (No UDMF maps found in configured PWADs — this is OK for classic-format WADs)");

            foreach (var c in allCrashes)
                _output.WriteLine($"  CRASH: {c}");

            Assert.Empty(allCrashes);
        }

        #endregion
    }
}
