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
        private BinaryReader _reader { get; set; }


        public WadReader(Stream input, Encoding encoding = null, bool leaveOpen = false)
        {
            if (encoding == null)
            {
                encoding = new UTF8Encoding();
            }
            _reader = new BinaryReader(input, encoding, leaveOpen);
        }

        public Wad ReadWad()
        {
            var wad = new Wad();

            var wadType = GetWadFileType();
            var directoryCount = _reader.ReadInt32();
            var directoryLocationPtr = _reader.ReadInt32();
            _reader.BaseStream.Seek(directoryLocationPtr, SeekOrigin.Begin);
            for (var i = 0; i < directoryCount; i++)
            {
                var (ptr, size, name) = GetNextLump();
                if (size == 0)
                {
                    HandleMarkerLump(wad, new String(name));
                }
                else
                {
                    HandleGenericLump(wad, ptr, new String(name), size);
                }
            }

            return wad;
        }

        private (int ptr, int size, char[] name) GetNextLump()
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

            return (lumpPtr, lumpSize, lumpNameChars);
        }

        private void HandleGenericLump(Wad wad, int lumpPtr, string lumpName, int lumpSize)
        {
            var lastPostion = _reader.BaseStream.Position;
            _reader.BaseStream.Seek(lumpPtr, SeekOrigin.Begin);
            var lump = LumpFactory.GenerateBinaryLumpFromData(lumpName, _reader.ReadBytes(lumpSize));
            wad.Lumps.Enqueue(lump);
            _reader.BaseStream.Seek(lastPostion, SeekOrigin.Begin);
        }

        private void HandleMarkerLump(Wad wad, string lumpName)
        {

        }

        private WadType GetWadFileType()
        {
            var wadType = new String(_reader.ReadChars(4));
            if (wadType.StartsWith("PK") || wadType.StartsWith("ZIP"))
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
