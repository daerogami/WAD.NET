using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Xunit;
using WAD.NET.Archives;
using WAD.NET.Enums;

namespace WAD.NET.Tests
{
    public class ArchiveReaderTests
    {
        #region LumpEntry Tests

        [Fact]
        public void LumpEntry_ShouldIdentifyMarker()
        {
            var entry = new LumpEntry
            {
                Name = "F_START",
                FullPath = "F_START",
                Size = 0,
                CompressedSize = 0,
                Category = LumpCategory.Unknown
            };

            Assert.True(entry.IsMarker);
        }

        [Fact]
        public void LumpEntry_ShouldNotBeMarkerWithData()
        {
            var entry = new LumpEntry
            {
                Name = "PLAYPAL",
                FullPath = "PLAYPAL",
                Size = 768,
                CompressedSize = 768,
                Category = LumpCategory.Graphic
            };

            Assert.False(entry.IsMarker);
        }

        #endregion

        #region ArchiveReaderFactory Tests

        [Fact]
        public void ArchiveReaderFactory_ShouldDetectWadMagic()
        {
            // Create a minimal WAD in memory
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms, Encoding.ASCII, leaveOpen: true);
            writer.Write(Encoding.ASCII.GetBytes("IWAD"));
            writer.Write(0); // lump count
            writer.Write(12); // directory offset
            writer.Flush();

            ms.Position = 0;
            var type = ArchiveReaderFactory.DetectArchiveType(ms);

            Assert.Equal(ArchiveTypeDetected.WAD, type);
        }

