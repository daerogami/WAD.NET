using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using WAD.NET.Abstract;
using WAD.NET.Enums;
using WAD.NET.Interfaces;

namespace WAD.NET.Concrete
{
    /// <summary>
    /// Reader for loading wad into memory for evaluation
    /// </summary>
    /// <remarks>
    /// Implemented following UDMF spec
    /// <see cref="https://github.com/coelckers/gzdoom/blob/master/specs/udmf.txt"/>
    /// </remarks>
    public class WadReader : IWadReader
    {
        private string _sourceWadName;
        private BinaryReader _reader { get; set; }
        private bool _readingFlats;


        public WadReader(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException();
            }
            _sourceWadName = Path.GetFileName(filePath);
            _reader = new BinaryReader(File.OpenRead(filePath));
        }

        public WadReader(Stream input, Encoding encoding = null, bool leaveOpen = false, string wadName = null)
        {
            if (encoding == null)
            {
                encoding = new UTF8Encoding();
            }
            _reader = new BinaryReader(input, encoding, leaveOpen);
        }

        public Wad ReadWad()
        {
            var _wad = new Wad
            {
                Name = _sourceWadName
            };

            var wadType = GetWadFileType();
            var directoryCount = _reader.ReadInt32();
            var directoryLocationPtr = _reader.ReadInt32();
            _reader.BaseStream.Seek(directoryLocationPtr, SeekOrigin.Begin);
            for (var i = 0; i < directoryCount; i++)
            {
                var (offset, size, name) = GetNextLumpInfo();
                if (size == 0)
                {
                    HandleMarkerLump(_wad, name);
                }
                else
                {
                    HandleDataLump(_wad, offset, name, size);
                }
            }

            return _wad;
        }

        private (int offset, int size, string name) GetNextLumpInfo()
        {
            var lumpPtr = _reader.ReadInt32();
            var lumpSize = _reader.ReadInt32();
            var lumpNameChars = _reader.ReadChars(8);
            var specialCompressionMarker = (char)128;
            if (lumpNameChars[0] == specialCompressionMarker)
            {
                //https://doomwiki.org/wiki/WAD#Compression
                throw new NotImplementedException("Lump Uses LZSS Compression");
            }
            var lumpNameAsString = new string(lumpNameChars);

            return (lumpPtr, lumpSize, lumpNameAsString);
        }

        private void HandleDataLump(Wad wad, int lumpPtr, string lumpName, int lumpSize)
        {
            var lastPostion = _reader.BaseStream.Position;
            _reader.BaseStream.Seek(lumpPtr, SeekOrigin.Begin);
            var data = _reader.ReadBytes(lumpSize);
            ILump lump;
            if (_readingFlats)
            {
                lump = new FlatLump(lumpName, _sourceWadName);
            }
            else if (lumpName.Equals("PLAYPAL"))
            {
                lump = new PaletteLump(lumpName, _sourceWadName);
            }
            else if (lumpName.Equals("DEMO1") || lumpName.Equals("DEMO2") || lumpName.Equals("DEMO3"))
            {
                lump = new DemoLump(lumpName, _sourceWadName);
            }
            else if (lumpName.Equals("TEXTURE1") || lumpName.Equals("TEXTURE2"))
            {
                lump = new TextureListLump(lumpName, _sourceWadName);
            }
            else if (lumpName.Equals("PNAMES"))
            {
                lump = new WallPatchLump(lumpName, _sourceWadName);
            }
            else if (lumpName.Equals("GENMIDI"))
            {
                lump = new MidiLump(lumpName, _sourceWadName);
            }
            else if (lumpName.StartsWith("DMXGUS"))
            {
                lump = new GravisLump(lumpName, _sourceWadName, data.ToString());
            }
            else if (lumpName.StartsWith("DP"))
            {
                lump = new SpeakerEffectsLump(lumpName, _sourceWadName);
            }
            else if (lumpName.StartsWith("DS"))
            {
                lump = new SoundEffectsLump(lumpName, _sourceWadName);
            }
            else if (lumpName.StartsWith("D_"))
            {
                lump = new MusicLump(lumpName, _sourceWadName, data);
            }
            else if (lumpName.Equals("F_START"))
            {
                _readingFlats = true;
                return;
            }
            else if (lumpName.Equals("F_END"))
            {
                _readingFlats = false;
                return;
            }
            else
            {
                lump = new BinaryLump(lumpName, _sourceWadName, data); // Other Graphics?
            }
            wad.Lumps.Enqueue(lump);
            _reader.BaseStream.Seek(lastPostion, SeekOrigin.Begin);
        }

        private void HandleMarkerLump(Wad wad, string lumpName)
        {

        }

        private WadType GetWadFileType()
        {
            var wadType = new string(_reader.ReadChars(4));
            if (wadType.StartsWith("PK") || wadType.StartsWith("ZIP") || wadType.StartsWith("7z"))
            {
                throw new FormatException($"Cannot use WadReader to read compressed data. Use CompressedWadReader instead.");
            }
            switch (wadType)
            {
                case "IWAD":
                    return WadType.IWAD;
                case "PWAD":
                    return WadType.IWAD;
                default:
                    // HIGH: This might happen for valid wads that are compressed (pk3/zip)
                    throw new FormatException($"Unknown wad file type of '{wadType}'. Expecting 'IWAD' or 'PWAD'");
            }
        }

        public void Dispose()
        {
            _reader.Dispose();
            _reader = null;
        }
    }
}
