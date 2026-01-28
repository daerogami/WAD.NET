namespace WAD.NET.Enums
{
    /// <summary>
    /// Types of archive formats supported by the library.
    /// </summary>
    public enum ArchiveType
    {
        /// <summary>Standard WAD file format.</summary>
        WAD,

        /// <summary>ZIP-based PK3 archive (ZDoom/GZDoom).</summary>
        PK3,

        /// <summary>7z-based PK7 archive (high compression).</summary>
        PK7,

        /// <summary>Unpacked folder (development mode).</summary>
        Folder
    }
}
