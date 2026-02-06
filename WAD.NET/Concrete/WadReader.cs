using System;
using System.IO;
using System.Text;
using WAD.NET.Abstract;
using WAD.NET.Compression;
using WAD.NET.Concrete.Maps;
using WAD.NET.Definitions;
using WAD.NET.Enums;
using WAD.NET.Interfaces;
using WAD.NET.Maps;

namespace WAD.NET.Concrete
{
    /// <summary>
    /// Reader for loading WAD files into memory for evaluation.
    /// </summary>
    /// <remarks>
    /// WAD file format specification: https://doomwiki.org/wiki/WAD
    /// </remarks>
    public class WadReader : IWadReader
    {
        private const int HeaderSize = 12;
        private const int DirectoryEntrySize = 16;
        private const int LumpNameSize = 8;

        private string _sourceWadName;
        private BinaryReader _reader = null!;
        private long _fileSize;
        private bool _readingFlats;
        private bool _readingSprites;
        private bool _readingPatches;

        public WadReader(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"WAD file not found: {filePath}", filePath);
            }

            _sourceWadName = Path.GetFileName(filePath);
            var stream = File.OpenRead(filePath);
            _fileSize = stream.Length;
            _reader = new BinaryReader(stream);
        }

        public WadReader(Stream input, Encoding? encoding = null, bool leaveOpen = false, string? wadName = null)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            encoding ??= Encoding.ASCII;
            _sourceWadName = wadName ?? string.Empty;
            _fileSize = input.Length;
            _reader = new BinaryReader(input, encoding, leaveOpen);
        }

        public Wad ReadWad()
        {
            ValidateMinimumFileSize();

            var wad = new Wad
            {
                Name = _sourceWadName
            };

            wad.WadType = ReadWadType();
            var directoryCount = _reader.ReadInt32();
            var directoryOffset = _reader.ReadInt32();

            ValidateDirectory(directoryCount, directoryOffset);

            _reader.BaseStream.Seek(directoryOffset, SeekOrigin.Begin);

            for (var i = 0; i < directoryCount; i++)
            {
                var (offset, size, name, isCompressed) = ReadDirectoryEntry();

                if (size == 0)
                {
                    HandleMarkerLump(name, wad);
                }
                else
                {
                    var lump = ReadLump(offset, size, name, isCompressed);
                    if (lump != null)
                    {
                        wad.Lumps.Enqueue(lump);
                    }
                }
            }

            return wad;
        }

        private void ValidateMinimumFileSize()
        {
            if (_fileSize < HeaderSize)
            {
                throw new FormatException(
                    $"WAD file too small: expected at least {HeaderSize} bytes, got {_fileSize}");
            }
        }

        private void ValidateDirectory(int lumpCount, int directoryOffset)
        {
            if (directoryOffset < HeaderSize)
            {
                throw new FormatException(
                    $"Invalid directory offset {directoryOffset}: cannot be before header");
            }

            if (directoryOffset > _fileSize)
            {
                throw new FormatException(
                    $"Directory offset {directoryOffset} exceeds file size {_fileSize}");
            }

            long requiredSize = directoryOffset + (long)lumpCount * DirectoryEntrySize;
            if (requiredSize > _fileSize)
            {
                throw new FormatException(
                    $"Directory extends beyond file: needs {requiredSize} bytes, file is {_fileSize}");
            }
        }

        private void ValidateLumpEntry(int offset, int size, string name)
        {
            if (size <= 0) return;

            if (offset < 0)
            {
                throw new FormatException(
                    $"Lump '{name}' has negative offset {offset}");
            }

            if (offset > _fileSize)
            {
                throw new FormatException(
                    $"Lump '{name}' offset {offset} exceeds file size {_fileSize}");
            }

            if ((long)offset + size > _fileSize)
            {
                throw new FormatException(
                    $"Lump '{name}' extends beyond file: offset {offset}, size {size}, file size {_fileSize}");
            }
        }

        private WadType ReadWadType()
        {
            var magic = new string(_reader.ReadChars(4));

            // Check for archive formats
            if (magic.StartsWith("PK") || magic.StartsWith("7z"))
            {
                throw new FormatException(
                    "This file appears to be a PK3/ZIP archive. Use a PK3 reader instead.");
            }

            return magic switch
            {
                BinaryConstants.IwadMarker => WadType.IWAD,
                BinaryConstants.PwadMarker => WadType.PWAD,
                _ => throw new FormatException(
                    $"Unknown WAD type '{magic}'. Expected '{BinaryConstants.IwadMarker}' or '{BinaryConstants.PwadMarker}'.")
            };
        }

        private (int offset, int size, string name, bool isCompressed) ReadDirectoryEntry()
        {
            var offset = _reader.ReadInt32();
            var size = _reader.ReadInt32();
            var nameBytes = _reader.ReadBytes(LumpNameSize);

            // Check for LZSS compression marker (high bit set on first byte)
            bool isCompressed = (nameBytes[0] & 0x80) != 0;
            if (isCompressed)
            {
                // Clear the compression marker bit to get the actual name
                nameBytes[0] = (byte)(nameBytes[0] & 0x7F);
            }

            var name = ReadLumpName(nameBytes);
            return (offset, size, name, isCompressed);
        }

        private static string ReadLumpName(byte[] nameBytes)
        {
            int length = Array.IndexOf(nameBytes, (byte)0);
            if (length < 0) length = nameBytes.Length;
            return Encoding.ASCII.GetString(nameBytes, 0, length).ToUpperInvariant();
        }

        private void HandleMarkerLump(string name, Wad wad)
        {
            switch (name)
            {
                case "F_START":
                case "FF_START":
                    _readingFlats = true;
                    break;
                case "F_END":
                case "FF_END":
                    _readingFlats = false;
                    break;
                case "S_START":
                case "SS_START":
                    _readingSprites = true;
                    break;
                case "S_END":
                case "SS_END":
                    _readingSprites = false;
                    break;
                case "P_START":
                case "PP_START":
                case "P1_START":
                case "P2_START":
                case "P3_START":
                    _readingPatches = true;
                    break;
                case "P_END":
                case "PP_END":
                case "P1_END":
                case "P2_END":
                case "P3_END":
                    _readingPatches = false;
                    break;
            }

            // Add map markers to the lump queue so MapReader can find them
            if (MapDetector.IsMapMarker(name))
            {
                wad.Lumps.Enqueue(new BinaryLump(name, _sourceWadName, Array.Empty<byte>()));
            }
        }

        private ILump ReadLump(int offset, int size, string name, bool isCompressed = false)
        {
            ValidateLumpEntry(offset, size, name);

            var currentPosition = _reader.BaseStream.Position;

            try
            {
                _reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                var data = _reader.ReadBytes(size);

                if (isCompressed)
                {
                    data = DecompressLzssData(data, name);
                }

                return CreateLump(name, data);
            }
            finally
            {
                _reader.BaseStream.Seek(currentPosition, SeekOrigin.Begin);
            }
        }

        /// <summary>
        /// Decompresses LZSS-compressed lump data.
        /// </summary>
        /// <param name="compressedData">The compressed lump data with size header.</param>
        /// <param name="lumpName">The name of the lump (for error messages).</param>
        /// <returns>The decompressed data.</returns>
        /// <exception cref="FormatException">Thrown when decompression fails.</exception>
        private static byte[] DecompressLzssData(byte[] compressedData, string lumpName)
        {
            try
            {
                // Jaguar DOOM LZSS compressed lumps have a 4-byte header containing
                // the uncompressed size, followed by the compressed data
                return LzssDecompressor.DecompressWithHeader(compressedData);
            }
            catch (Exception ex) when (ex is FormatException || ex is ArgumentException)
            {
                throw new FormatException(
                    $"Failed to decompress LZSS-compressed lump '{lumpName}': {ex.Message}", ex);
            }
        }

        private ILump CreateLump(string name, byte[] data)
        {
            // Check marker transitions that have data (shouldn't happen but handle gracefully)
            if (name == "F_START" || name == "F_END" || name == "FF_START" || name == "FF_END")
            {
                _readingFlats = name.Contains("START");
                return new BinaryLump(name, _sourceWadName, data);
            }

            if (name == "S_START" || name == "S_END" || name == "SS_START" || name == "SS_END")
            {
                _readingSprites = name.Contains("START");
                return new BinaryLump(name, _sourceWadName, data);
            }

            if (name == "P_START" || name == "P_END" || name == "PP_START" || name == "PP_END" ||
                name == "P1_START" || name == "P1_END" || name == "P2_START" || name == "P2_END" ||
                name == "P3_START" || name == "P3_END")
            {
                _readingPatches = name.Contains("START");
                return new BinaryLump(name, _sourceWadName, data);
            }

            // Context-aware lump creation (flats between markers)
            if (_readingFlats && data.Length == FlatLump.Size)
            {
                return new FlatLump(name, _sourceWadName, data);
            }

            // Sprites between S_START/S_END markers
            if (_readingSprites && data.Length > 8 && PictureLump.LooksLikePicture(data))
            {
                try
                {
                    return new PictureLump(name, _sourceWadName, data);
                }
                catch (FormatException)
                {
                    // Fall through to default handling
                }
            }

            // Patches between P_START/P_END markers
            if (_readingPatches && data.Length > 8 && PictureLump.LooksLikePicture(data))
            {
                try
                {
                    return new PictureLump(name, _sourceWadName, data);
                }
                catch (FormatException)
                {
                    // Fall through to default handling
                }
            }

            // Named lump types
            return name switch
            {
                // Resource lumps
                "PLAYPAL" => new PaletteLump(name, _sourceWadName, data),
                "COLORMAP" => new ColorMapLump(name, _sourceWadName, data),
                "PNAMES" => new PatchNamesLump(name, _sourceWadName, data),
                "TEXTURE1" or "TEXTURE2" => new TextureLump(name, _sourceWadName, data),
                "GENMIDI" => new MidiLump(name, _sourceWadName),
                "DMXGUS" or "DMXGUSC" => new GravisLump(name, _sourceWadName, Encoding.ASCII.GetString(data)),

                // Map lumps (binary format)
                "THINGS" => new ThingsLump(name, _sourceWadName, data),
                "LINEDEFS" => new LinedefsLump(name, _sourceWadName, data),
                "SIDEDEFS" => new SidedefsLump(name, _sourceWadName, data),
                "VERTEXES" => new VertexesLump(name, _sourceWadName, data),
                "SEGS" => new SegsLump(name, _sourceWadName, data),
                "SSECTORS" => new SubsectorsLump(name, _sourceWadName, data),
                "NODES" => new NodesLump(name, _sourceWadName, data),
                "SECTORS" => new SectorsLump(name, _sourceWadName, data),
                "REJECT" => new RejectLump(name, _sourceWadName, data),
                "BLOCKMAP" => new BlockmapLump(name, _sourceWadName, data),
                "BEHAVIOR" => new BehaviorLump(name, _sourceWadName, data),

                // UDMF map lumps
                "TEXTMAP" => new TextMapLump(name, _sourceWadName, data),

                // Prefix-based lumps
                _ when name.StartsWith("DEMO") => new DemoLump(name, _sourceWadName),
                _ when name.StartsWith("DP") => new SpeakerEffectsLump(name, _sourceWadName),
                _ when name.StartsWith("DS") => CreateSoundLump(name, data),
                _ when name.StartsWith("D_") => new MusicLump(name, _sourceWadName, data),

                // Map markers are stored as BinaryLump (they're just markers with no real data parsing)
                _ when MapDetector.IsMapMarker(name) => new BinaryLump(name, _sourceWadName, data),

                _ => new BinaryLump(name, _sourceWadName, data)
            };
        }

        private ILump CreateSoundLump(string name, byte[] data)
        {
            try
            {
                return new SoundLump(name, _sourceWadName, data);
            }
            catch (FormatException)
            {
                // Fall back to binary lump if sound parsing fails
                return new BinaryLump(name, _sourceWadName, data);
            }
        }

        public void Dispose()
        {
            _reader?.Dispose();
            _reader = null!;
        }
    }
}