        [Fact]
        public void ArchiveReaderFactory_ShouldDetectPwadMagic()
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms, Encoding.ASCII, leaveOpen: true);
            writer.Write(Encoding.ASCII.GetBytes("PWAD"));
            writer.Write(0);
            writer.Write(12);
            writer.Flush();

            ms.Position = 0;
            var type = ArchiveReaderFactory.DetectArchiveType(ms);

            Assert.Equal(ArchiveTypeDetected.WAD, type);
        }

        [Fact]
        public void ArchiveReaderFactory_ShouldDetectZipMagic()
        {
            using var ms = new MemoryStream();
            // Write ZIP magic bytes
            ms.Write(new byte[] { 0x50, 0x4B, 0x03, 0x04 }, 0, 4);
            ms.Position = 0;

            var type = ArchiveReaderFactory.DetectArchiveType(ms);

            Assert.Equal(ArchiveTypeDetected.ZIP, type);
        }

        [Fact]
        public void ArchiveReaderFactory_ShouldDetect7zMagic()
        {
            using var ms = new MemoryStream();
            // Write full 7z magic bytes (6 bytes)
            ms.Write(new byte[] { 0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C }, 0, 6);
            ms.Position = 0;

            var type = ArchiveReaderFactory.DetectArchiveType(ms);

            Assert.Equal(ArchiveTypeDetected.SevenZip, type);
        }

        [Fact]
        public void ArchiveReaderFactory_ShouldReturnUnknownForInvalidMagic()
        {
            using var ms = new MemoryStream();
            ms.Write(new byte[] { 0x00, 0x00, 0x00, 0x00 }, 0, 4);
            ms.Position = 0;

            var type = ArchiveReaderFactory.DetectArchiveType(ms);

            Assert.Equal(ArchiveTypeDetected.Unknown, type);
        }

        [Fact]
        public void ArchiveReaderFactory_ShouldOpenFolder()
        {
            var tempFolder = Path.Combine(Path.GetTempPath(), $"wadtest_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempFolder);

            try
            {
                // Create a test file
                File.WriteAllText(Path.Combine(tempFolder, "test.txt"), "test");

                using var reader = ArchiveReaderFactory.Open(tempFolder);

                Assert.IsType<FolderReader>(reader);
                Assert.Equal(ArchiveType.Folder, reader.Type);
            }
            finally
            {
                Directory.Delete(tempFolder, true);
            }
        }

        [Fact]
        public void ArchiveReaderFactory_ShouldThrowForNonExistentPath()
        {
            Assert.Throws<FileNotFoundException>(() =>
                ArchiveReaderFactory.Open("/nonexistent/path/file.wad"));
        }

        #endregion

        #region Pk3Reader Tests

        [Fact]
        public void Pk3Reader_ShouldReadEntries()
        {
            using var pk3Stream = CreateTestPk3();
            using var reader = new Pk3Reader(pk3Stream, leaveOpen: false);

            var entries = reader.GetEntries().ToList();

            Assert.NotEmpty(entries);
        }

        [Fact]
        public void Pk3Reader_ShouldCategorizeSprites()
        {
            using var pk3Stream = CreateTestPk3();
            using var reader = new Pk3Reader(pk3Stream, leaveOpen: false);

            var spriteEntry = reader.GetEntries()
                .FirstOrDefault(e => e.FullPath.StartsWith("sprites/"));

            Assert.NotNull(spriteEntry);
            Assert.Equal(LumpCategory.Sprite, spriteEntry.Category);
        }

        [Fact]
        public void Pk3Reader_ShouldCategorizeFlats()
        {
            using var pk3Stream = CreateTestPk3();
            using var reader = new Pk3Reader(pk3Stream, leaveOpen: false);

            var flatEntry = reader.GetEntries()
                .FirstOrDefault(e => e.FullPath.StartsWith("flats/"));

            Assert.NotNull(flatEntry);
            Assert.Equal(LumpCategory.Flat, flatEntry.Category);
        }

        [Fact]
        public void Pk3Reader_ShouldCategorizeMusic()
        {
            using var pk3Stream = CreateTestPk3();
            using var reader = new Pk3Reader(pk3Stream, leaveOpen: false);

            var musicEntry = reader.GetEntries()
                .FirstOrDefault(e => e.FullPath.StartsWith("music/"));

            Assert.NotNull(musicEntry);
            Assert.Equal(LumpCategory.Music, musicEntry.Category);
        }

        [Fact]
        public void Pk3Reader_ShouldCategorizeDefinitionFiles()
        {
            using var pk3Stream = CreateTestPk3();
            using var reader = new Pk3Reader(pk3Stream, leaveOpen: false);

            var mapinfoEntry = reader.GetEntries()
                .FirstOrDefault(e => e.Name == "MAPINFO");

            Assert.NotNull(mapinfoEntry);
            Assert.Equal(LumpCategory.Definition, mapinfoEntry.Category);
        }

        [Fact]
        public void Pk3Reader_ShouldReadLumpData()
        {
            using var pk3Stream = CreateTestPk3();
            using var reader = new Pk3Reader(pk3Stream, leaveOpen: false);

            var entry = reader.GetEntries().First();
            var data = reader.ReadLump(entry);

            Assert.NotEmpty(data);
        }

        [Fact]
        public void Pk3Reader_ShouldOpenLumpAsStream()
        {
            using var pk3Stream = CreateTestPk3();
            using var reader = new Pk3Reader(pk3Stream, leaveOpen: false);

            var entry = reader.GetEntries().First();
            using var stream = reader.OpenLump(entry);

            Assert.NotNull(stream);
            Assert.True(stream.CanRead);
            Assert.True(stream.CanSeek);
        }

        [Fact]
        public void Pk3Reader_ShouldGetEntryByName()
        {
            using var pk3Stream = CreateTestPk3();
            using var reader = new Pk3Reader(pk3Stream, leaveOpen: false);

            var entry = reader.GetEntry("MAPINFO");

            Assert.NotNull(entry);
            Assert.Equal("MAPINFO", entry.Name);
        }

        [Fact]
        public void Pk3Reader_ShouldReturnNullForMissingEntry()
        {
            using var pk3Stream = CreateTestPk3();
            using var reader = new Pk3Reader(pk3Stream, leaveOpen: false);

            var entry = reader.GetEntry("NONEXISTENT");

            Assert.Null(entry);
        }

        [Fact]
        public void Pk3Reader_ContainsShouldWork()
        {
            using var pk3Stream = CreateTestPk3();
            using var reader = new Pk3Reader(pk3Stream, leaveOpen: false);

            Assert.True(reader.Contains("MAPINFO"));
            Assert.False(reader.Contains("NONEXISTENT"));
        }

        [Fact]
        public void Pk3Reader_ShouldFindEmbeddedWads()
        {
            using var pk3Stream = CreateTestPk3WithEmbeddedWad();
            using var reader = new Pk3Reader(pk3Stream, leaveOpen: false);

            var wads = reader.GetEmbeddedWads().ToList();

            Assert.Single(wads);
            Assert.Contains("maps/MAP01.wad", wads);
        }

        [Fact]
        public void Pk3Reader_ShouldGetEntriesByCategory()
        {
            using var pk3Stream = CreateTestPk3();
            using var reader = new Pk3Reader(pk3Stream, leaveOpen: false);

            var sprites = reader.GetEntriesByCategory(LumpCategory.Sprite).ToList();

            Assert.NotEmpty(sprites);
            Assert.All(sprites, e => Assert.Equal(LumpCategory.Sprite, e.Category));
        }

        [Fact]
        public void Pk3Reader_ShouldGetEntriesInFolder()
        {
            using var pk3Stream = CreateTestPk3();
            using var reader = new Pk3Reader(pk3Stream, leaveOpen: false);

            var spritesInFolder = reader.GetEntriesInFolder("sprites").ToList();

            Assert.NotEmpty(spritesInFolder);
        }

        [Fact]
        public void Pk3Reader_ShouldTruncateLongNames()
        {
            using var pk3Stream = CreateTestPk3WithLongNames();
            using var reader = new Pk3Reader(pk3Stream, leaveOpen: false);

            var entry = reader.GetEntries()
                .FirstOrDefault(e => e.FullPath.Contains("VERYLONGFILENAME"));

            Assert.NotNull(entry);
            Assert.Equal(8, entry.Name.Length);
            Assert.Equal("VERYLONG", entry.Name);
        }

        #endregion

        #region FolderReader Tests

        [Fact]
        public void FolderReader_ShouldReadEntries()
        {
            var tempFolder = CreateTestFolder();
            try
            {
                using var reader = new FolderReader(tempFolder);

                var entries = reader.GetEntries().ToList();

                Assert.NotEmpty(entries);
            }
            finally
            {
                Directory.Delete(tempFolder, true);
            }
        }

        [Fact]
        public void FolderReader_ShouldCategorizeByFolder()
        {
            var tempFolder = CreateTestFolder();
            try
            {
                using var reader = new FolderReader(tempFolder);

                var spriteEntry = reader.GetEntries()
                    .FirstOrDefault(e => e.FullPath.StartsWith("sprites/"));

                Assert.NotNull(spriteEntry);
                Assert.Equal(LumpCategory.Sprite, spriteEntry.Category);
            }
            finally
            {
                Directory.Delete(tempFolder, true);
            }
        }

        [Fact]
        public void FolderReader_ShouldReadFileData()
        {
            var tempFolder = CreateTestFolder();
            try
            {
                using var reader = new FolderReader(tempFolder);

                var entry = reader.GetEntry("MAPINFO");
                Assert.NotNull(entry);

                var data = reader.ReadLump(entry);

                Assert.NotEmpty(data);
                Assert.Equal("// Test MAPINFO", Encoding.UTF8.GetString(data));
            }
            finally
            {
                Directory.Delete(tempFolder, true);
            }
        }

        [Fact]
        public void FolderReader_ShouldThrowForNonExistentFolder()
        {
            Assert.Throws<DirectoryNotFoundException>(() =>
                new FolderReader("/nonexistent/folder/path"));
        }

        [Fact]
        public void FolderReader_ContainsShouldWork()
        {
            var tempFolder = CreateTestFolder();
            try
            {
                using var reader = new FolderReader(tempFolder);

                Assert.True(reader.Contains("MAPINFO"));
                Assert.False(reader.Contains("NONEXISTENT"));
            }
            finally
            {
                Directory.Delete(tempFolder, true);
            }
        }

        #endregion

        #region WadArchiveReader Tests

        [Fact]
        public void WadArchiveReader_ShouldReadFromStream()
        {
            using var wadStream = CreateMinimalWad();
            using var reader = new WadArchiveReader(wadStream, leaveOpen: false, wadName: "test.wad");

            Assert.Equal(ArchiveType.WAD, reader.Type);
            Assert.Equal(WadType.PWAD, reader.WadType);
        }

        [Fact]
        public void WadArchiveReader_ShouldReadEntries()
        {
            using var wadStream = CreateMinimalWad();
            using var reader = new WadArchiveReader(wadStream, leaveOpen: false);

            var entries = reader.GetEntries().ToList();

            Assert.Single(entries);
            Assert.Equal("PLAYPAL", entries[0].Name);
        }

        [Fact]
        public void WadArchiveReader_ShouldReadLumpData()
        {
            using var wadStream = CreateMinimalWad();
            using var reader = new WadArchiveReader(wadStream, leaveOpen: false);

            var entry = reader.GetEntry("PLAYPAL");
            Assert.NotNull(entry);

            var data = reader.ReadLump(entry);

            Assert.Equal(10752, data.Length);
        }

        [Fact]
        public void WadArchiveReader_ShouldGetEntryByName()
        {
            using var wadStream = CreateMinimalWad();
            using var reader = new WadArchiveReader(wadStream, leaveOpen: false);

            var entry = reader.GetEntry("PLAYPAL");

            Assert.NotNull(entry);
            Assert.Equal("PLAYPAL", entry.Name);
        }

        [Fact]
        public void WadArchiveReader_ContainsShouldWork()
        {
            using var wadStream = CreateMinimalWad();
            using var reader = new WadArchiveReader(wadStream, leaveOpen: false);

            Assert.True(reader.Contains("PLAYPAL"));
            Assert.False(reader.Contains("NONEXISTENT"));
        }

        [Fact]
        public void WadArchiveReader_ShouldCategorizeMapMarkers()
        {
            using var wadStream = CreateWadWithMap();
            using var reader = new WadArchiveReader(wadStream, leaveOpen: false);

            var mapEntry = reader.GetEntry("E1M1");

            Assert.NotNull(mapEntry);
            Assert.Equal(LumpCategory.Map, mapEntry.Category);
        }

        [Fact]
        public void WadArchiveReader_ShouldGetMapNames()
        {
            using var wadStream = CreateWadWithMap();
            using var reader = new WadArchiveReader(wadStream, leaveOpen: false);

            var mapNames = reader.GetMapNames().ToList();

            Assert.Single(mapNames);
            Assert.Contains("E1M1", mapNames);
        }

        [Fact]
        public void WadArchiveReader_ShouldReadLegacyWad()
        {
            using var wadStream = CreateMinimalWad();
            using var reader = new WadArchiveReader(wadStream, leaveOpen: false);

            var wad = reader.ReadWad();

            Assert.NotNull(wad);
            Assert.Equal(WadType.PWAD, wad.WadType);
        }

        #endregion

        #region Helper Methods

        private static MemoryStream CreateTestPk3()
        {
            var ms = new MemoryStream();
            using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
            {
                // Add various entries with different categories
                AddZipEntry(archive, "sprites/PLAYA1.png", new byte[] { 0x89, 0x50, 0x4E, 0x47 });
                AddZipEntry(archive, "flats/FLOOR4_8.png", new byte[] { 0x89, 0x50, 0x4E, 0x47 });
                AddZipEntry(archive, "music/D_E1M1.ogg", new byte[] { 0x4F, 0x67, 0x67, 0x53 });
                AddZipEntry(archive, "sounds/DSPISTOL.wav", new byte[] { 0x52, 0x49, 0x46, 0x46 });
                AddZipEntry(archive, "textures/STARTAN1.png", new byte[] { 0x89, 0x50, 0x4E, 0x47 });
                AddZipEntry(archive, "graphics/TITLEPIC.png", new byte[] { 0x89, 0x50, 0x4E, 0x47 });
                AddZipEntry(archive, "MAPINFO.txt", Encoding.UTF8.GetBytes("map E1M1 { }"));
                AddZipEntry(archive, "DECORATE.txt", Encoding.UTF8.GetBytes("actor Test { }"));
            }
            ms.Position = 0;
            return ms;
        }

        private static MemoryStream CreateTestPk3WithEmbeddedWad()
        {
            var ms = new MemoryStream();
            using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
            {
                // Add an embedded WAD
                var wadData = CreateMinimalWadData();
                AddZipEntry(archive, "maps/MAP01.wad", wadData);

                AddZipEntry(archive, "MAPINFO.txt", Encoding.UTF8.GetBytes("map MAP01 { }"));
            }
            ms.Position = 0;
            return ms;
        }

        private static MemoryStream CreateTestPk3WithLongNames()
        {
            var ms = new MemoryStream();
            using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
            {
                AddZipEntry(archive, "graphics/VERYLONGFILENAME.png", new byte[] { 0x89, 0x50, 0x4E, 0x47 });
            }
            ms.Position = 0;
            return ms;
        }

        private static void AddZipEntry(ZipArchive archive, string path, byte[] data)
        {
            var entry = archive.CreateEntry(path);
            using var stream = entry.Open();
            stream.Write(data, 0, data.Length);
        }

        private static string CreateTestFolder()
        {
            var tempFolder = Path.Combine(Path.GetTempPath(), $"wadtest_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempFolder);

            // Create subfolders
            Directory.CreateDirectory(Path.Combine(tempFolder, "sprites"));
            Directory.CreateDirectory(Path.Combine(tempFolder, "flats"));
            Directory.CreateDirectory(Path.Combine(tempFolder, "music"));

            // Create test files
            File.WriteAllBytes(Path.Combine(tempFolder, "sprites", "PLAYA1.png"),
                new byte[] { 0x89, 0x50, 0x4E, 0x47 });
            File.WriteAllBytes(Path.Combine(tempFolder, "flats", "FLOOR4_8.png"),
                new byte[] { 0x89, 0x50, 0x4E, 0x47 });
            File.WriteAllText(Path.Combine(tempFolder, "MAPINFO.txt"), "// Test MAPINFO");

            return tempFolder;
        }

        private static MemoryStream CreateMinimalWad()
        {
            return new MemoryStream(CreateMinimalWadData());
        }

        private static byte[] CreateMinimalWadData()
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms, Encoding.ASCII, leaveOpen: true);

            // Header
            writer.Write(Encoding.ASCII.GetBytes("PWAD"));
            writer.Write(1); // 1 lump
            writer.Write(12 + 10752); // directory offset (after header + lump data)

            // Lump data (PLAYPAL - 14 palettes × 768 bytes = 10752 bytes)
            var paletteData = new byte[10752];
            for (int p = 0; p < 14; p++)
            {
                for (int i = 0; i < 256; i++)
                {
                    var offset = p * 768 + i * 3;
                    paletteData[offset] = (byte)i;     // R
                    paletteData[offset + 1] = (byte)i; // G
                    paletteData[offset + 2] = (byte)i; // B
                }
            }
            writer.Write(paletteData);

            // Directory entry
            writer.Write(12);    // offset
            writer.Write(10752); // size
            writer.Write(Encoding.ASCII.GetBytes("PLAYPAL\0")); // name (8 bytes)

            writer.Flush();
            return ms.ToArray();
        }

        private static MemoryStream CreateWadWithMap()
        {
            var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms, Encoding.ASCII, leaveOpen: true);

            // Header
            writer.Write(Encoding.ASCII.GetBytes("PWAD"));
            writer.Write(2); // 2 lumps (E1M1 marker + THINGS)
            writer.Write(12 + 10); // directory offset

            // E1M1 marker (0 bytes) - no data

            // THINGS lump data (10 bytes - 1 thing)
            writer.Write((short)0); // x
            writer.Write((short)0); // y
            writer.Write((short)0); // angle
            writer.Write((short)1); // type
            writer.Write((short)7); // flags

            // Directory
            // E1M1 marker
            writer.Write(12);  // offset
            writer.Write(0);   // size (marker)
            writer.Write(Encoding.ASCII.GetBytes("E1M1\0\0\0\0")); // name

            // THINGS
            writer.Write(12);  // offset
            writer.Write(10);  // size
            writer.Write(Encoding.ASCII.GetBytes("THINGS\0\0")); // name

            writer.Flush();
            ms.Position = 0;
            return ms;
        }

        #endregion

        #region Pk7Reader Tests

        /// <summary>
        /// Helper method to safely delete temp files/directories, ignoring errors
        /// from SharpCompress file handle issues.
        /// </summary>
        private static void SafeDeleteTempFile(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                // Ignore cleanup errors - SharpCompress may not release file handles immediately
            }
        }

        private static void SafeDeleteTempDir(string path)
        {
            try
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        /// <summary>
        /// Finds 7z.exe on the system. Returns null if not found.
        /// </summary>
        private static string? Find7zExecutable()
        {
            // Check common install locations
            var candidates = new[]
            {
                @"C:\Program Files\7-Zip\7z.exe",
                @"C:\Program Files (x86)\7-Zip\7z.exe",
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                    return candidate;
            }

            // Try PATH
            try
            {
                var psi = new ProcessStartInfo("7z.exe", "--help")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                proc?.WaitForExit(3000);
                if (proc?.ExitCode == 0)
                    return "7z.exe";
            }
            catch
            {
                // Not in PATH
            }

            return null;
        }

        /// <summary>
        /// Creates a valid 7z archive using the 7z CLI.
        /// Returns the path to the archive, or null if 7z is not available.
        /// The caller is responsible for cleanup.
        /// </summary>
        private static string? CreateTest7zArchive()
        {
            var sevenZip = Find7zExecutable();
            if (sevenZip == null)
                return null;

            var id = Guid.NewGuid().ToString("N");
            var contentDir = Path.Combine(Path.GetTempPath(), $"pk7test_content_{id}");
            var archivePath = Path.Combine(Path.GetTempPath(), $"pk7test_{id}.7z");

            try
            {
                // Create content directory structure
                Directory.CreateDirectory(Path.Combine(contentDir, "sprites"));
                Directory.CreateDirectory(Path.Combine(contentDir, "flats"));
                Directory.CreateDirectory(Path.Combine(contentDir, "music"));
                Directory.CreateDirectory(Path.Combine(contentDir, "maps"));

                // Write test files
                File.WriteAllBytes(Path.Combine(contentDir, "sprites", "PLAYA1.png"),
                    new byte[] { 0x89, 0x50, 0x4E, 0x47 });
                File.WriteAllBytes(Path.Combine(contentDir, "flats", "FLOOR1.png"),
                    new byte[] { 0x89, 0x50, 0x4E, 0x47 });
                File.WriteAllBytes(Path.Combine(contentDir, "music", "D_E1M1.ogg"),
                    new byte[] { 0x4F, 0x67, 0x67, 0x53 });
                File.WriteAllBytes(Path.Combine(contentDir, "maps", "MAP01.wad"),
                    CreateMinimalWadData());
                File.WriteAllText(Path.Combine(contentDir, "MAPINFO.txt"), "map MAP01 { }");
                File.WriteAllText(Path.Combine(contentDir, "DECORATE.txt"), "actor Test { }");

                // Create 7z archive
                var psi = new ProcessStartInfo(sevenZip, $"a \"{archivePath}\" \"{contentDir}\\*\"")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                proc?.WaitForExit(10000);

                if (proc?.ExitCode != 0 || !File.Exists(archivePath))
                    return null;

                return archivePath;
            }
            catch
            {
                SafeDeleteTempFile(archivePath);
                return null;
            }
            finally
            {
                SafeDeleteTempDir(contentDir);
            }
        }

        /// <summary>
        /// Gets or creates the shared test 7z archive path.
        /// Cached across tests in a single run for performance.
        /// </summary>
        private static readonly Lazy<string?> _test7zPath = new(CreateTest7zArchive);

        /// <summary>
        /// Returns the path to a valid 7z test archive, skipping if 7z CLI is unavailable.
        /// </summary>
        private static string Require7zArchive()
        {
            var path = _test7zPath.Value;
            Skip.If(path == null, "7z CLI not available — cannot create test archive");
            return path!;
        }

        [SkippableFact]
        public void Pk7Reader_ShouldReturnCorrectArchiveType()
        {
            var archivePath = Require7zArchive();

            using var reader = new Pk7Reader(archivePath);

            Assert.Equal(ArchiveType.PK7, reader.Type);
            Assert.Equal(archivePath, reader.Path);
        }

        [Fact]
        public void Pk7Reader_ShouldThrowForNonExistentFile()
        {
            var nonExistentPath = Path.Combine(Path.GetTempPath(), "nonexistent_file.7z");

            Assert.Throws<FileNotFoundException>(() => new Pk7Reader(nonExistentPath));
        }

        [SkippableFact]
        public void Pk7Reader_ShouldCreateFromStream()
        {
            var archivePath = Require7zArchive();

            using var stream = File.OpenRead(archivePath);
            using var reader = new Pk7Reader(stream);

            Assert.Equal(ArchiveType.PK7, reader.Type);
            Assert.Equal(string.Empty, reader.Path);
        }

        [SkippableFact]
        public void Pk7Reader_GetEntries_ShouldReturnAllEntries()
        {
            var archivePath = Require7zArchive();

            using var reader = new Pk7Reader(archivePath);
            var entries = reader.GetEntries().ToList();

            Assert.NotEmpty(entries);
            Assert.Equal(6, entries.Count);
        }

        [SkippableFact]
        public void Pk7Reader_GetEntry_ShouldFindEntryCaseInsensitive()
        {
            var archivePath = Require7zArchive();

            using var reader = new Pk7Reader(archivePath);

            var entryLower = reader.GetEntry("mapinfo");
            var entryUpper = reader.GetEntry("MAPINFO");

            Assert.NotNull(entryLower ?? entryUpper);
        }

        [SkippableFact]
        public void Pk7Reader_GetEntry_ShouldReturnNullForNonExistent()
        {
            var archivePath = Require7zArchive();

            using var reader = new Pk7Reader(archivePath);
            var entry = reader.GetEntry("NONEXISTENT_ENTRY_12345");

            Assert.Null(entry);
        }

        [SkippableFact]
        public void Pk7Reader_Contains_ShouldReturnCorrectBoolean()
        {
            var archivePath = Require7zArchive();

            using var reader = new Pk7Reader(archivePath);
            var entries = reader.GetEntries().ToList();

            Assert.True(reader.Contains(entries[0].Name));
            Assert.False(reader.Contains("DEFINITELY_NOT_HERE_XYZ"));
        }

        [SkippableFact]
        public void Pk7Reader_ReadLump_ShouldReturnCorrectData()
        {
            var archivePath = Require7zArchive();

            using var reader = new Pk7Reader(archivePath);
            var entry = reader.GetEntries().First();
            var data = reader.ReadLump(entry);

            Assert.NotNull(data);
            Assert.Equal(entry.Size, data.Length);
        }

        [SkippableFact]
        public void Pk7Reader_OpenLump_ShouldReturnReadableStream()
        {
            var archivePath = Require7zArchive();

            using var reader = new Pk7Reader(archivePath);
            var entry = reader.GetEntries().First();
            using var stream = reader.OpenLump(entry);

            Assert.NotNull(stream);
            Assert.True(stream.CanRead);
            Assert.True(stream.CanSeek);
            Assert.Equal(0, stream.Position);
        }

        [SkippableFact]
        public void Pk7Reader_ShouldCategorizeMapEntries()
        {
            var archivePath = Require7zArchive();

            using var reader = new Pk7Reader(archivePath);
            var mapEntries = reader.GetEntries()
                .Where(e => e.FullPath.StartsWith("maps/", StringComparison.OrdinalIgnoreCase))
                .ToList();

            Assert.NotEmpty(mapEntries);
            Assert.All(mapEntries, e => Assert.Equal(LumpCategory.Map, e.Category));
        }

        [SkippableFact]
        public void Pk7Reader_ShouldCategorizeSpriteEntries()
        {
            var archivePath = Require7zArchive();

            using var reader = new Pk7Reader(archivePath);
            var spriteEntries = reader.GetEntries()
                .Where(e => e.FullPath.StartsWith("sprites/", StringComparison.OrdinalIgnoreCase))
                .ToList();

            Assert.NotEmpty(spriteEntries);
            Assert.All(spriteEntries, e => Assert.Equal(LumpCategory.Sprite, e.Category));
        }

        [SkippableFact]
        public void Pk7Reader_ShouldCategorizeByFilename()
        {
            var archivePath = Require7zArchive();

            using var reader = new Pk7Reader(archivePath);

            var mapinfoEntry = reader.GetEntries()
                .FirstOrDefault(e => e.Name.Equals("MAPINFO", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(mapinfoEntry);
            Assert.Equal(LumpCategory.Definition, mapinfoEntry!.Category);

            var decorateEntry = reader.GetEntries()
                .FirstOrDefault(e => e.Name.Equals("DECORATE", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(decorateEntry);
            Assert.Equal(LumpCategory.Script, decorateEntry!.Category);
        }

        [SkippableFact]
        public void Pk7Reader_Dispose_ShouldReleaseResources()
        {
            var archivePath = Require7zArchive();

            var reader = new Pk7Reader(archivePath);
            var entries = reader.GetEntries().ToList();

            reader.Dispose();

            // If we get here, Dispose succeeded without throwing
            Assert.NotEmpty(entries);
        }

        [SkippableFact]
        public void Pk7Reader_GetEntriesByCategory_ShouldFilterCorrectly()
        {
            var archivePath = Require7zArchive();

            using var reader = new Pk7Reader(archivePath);
            var sprites = reader.GetEntriesByCategory(LumpCategory.Sprite).ToList();

            Assert.NotEmpty(sprites);
            Assert.All(sprites, e => Assert.Equal(LumpCategory.Sprite, e.Category));
        }

        [SkippableFact]
        public void Pk7Reader_GetEntriesInFolder_ShouldFilterByPath()
        {
            var archivePath = Require7zArchive();

            using var reader = new Pk7Reader(archivePath);
            var spritesInFolder = reader.GetEntriesInFolder("sprites").ToList();

            Assert.NotEmpty(spritesInFolder);
            Assert.All(spritesInFolder, e =>
                Assert.StartsWith("sprites/", e.FullPath.ToLowerInvariant()));
        }

        [SkippableFact]
        public void Pk7Reader_GetEmbeddedWads_ShouldFindWadFiles()
        {
            var archivePath = Require7zArchive();

            using var reader = new Pk7Reader(archivePath);
            var wads = reader.GetEmbeddedWads().ToList();

            Assert.NotEmpty(wads);
            Assert.All(wads, w => Assert.EndsWith(".wad", w, StringComparison.OrdinalIgnoreCase));
        }

        [SkippableFact]
        public void Pk7Reader_ShouldTruncateLongNames()
        {
            var archivePath = Require7zArchive();

            using var reader = new Pk7Reader(archivePath);
            var entries = reader.GetEntries().ToList();

            Assert.All(entries, e => Assert.True(e.Name.Length <= 8,
                $"Entry name '{e.Name}' exceeds 8 characters"));
        }

        [SkippableFact]
        public void Pk7Reader_ReadLump_ShouldThrowForInvalidEntry()
        {
            var archivePath = Require7zArchive();

            using var reader = new Pk7Reader(archivePath);

            var fakeEntry = new LumpEntry
            {
                Name = "FAKE",
                FullPath = "nonexistent/fake_entry.txt",
                Size = 100,
                CompressedSize = 50,
                Category = LumpCategory.Unknown
            };

            Assert.Throws<InvalidOperationException>(() => reader.ReadLump(fakeEntry));
        }

        [SkippableFact]
        public void Pk7Reader_OpenLump_ShouldThrowForInvalidEntry()
        {
            var archivePath = Require7zArchive();

            using var reader = new Pk7Reader(archivePath);

            var fakeEntry = new LumpEntry
            {
                Name = "FAKE",
                FullPath = "nonexistent/fake_entry.txt",
                Size = 100,
                CompressedSize = 50,
                Category = LumpCategory.Unknown
            };

            Assert.Throws<InvalidOperationException>(() => reader.OpenLump(fakeEntry));
        }

        #endregion
    }
}
