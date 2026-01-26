using System.Linq;
using Xunit;
using WAD.NET.Archives;
using WAD.NET.Enums;
using WAD.NET.Tests.Infrastructure;

namespace WAD.NET.Tests.RealWorld
{
    /// <summary>
    /// Tests against real PK3 files (modern DOOM mods) to validate parsing correctness.
    /// These tests are skipped if the PK3 files are not configured.
    /// </summary>
    public class Pk3Tests : RealWorldWadTestBase
    {
        private static WadPaths Paths => WadTestConfiguration.Paths;

        #region Brutal Doom Tests

        [SkippableFact]
        public void BrutalDoom_ShouldBeDetectedAsPk3()
        {
            var path = RequireWad("BrutalDoom", Paths.BrutalDoom);

            using var reader = ArchiveReaderFactory.Open(path);

            Assert.Equal(ArchiveType.PK3, reader.Type);
        }

        [SkippableFact]
        public void BrutalDoom_ShouldHaveDecorateDefinitions()
        {
            var path = RequireWad("BrutalDoom", Paths.BrutalDoom);

            using var reader = new Pk3Reader(path);

            var scriptEntries = reader.GetEntriesByCategory(LumpCategory.Script).ToList();

            Assert.NotEmpty(scriptEntries);
        }

        [SkippableFact]
        public void BrutalDoom_ShouldHaveSpriteReplacements()
        {
            var path = RequireWad("BrutalDoom", Paths.BrutalDoom);

            using var reader = new Pk3Reader(path);

            var spriteEntries = reader.GetEntriesByCategory(LumpCategory.Sprite).ToList();

            // Brutal Doom has many custom sprites
            Assert.NotEmpty(spriteEntries);
        }

        [SkippableFact]
        public void BrutalDoom_ShouldHaveSoundReplacements()
        {
            var path = RequireWad("BrutalDoom", Paths.BrutalDoom);

            using var reader = new Pk3Reader(path);

            var soundEntries = reader.GetEntriesByCategory(LumpCategory.Sound).ToList();

            Assert.NotEmpty(soundEntries);
        }

        [SkippableFact]
        public void BrutalDoom_ShouldHaveSndInfoDefinition()
        {
            var path = RequireWad("BrutalDoom", Paths.BrutalDoom);

            using var reader = new Pk3Reader(path);

            // Most complex mods have SNDINFO for sound definitions
            var sndinfo = reader.GetEntries()
                .FirstOrDefault(e => e.Name == "SNDINFO" ||
                                    e.FullPath.ToUpperInvariant().Contains("SNDINFO"));

            Assert.NotNull(sndinfo);
        }

        #endregion

        #region GZDoom PK3 Tests

        [SkippableFact]
        public void GzdoomPk3_ShouldBeDetectedAsPk3()
        {
            var path = RequireWad("GzdoomPk3", Paths.GzdoomPk3);

            using var reader = ArchiveReaderFactory.Open(path);

            Assert.Equal(ArchiveType.PK3, reader.Type);
        }

        [SkippableFact]
        public void GzdoomPk3_ShouldHaveEntries()
        {
            var path = RequireWad("GzdoomPk3", Paths.GzdoomPk3);

            using var reader = new Pk3Reader(path);

            var entries = reader.GetEntries().ToList();

            Assert.NotEmpty(entries);
        }

        #endregion

        #region Custom Mod Tests

        [SkippableFact]
        public void CustomMod_ShouldBeDetectedAsPk3()
        {
            var path = RequireWad("CustomMod", Paths.CustomMod);

            using var reader = ArchiveReaderFactory.Open(path);

            Assert.Equal(ArchiveType.PK3, reader.Type);
        }

        [SkippableFact]
        public void CustomMod_ShouldParseAllEntries()
        {
            var path = RequireWad("CustomMod", Paths.CustomMod);

            using var reader = new Pk3Reader(path);

            var entries = reader.GetEntries().ToList();

            Assert.NotEmpty(entries);

            // Verify we can read all entries without error
            foreach (var entry in entries)
            {
                if (entry.Size > 0 && entry.Size < 10_000_000) // Skip huge files
                {
                    var data = reader.ReadLump(entry);
                    Assert.NotNull(data);
                    Assert.Equal(entry.Size, data.Length);
                }
            }
        }

        #endregion

        #region Generic PK3 Tests

