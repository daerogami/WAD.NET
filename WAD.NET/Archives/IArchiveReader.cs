using System;
using System.Collections.Generic;
using System.IO;
using WAD.NET.Enums;

namespace WAD.NET.Archives
{
    /// <summary>
    /// Base interface for all archive readers (WAD, PK3, PK7, folder).
    /// </summary>
    public interface IArchiveReader : IDisposable
    {
        /// <summary>
        /// Archive file path or folder path.
        /// </summary>
        string Path { get; }

        /// <summary>
        /// Archive type (WAD, PK3, PK7, Folder).
        /// </summary>
        ArchiveType Type { get; }

        /// <summary>
        /// Get all lump entries in the archive.
        /// </summary>
        IEnumerable<LumpEntry> GetEntries();

        /// <summary>
        /// Get a specific lump by name.
        /// </summary>
        /// <param name="name">The lump name to search for.</param>
        /// <returns>The lump entry, or null if not found.</returns>
        LumpEntry GetEntry(string name);

        /// <summary>
        /// Read lump data into a byte array.
        /// </summary>
        /// <param name="entry">The lump entry to read.</param>
        /// <returns>The lump data as a byte array.</returns>
        byte[] ReadLump(LumpEntry entry);

        /// <summary>
        /// Read lump data as a stream.
        /// </summary>
        /// <param name="entry">The lump entry to read.</param>
        /// <returns>A stream containing the lump data.</returns>
        Stream OpenLump(LumpEntry entry);

        /// <summary>
        /// Check if archive contains a lump.
        /// </summary>
        /// <param name="name">The lump name to check.</param>
        /// <returns>True if the lump exists, false otherwise.</returns>
        bool Contains(string name);
    }
}
