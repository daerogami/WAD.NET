using WAD.NET.Enums;

namespace WAD.NET.Archives
{
    /// <summary>
    /// Represents a lump/file entry in an archive.
    /// </summary>
    public class LumpEntry
    {
        /// <summary>
        /// Lump name (8-char max for WAD, truncated filename for PK3).
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// Full path within archive (for PK3/folder), same as Name for WAD.
        /// </summary>
        public string FullPath { get; init; } = string.Empty;

        /// <summary>
        /// Compressed size in bytes.
        /// </summary>
        public long CompressedSize { get; init; }

        /// <summary>
        /// Uncompressed size in bytes.
        /// </summary>
        public long Size { get; init; }

        /// <summary>
        /// Lump category based on location/name.
        /// </summary>
        public LumpCategory Category { get; init; }

        /// <summary>
        /// Offset within the archive (for WAD files).
        /// </summary>
        public long Offset { get; init; }

        /// <summary>
        /// Whether this is a marker lump (zero size).
        /// </summary>
        public bool IsMarker => Size == 0;

        public override string ToString() => $"{Name} ({Category}, {Size} bytes)";
    }
}
