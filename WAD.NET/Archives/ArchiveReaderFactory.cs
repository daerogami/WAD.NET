using System;
using System.IO;
using System.Text;
using WAD.NET.Definitions;

namespace WAD.NET.Archives
{
    /// <summary>
    /// Factory for creating the appropriate archive reader based on file content.
    /// </summary>
    public static class ArchiveReaderFactory
    {
        // Magic bytes for file type detection
        private static readonly byte[] WadMagicIWAD = Encoding.ASCII.GetBytes(BinaryConstants.IwadMarker);
        private static readonly byte[] WadMagicPWAD = Encoding.ASCII.GetBytes(BinaryConstants.PwadMarker);
        private static readonly byte[] ZipMagic = { (byte)'P', (byte)'K', 0x03, 0x04 };
        private static readonly byte[] SevenZipMagic = { 0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C };

        /// <summary>
        /// Opens an archive file or folder with the appropriate reader.
        /// </summary>
        /// <param name="path">Path to the file or folder.</param>
        /// <returns>An archive reader for the specified path.</returns>
        /// <exception cref="FileNotFoundException">File or folder not found.</exception>
        /// <exception cref="FormatException">Unknown archive format.</exception>
        public static IArchiveReader Open(string path)
        {
            if (Directory.Exists(path))
            {
                return new FolderReader(path);
            }

            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Archive not found", path);
            }

            // Detect by magic bytes, not extension
            var archiveType = DetectArchiveType(path);

            return archiveType switch
            {
                ArchiveTypeDetected.WAD => new WadArchiveReader(path),
                ArchiveTypeDetected.ZIP => new Pk3Reader(path),
                ArchiveTypeDetected.SevenZip => new Pk7Reader(path),
                _ => throw new FormatException($"Unknown archive format: {path}")
            };
        }

        /// <summary>
        /// Detects the archive type from a file's magic bytes.
        /// </summary>
        /// <param name="filePath">Path to the file.</param>
        /// <returns>The detected archive type.</returns>
        public static ArchiveTypeDetected DetectArchiveType(string filePath)
        {
            using var fs = File.OpenRead(filePath);
            return DetectArchiveType(fs);
        }

        /// <summary>
        /// Detects the archive type from a stream's magic bytes.
        /// </summary>
        /// <param name="stream">The stream to read from (must be seekable).</param>
        /// <returns>The detected archive type.</returns>
        public static ArchiveTypeDetected DetectArchiveType(Stream stream)
        {
            if (stream.Length < 4)
                return ArchiveTypeDetected.Unknown;

            var originalPosition = stream.Position;
            // Read 6 bytes to accommodate 7z magic (6 bytes)
            var magic = new byte[6];

            try
            {
                stream.Position = 0;
                var bytesRead = stream.Read(magic, 0, 6);
                if (bytesRead < 4)
                    return ArchiveTypeDetected.Unknown;

                // Check WAD (IWAD or PWAD)
                if (MatchesMagic(magic, WadMagicIWAD) || MatchesMagic(magic, WadMagicPWAD))
                {
                    return ArchiveTypeDetected.WAD;
                }

                // Check ZIP/PK3
                if (MatchesMagic(magic, ZipMagic))
                {
                    return ArchiveTypeDetected.ZIP;
                }

                // Check 7z/PK7 (requires 6 bytes)
                if (bytesRead >= 6 && MatchesMagic(magic, SevenZipMagic))
                {
                    return ArchiveTypeDetected.SevenZip;
                }

                return ArchiveTypeDetected.Unknown;
            }
            finally
            {
                // Restore original position if stream is seekable
                if (stream.CanSeek)
                {
                    stream.Position = originalPosition;
                }
            }
        }

        private static bool MatchesMagic(byte[] data, byte[] magic)
        {
            if (data.Length < magic.Length)
                return false;

            for (int i = 0; i < magic.Length; i++)
            {
                if (data[i] != magic[i])
                    return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Archive types that can be detected from magic bytes.
    /// </summary>
    public enum ArchiveTypeDetected
    {
        /// <summary>Unknown or unrecognized format.</summary>
        Unknown,

        /// <summary>WAD file (IWAD or PWAD).</summary>
        WAD,

        /// <summary>ZIP archive (PK3, PKZ, PKE).</summary>
        ZIP,

        /// <summary>7z archive (PK7).</summary>
        SevenZip
    }
}
