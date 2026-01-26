using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using WAD.NET.Enums;

namespace WAD.NET.Archives
{
    /// <summary>
    /// Reader for PK3 (ZIP-based) archives used by ZDoom and GZDoom.
    /// </summary>
    public sealed class Pk3Reader : IArchiveReader
    {
        private readonly ZipArchive _archive;
        private readonly Dictionary<string, LumpEntry> _entries;
        private readonly Dictionary<string, string> _entryPaths; // Name -> FullPath mapping

        /// <inheritdoc/>
        public string Path { get; }

        /// <inheritdoc/>
        public ArchiveType Type => ArchiveType.PK3;

        /// <summary>
        /// Creates a new PK3 reader from a file path.
        /// </summary>
        /// <param name="filePath">Path to the PK3 file.</param>
        public Pk3Reader(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("PK3 file not found", filePath);

            Path = filePath;
            _archive = ZipFile.OpenRead(filePath);
            (_entries, _entryPaths) = BuildEntryIndex();
        }

        /// <summary>
        /// Creates a new PK3 reader from a stream.
        /// </summary>
        /// <param name="stream">Stream containing the PK3 data.</param>
        /// <param name="leaveOpen">Whether to leave the stream open when disposed.</param>
        public Pk3Reader(Stream stream, bool leaveOpen = false)
        {
            Path = string.Empty;
            _archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen);
            (_entries, _entryPaths) = BuildEntryIndex();
        }

        private (Dictionary<string, LumpEntry>, Dictionary<string, string>) BuildEntryIndex()
        {
            var entries = new Dictionary<string, LumpEntry>(StringComparer.OrdinalIgnoreCase);
            var paths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var zipEntry in _archive.Entries)
            {
                // Skip directories
                if (string.IsNullOrEmpty(zipEntry.Name) || zipEntry.FullName.EndsWith("/"))
                    continue;

                var entry = CreateLumpEntry(zipEntry);
                var key = entry.Name;

                // Handle duplicates by using full path as key
                if (entries.ContainsKey(key))
                {
                    key = zipEntry.FullName;
                }

                entries[key] = entry;
                paths[key] = zipEntry.FullName;
            }

