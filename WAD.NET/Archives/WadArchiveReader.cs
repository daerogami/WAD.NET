using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using WAD.NET.Concrete;
using WAD.NET.Enums;
using WAD.NET.Maps;

namespace WAD.NET.Archives
{
    /// <summary>
    /// Archive reader adapter for WAD files implementing IArchiveReader.
    /// </summary>
    public sealed class WadArchiveReader : IArchiveReader
    {
        private const int HeaderSize = 12;
        private const int DirectoryEntrySize = 16;
        private const int LumpNameSize = 8;

        private readonly BinaryReader _reader;
        private readonly List<LumpEntry> _entries;
        private readonly string _filePath;
        private readonly bool _leaveOpen;

        /// <inheritdoc/>
        public string Path => _filePath;

        /// <inheritdoc/>
        public ArchiveType Type => ArchiveType.WAD;

        /// <summary>
        /// Gets the WAD type (IWAD or PWAD).
        /// </summary>
        public WadType WadType { get; private set; }

        /// <summary>
        /// Creates a new WAD archive reader from a file path.
        /// </summary>
        /// <param name="filePath">Path to the WAD file.</param>
        public WadArchiveReader(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("WAD file not found", filePath);

            _filePath = filePath;
            _leaveOpen = false;
            _reader = new BinaryReader(File.OpenRead(filePath));
            _entries = ReadDirectory();
        }

        /// <summary>
        /// Creates a new WAD archive reader from a stream.
        /// </summary>
        /// <param name="stream">Stream containing WAD data.</param>
        /// <param name="leaveOpen">Whether to leave the stream open when disposed.</param>
        /// <param name="wadName">Optional name for the WAD.</param>
        public WadArchiveReader(Stream stream, bool leaveOpen = false, string? wadName = null)
        {
            _filePath = wadName ?? string.Empty;
            _leaveOpen = leaveOpen;
            _reader = new BinaryReader(stream, Encoding.ASCII, leaveOpen);
            _entries = ReadDirectory();
        }

        private List<LumpEntry> ReadDirectory()
        {
            // Read header
            var magic = new string(_reader.ReadChars(4));
            WadType = magic switch
            {
                "IWAD" => WadType.IWAD,
                "PWAD" => WadType.PWAD,
                _ => throw new FormatException($"Invalid WAD header: {magic}")
            };

            var lumpCount = _reader.ReadInt32();
            var directoryOffset = _reader.ReadInt32();

            // Read directory
            _reader.BaseStream.Seek(directoryOffset, SeekOrigin.Begin);

            var entries = new List<LumpEntry>(lumpCount);
            var inFlats = false;
            var inSprites = false;

            for (int i = 0; i < lumpCount; i++)
            {
                var offset = _reader.ReadInt32();
                var size = _reader.ReadInt32();
                var nameBytes = _reader.ReadBytes(LumpNameSize);
                var name = ReadLumpName(nameBytes);

                // Track marker sections
                switch (name)
                {
                    case "F_START":
                    case "FF_START":
                        inFlats = true;
                        break;
                    case "F_END":
                    case "FF_END":
                        inFlats = false;
                        break;
                    case "S_START":
                    case "SS_START":
                        inSprites = true;
                        break;
                    case "S_END":
                    case "SS_END":
                        inSprites = false;
                        break;
                }

                var category = CategorizeLump(name, size, inFlats, inSprites);

                entries.Add(new LumpEntry
                {
                    Name = name,
                    FullPath = name,
                    Offset = offset,
                    CompressedSize = size,
                    Size = size,
                    Category = category
                });
            }

            return entries;
        }

        private static string ReadLumpName(byte[] nameBytes)
        {
            int length = Array.IndexOf(nameBytes, (byte)0);
            if (length < 0) length = nameBytes.Length;
            return Encoding.ASCII.GetString(nameBytes, 0, length).ToUpperInvariant();
        }

        private static LumpCategory CategorizeLump(string name, int size, bool inFlats, bool inSprites)
        {
            // Map markers (usually zero-size, check first)
            if (MapDetector.IsMapMarker(name))
                return LumpCategory.Map;

            // Map lumps
            if (MapDetector.IsMapLump(name))
                return LumpCategory.Map;

            // Zero-size lumps are markers (but not map markers)
            if (size == 0)
                return LumpCategory.Unknown;

            // Section-based categorization
            if (inFlats)
                return LumpCategory.Flat;
            if (inSprites)
                return LumpCategory.Sprite;

            // Named resources
            return name switch
            {
                "PLAYPAL" or "COLORMAP" => LumpCategory.Graphic,
                "PNAMES" or "TEXTURE1" or "TEXTURE2" => LumpCategory.Texture,
                "GENMIDI" or "DMXGUS" or "DMXGUSC" => LumpCategory.Sound,
                _ when name.StartsWith("D_") => LumpCategory.Music,
                _ when name.StartsWith("DS") => LumpCategory.Sound,
                _ when name.StartsWith("DP") => LumpCategory.Sound,
                _ when name.StartsWith("DEMO") => LumpCategory.Unknown,
                _ => LumpCategory.Unknown
            };
        }

        /// <inheritdoc/>
        public IEnumerable<LumpEntry> GetEntries() => _entries;

        /// <inheritdoc/>
        public LumpEntry GetEntry(string name)
        {
            return _entries.FirstOrDefault(e =>
                e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        /// <inheritdoc/>
        public byte[] ReadLump(LumpEntry entry)
        {
            if (entry.Size == 0)
                return Array.Empty<byte>();

            _reader.BaseStream.Seek(entry.Offset, SeekOrigin.Begin);
            return _reader.ReadBytes((int)entry.Size);
        }

        /// <inheritdoc/>
        public Stream OpenLump(LumpEntry entry)
        {
            var data = ReadLump(entry);
            return new MemoryStream(data, writable: false);
        }

        /// <inheritdoc/>
        public bool Contains(string name) => GetEntry(name) != null;

        /// <summary>
        /// Get entries in a specific category.
        /// </summary>
        public IEnumerable<LumpEntry> GetEntriesByCategory(LumpCategory category)
        {
            return _entries.Where(e => e.Category == category);
        }

        /// <summary>
        /// Gets all map names in the WAD.
        /// </summary>
        public IEnumerable<string> GetMapNames()
        {
            return _entries
                .Where(e => MapDetector.IsMapMarker(e.Name))
                .Select(e => e.Name);
        }

        /// <summary>
        /// Reads the WAD using the legacy WadReader API for full lump parsing.
        /// </summary>
        /// <returns>A Wad object with parsed lumps.</returns>
        public Wad ReadWad()
        {
            // Reset stream position
            _reader.BaseStream.Seek(0, SeekOrigin.Begin);

            // Use the existing WadReader implementation
            using var legacyReader = new WadReader(
                _reader.BaseStream,
                Encoding.ASCII,
                leaveOpen: true,
                wadName: System.IO.Path.GetFileName(_filePath));

            return legacyReader.ReadWad();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!_leaveOpen)
            {
                _reader?.Dispose();
            }
        }
    }
}