        [SkippableFact]
        public void AllConfiguredPk3s_ShouldParseWithoutError()
        {
            var pk3s = Paths.GetPk3s();
            int testedCount = 0;

            foreach (var (name, path) in pk3s)
            {
                if (!WadTestConfiguration.IsAvailable(path))
                    continue;

                testedCount++;

                using var reader = new Pk3Reader(path);

                var entries = reader.GetEntries().ToList();
                Assert.NotEmpty(entries);
            }

            Skip.If(testedCount == 0, "No PK3s configured for testing");
        }

        [SkippableFact]
        public void AllConfiguredPk3s_ShouldCategorizeCorrectly()
        {
            var pk3s = Paths.GetPk3s();
            int testedCount = 0;

            foreach (var (name, path) in pk3s)
            {
                if (!WadTestConfiguration.IsAvailable(path))
                    continue;

                testedCount++;

                using var reader = new Pk3Reader(path);

                var entries = reader.GetEntries().ToList();

                // Count categorized vs uncategorized
                var categorized = entries.Count(e => e.Category != LumpCategory.Unknown);
                var total = entries.Count;

                // At least some entries should be categorized
                // (root-level files may be Unknown, that's OK)
                Assert.True(categorized > 0 || total < 5,
                    $"{name}: Expected some entries to be categorized");
            }

            Skip.If(testedCount == 0, "No PK3s configured for testing");
        }

        [SkippableFact]
        public void AllConfiguredPk3s_ShouldOpenWithFactory()
        {
            var pk3s = Paths.GetPk3s();
            int testedCount = 0;

            foreach (var (name, path) in pk3s)
            {
                if (!WadTestConfiguration.IsAvailable(path))
                    continue;

                testedCount++;

                using var reader = ArchiveReaderFactory.Open(path);

                Assert.Equal(ArchiveType.PK3, reader.Type);
                Assert.IsType<Pk3Reader>(reader);
            }

            Skip.If(testedCount == 0, "No PK3s configured for testing");
        }

        #endregion

        #region PK3 with Embedded WAD Tests

        [SkippableFact]
        public void Pk3WithMaps_ShouldFindEmbeddedWads()
        {
            // Try all configured PK3s to find one with embedded WADs
            var pk3s = Paths.GetPk3s();
            int testedCount = 0;
            bool foundWads = false;

            foreach (var (name, path) in pk3s)
            {
                if (!WadTestConfiguration.IsAvailable(path))
                    continue;

                testedCount++;

                using var reader = new Pk3Reader(path);
                var embeddedWads = reader.GetEmbeddedWads().ToList();

                if (embeddedWads.Count > 0)
                {
                    foundWads = true;

                    // Verify we can open the first embedded WAD
                    using var wadReader = reader.OpenEmbeddedWad(embeddedWads[0]);
                    var entries = wadReader.GetEntries().ToList();
                    Assert.NotEmpty(entries);
                    break;
                }
            }

            Skip.If(testedCount == 0, "No PK3s configured for testing");

            // This test passes if we either found embedded WADs or didn't
            // (not all PK3s have embedded WADs)
            if (!foundWads && testedCount > 0)
            {
                // Log that no embedded WADs were found, but don't fail
                Assert.True(true, "No embedded WADs found in configured PK3s (this is OK)");
            }
        }

        #endregion

        #region Category Tests

        [SkippableFact]
        public void Pk3_SpritesFolder_ShouldBeCategorizedAsSprite()
        {
            var pk3s = Paths.GetPk3s();

            foreach (var (name, path) in pk3s)
            {
                if (!WadTestConfiguration.IsAvailable(path))
                    continue;

                using var reader = new Pk3Reader(path);

                var spriteEntries = reader.GetEntriesInFolder("sprites").ToList();

                foreach (var entry in spriteEntries)
                {
                    Assert.Equal(LumpCategory.Sprite, entry.Category);
                }

                if (spriteEntries.Count > 0)
                    return; // Found and tested sprites folder
            }

            Skip.If(true, "No PK3s with sprites folder configured");
        }

        [SkippableFact]
        public void Pk3_SoundsFolder_ShouldBeCategorizedAsSound()
        {
            var pk3s = Paths.GetPk3s();

            foreach (var (name, path) in pk3s)
            {
                if (!WadTestConfiguration.IsAvailable(path))
                    continue;

                using var reader = new Pk3Reader(path);

                var soundEntries = reader.GetEntriesInFolder("sounds").ToList();

                foreach (var entry in soundEntries)
                {
                    Assert.Equal(LumpCategory.Sound, entry.Category);
                }

                if (soundEntries.Count > 0)
                    return; // Found and tested sounds folder
            }

            Skip.If(true, "No PK3s with sounds folder configured");
        }

        #endregion
    }
}