            return (entries, paths);
        }

        private LumpEntry CreateLumpEntry(ZipArchiveEntry zipEntry)
        {
            return new LumpEntry
            {
                Name = GetLumpName(zipEntry.FullName),
                FullPath = zipEntry.FullName,
                CompressedSize = zipEntry.CompressedLength,
                Size = zipEntry.Length,
                Category = CategorizeEntry(zipEntry.FullName)
            };
        }

        private static string GetLumpName(string fullPath)
        {
            var fileName = System.IO.Path.GetFileName(fullPath);
            var nameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(fileName);

            // Truncate to 8 chars for WAD compatibility
            if (nameWithoutExt.Length > 8)
                return nameWithoutExt.Substring(0, 8).ToUpperInvariant();

            return nameWithoutExt.ToUpperInvariant();
        }

        private static LumpCategory CategorizeEntry(string path)
        {
            var parts = path.Split('/', '\\');
            if (parts.Length < 1)
                return LumpCategory.Unknown;

            var folder = parts[0].ToLowerInvariant();

            return folder switch
            {
                "maps" => LumpCategory.Map,
                "sprites" => LumpCategory.Sprite,
                "flats" => LumpCategory.Flat,
                "textures" => LumpCategory.Texture,
                "patches" => LumpCategory.Patch,
                "sounds" => LumpCategory.Sound,
                "music" => LumpCategory.Music,
                "graphics" => LumpCategory.Graphic,
                "acs" => LumpCategory.ACS,
                "zscript" or "decorate" => LumpCategory.Script,
                "hires" => LumpCategory.Texture,
                "colormaps" => LumpCategory.Graphic,
                "brightmaps" => LumpCategory.Texture,
                "fonts" => LumpCategory.Graphic,
                "models" => LumpCategory.Unknown,
                "voxels" => LumpCategory.Unknown,
                "filter" => CategorizeFilterEntry(parts),
                _ => CategorizeByFileName(path)
            };
        }

        private static LumpCategory CategorizeFilterEntry(string[] parts)
        {
            // filter/game.id/category/file
            if (parts.Length < 3)
                return LumpCategory.Unknown;

            var subFolder = parts[2].ToLowerInvariant();
            return subFolder switch
            {
                "sprites" => LumpCategory.Sprite,
                "flats" => LumpCategory.Flat,
                "textures" => LumpCategory.Texture,
                "sounds" => LumpCategory.Sound,
                "music" => LumpCategory.Music,
                "graphics" => LumpCategory.Graphic,
                _ => LumpCategory.Unknown
            };
        }

        private static LumpCategory CategorizeByFileName(string path)
        {
            var fileName = System.IO.Path.GetFileName(path).ToUpperInvariant();
            var nameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(fileName).ToUpperInvariant();

            // Definition files
            if (nameWithoutExt is "MAPINFO" or "ZMAPINFO" or "UMAPINFO")
                return LumpCategory.Definition;
            if (nameWithoutExt is "SNDINFO" or "SNDSEQ")
                return LumpCategory.Definition;
            if (nameWithoutExt is "TEXTURES" or "ANIMDEFS" or "GLDEFS")
                return LumpCategory.Definition;
            if (nameWithoutExt is "TERRAIN" or "MENUDEF" or "LOCKDEFS")
                return LumpCategory.Definition;

            // Script files
            if (nameWithoutExt is "DECORATE" or "ZSCRIPT")
                return LumpCategory.Script;

            // Extension-based
            var ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
            return ext switch
            {
                ".wad" => LumpCategory.Map,
                ".ogg" or ".mp3" or ".flac" or ".mid" or ".mus" => LumpCategory.Music,
                ".wav" => LumpCategory.Sound,
                ".png" or ".jpg" or ".jpeg" or ".tga" => LumpCategory.Graphic,
                ".zs" or ".zsc" => LumpCategory.Script,
                ".txt" or ".dec" => LumpCategory.Script,
                ".acs" or ".o" => LumpCategory.ACS,
                _ => LumpCategory.Unknown
            };
        }

        /// <inheritdoc/>
        public IEnumerable<LumpEntry> GetEntries() => _entries.Values;

        /// <inheritdoc/>
        public LumpEntry GetEntry(string name)
        {
            return _entries.TryGetValue(name, out var entry) ? entry : null;
        }

        /// <inheritdoc/>
        public byte[] ReadLump(LumpEntry entry)
        {
            var zipEntry = _archive.GetEntry(entry.FullPath);
            if (zipEntry == null)
                throw new InvalidOperationException($"Entry not found: {entry.FullPath}");

            using var stream = zipEntry.Open();
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }

        /// <inheritdoc/>
        public Stream OpenLump(LumpEntry entry)
        {
            var zipEntry = _archive.GetEntry(entry.FullPath);
            if (zipEntry == null)
                throw new InvalidOperationException($"Entry not found: {entry.FullPath}");

            // Return a MemoryStream because ZipArchiveEntry.Open() stream doesn't support seeking
            var ms = new MemoryStream();
            using (var stream = zipEntry.Open())
            {
                stream.CopyTo(ms);
            }
            ms.Position = 0;
            return ms;
        }

        /// <inheritdoc/>
        public bool Contains(string name) => _entries.ContainsKey(name);

        /// <summary>
        /// Get all embedded WAD files in the archive.
        /// </summary>
        public IEnumerable<string> GetEmbeddedWads()
        {
            return _entries.Values
                .Where(e => e.FullPath.EndsWith(".wad", StringComparison.OrdinalIgnoreCase))
                .Select(e => e.FullPath);
        }

        /// <summary>
        /// Opens an embedded WAD file as a WadArchiveReader.
        /// </summary>
        /// <param name="wadPath">Full path to the WAD within the archive.</param>
        /// <returns>A WadArchiveReader for the embedded WAD.</returns>
        public WadArchiveReader OpenEmbeddedWad(string wadPath)
        {
            var entry = _entries.Values
                .FirstOrDefault(e => e.FullPath.Equals(wadPath, StringComparison.OrdinalIgnoreCase));

            if (entry == null)
                throw new FileNotFoundException($"Embedded WAD not found: {wadPath}");

            var zipEntry = _archive.GetEntry(entry.FullPath);
            if (zipEntry == null)
                throw new InvalidOperationException($"Entry not found in archive: {entry.FullPath}");

            // Read WAD into memory (WADs are typically small enough)
            var ms = new MemoryStream();
            using (var stream = zipEntry.Open())
            {
                stream.CopyTo(ms);
            }
            ms.Position = 0;

            return new WadArchiveReader(ms, leaveOpen: false, wadName: entry.Name);
        }

        /// <summary>
        /// Get entries in a specific category.
        /// </summary>
        public IEnumerable<LumpEntry> GetEntriesByCategory(LumpCategory category)
        {
            return _entries.Values.Where(e => e.Category == category);
        }

        /// <summary>
        /// Get entries by folder path.
        /// </summary>
        public IEnumerable<LumpEntry> GetEntriesInFolder(string folderPath)
        {
            var normalizedPath = folderPath.TrimEnd('/', '\\').ToLowerInvariant() + "/";
            return _entries.Values.Where(e =>
                e.FullPath.ToLowerInvariant().StartsWith(normalizedPath));
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _archive?.Dispose();
        }
    }
}
