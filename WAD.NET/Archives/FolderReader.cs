using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WAD.NET.Enums;

namespace WAD.NET.Archives
{
    /// <summary>
    /// Reader for unpacked mod folders (development/debug mode).
    /// </summary>
    public sealed class FolderReader : IArchiveReader
    {
        private readonly Dictionary<string, LumpEntry> _entries;
        private readonly string _basePath;

        /// <inheritdoc/>
        public string Path => _basePath;

        /// <inheritdoc/>
        public ArchiveType Type => ArchiveType.Folder;

        /// <summary>
        /// Creates a new folder reader.
        /// </summary>
        /// <param name="folderPath">Path to the folder to read.</param>
        public FolderReader(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                throw new DirectoryNotFoundException($"Folder not found: {folderPath}");

            _basePath = System.IO.Path.GetFullPath(folderPath);
            _entries = BuildEntryIndex();
        }

        private Dictionary<string, LumpEntry> BuildEntryIndex()
        {
            var entries = new Dictionary<string, LumpEntry>(StringComparer.OrdinalIgnoreCase);

            foreach (var file in Directory.EnumerateFiles(_basePath, "*", SearchOption.AllDirectories))
            {
                var relativePath = GetRelativePath(_basePath, file).Replace('\\', '/');
                var fileInfo = new FileInfo(file);

                var entry = new LumpEntry
                {
                    Name = GetLumpName(relativePath),
                    FullPath = relativePath,
                    CompressedSize = fileInfo.Length,
                    Size = fileInfo.Length,
                    Category = CategorizeEntry(relativePath)
                };

                var key = entry.Name;

                // Handle duplicates by using full path as key
                if (entries.ContainsKey(key))
                {
                    key = relativePath;
                }

                entries[key] = entry;
            }

            return entries;
        }

        private static string GetRelativePath(string basePath, string fullPath)
        {
            // .NET Standard 2.1 has Path.GetRelativePath
            return System.IO.Path.GetRelativePath(basePath, fullPath);
        }

        private static string GetLumpName(string relativePath)
        {
            var fileName = System.IO.Path.GetFileName(relativePath);
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
                "filter" => CategorizeFilterEntry(parts),
                _ => CategorizeByFileName(path)
            };
        }

        private static LumpCategory CategorizeFilterEntry(string[] parts)
        {
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
            var fullPath = System.IO.Path.Combine(_basePath, entry.FullPath);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"File not found: {entry.FullPath}", fullPath);

            return File.ReadAllBytes(fullPath);
        }

        /// <inheritdoc/>
        public Stream OpenLump(LumpEntry entry)
        {
            var fullPath = System.IO.Path.Combine(_basePath, entry.FullPath);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"File not found: {entry.FullPath}", fullPath);

            return File.OpenRead(fullPath);
        }

        /// <inheritdoc/>
        public bool Contains(string name) => _entries.ContainsKey(name);

        /// <summary>
        /// Get all embedded WAD files in the folder.
        /// </summary>
        public IEnumerable<string> GetEmbeddedWads()
        {
            return _entries.Values
                .Where(e => e.FullPath.EndsWith(".wad", StringComparison.OrdinalIgnoreCase))
                .Select(e => e.FullPath);
        }

        /// <summary>
        /// Get entries in a specific category.
        /// </summary>
        public IEnumerable<LumpEntry> GetEntriesByCategory(LumpCategory category)
        {
            return _entries.Values.Where(e => e.Category == category);
        }

        /// <summary>
        /// Get entries in a specific subfolder.
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
            // Nothing to dispose for folder reader
        }
    }
}
