using System;
using System.IO;
using System.Text;
using WAD.NET.Abstract;
using WAD.NET.Enums;
using WAD.NET.Interfaces;

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
        private BinaryReader _reader;
        private long _fileSize;
        private bool _readingFlats;
        private bool _readingSprites;

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

        public WadReader(Stream input, Encoding encoding = null, bool leaveOpen = false, string wadName = null)
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
                var (offset, size, name) = ReadDirectoryEntry();

                if (size == 0)
                {
                    HandleMarkerLump(name);
                }
                else
                {
                    var lump = ReadLump(offset, size, name);
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
                "IWAD" => WadType.IWAD,
                "PWAD" => WadType.PWAD,
                _ => throw new FormatException(
                    $"Unknown WAD type '{magic}'. Expected 'IWAD' or 'PWAD'.")
            };
        }

        private (int offset, int size, string name) ReadDirectoryEntry()
        {
            var offset = _reader.ReadInt32();
            var size = _reader.ReadInt32();
            var nameBytes = _reader.ReadBytes(LumpNameSize);

            // Check for LZSS compression marker
            if (nameBytes[0] == 128)
            {
                throw new NotSupportedException(
                    "LZSS compressed lumps are not supported. See: https://doomwiki.org/wiki/WAD#Compression");
            }

            var name = ReadLumpName(nameBytes);
            return (offset, size, name);
        }

        private static string ReadLumpName(byte[] nameBytes)
        {
            int length = Array.IndexOf(nameBytes, (byte)0);
            if (length < 0) length = nameBytes.Length;
            return Encoding.ASCII.GetString(nameBytes, 0, length).ToUpperInvariant();
        }

        private void HandleMarkerLump(string name)
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
            }
        }

        private ILump ReadLump(int offset, int size, string name)
        {
            ValidateLumpEntry(offset, size, name);

            var currentPosition = _reader.BaseStream.Position;

            try
            {
                _reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                var data = _reader.ReadBytes(size);

                return CreateLump(name, data);
            }
            finally
            {
                _reader.BaseStream.Seek(currentPosition, SeekOrigin.Begin);
            }
        }

        private ILump CreateLump(string name, byte[] data)
        {
            // Check marker transitions that have data (shouldn't happen but handle gracefully)
            if (name == "F_START" || name == "F_END" || name == "FF_START" || name == "FF_END")
            {
                _readingFlats = name.Contains("START");
                return null;
            }

            if (name == "S_START" || name == "S_END" || name == "SS_START" || name == "SS_END")
            {
                _readingSprites = name.Contains("START");
                return null;
            }

            // Context-aware lump creation (flats between markers)
            if (_readingFlats && data.Length == FlatLump.Size)
            {
                return new FlatLump(name, _sourceWadName, data);
            }

            // Named lump types
            return name switch
            {
                "PLAYPAL" => new PaletteLump(name, _sourceWadName, data),
                "COLORMAP" => new ColorMapLump(name, _sourceWadName, data),
                "PNAMES" => new PatchNamesLump(name, _sourceWadName, data),
                "TEXTURE1" or "TEXTURE2" => new TextureLump(name, _sourceWadName, data),
                "GENMIDI" => new MidiLump(name, _sourceWadName),
                "DMXGUS" or "DMXGUSC" => new GravisLump(name, _sourceWadName, Encoding.ASCII.GetString(data)),
                _ when name.StartsWith("DEMO") => new DemoLump(name, _sourceWadName),
                _ when name.StartsWith("DP") => new SpeakerEffectsLump(name, _sourceWadName),
                _ when name.StartsWith("DS") => CreateSoundLump(name, data),
                _ when name.StartsWith("D_") => new MusicLump(name, _sourceWadName, data),
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
            _reader = null;
        }
    }
}
