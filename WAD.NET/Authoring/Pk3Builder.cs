using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using WAD.NET.Archives;

namespace WAD.NET.Authoring
{
    /// <summary>
    /// Builds PK3 (ZIP-based) archives programmatically with a fluent API.
    /// </summary>
    public class Pk3Builder
    {
        private readonly Dictionary<string, byte[]> _entries =
            new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initializes a new empty <see cref="Pk3Builder"/>.
        /// </summary>
        public Pk3Builder()
        {
        }

        /// <summary>
        /// Creates a <see cref="Pk3Builder"/> pre-populated with entries from an existing archive.
        /// </summary>
        /// <param name="reader">The archive reader to read entries from.</param>
        /// <returns>A new <see cref="Pk3Builder"/> containing all entries from the archive.</returns>
        public static Pk3Builder FromArchive(IArchiveReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            var builder = new Pk3Builder();
            foreach (var entry in reader.GetEntries())
            {
                var path = string.IsNullOrEmpty(entry.FullPath) ? entry.Name : entry.FullPath;
                builder.AddEntry(path, reader.ReadLump(entry));
            }
            return builder;
        }

        /// <summary>
        /// Adds a file entry at the given path within the archive.
        /// </summary>
        /// <param name="path">The entry path (e.g., "maps/MAP01.wad"). Backslashes are normalized to forward slashes.</param>
        /// <param name="data">The entry data.</param>
        /// <returns>This builder for fluent chaining.</returns>
        /// <exception cref="ArgumentException">Thrown if the path is null or empty, or if an entry with this path already exists.</exception>
        public Pk3Builder AddEntry(string path, byte[] data)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Entry path cannot be null or empty.", nameof(path));
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            var normalized = NormalizePath(path);
            if (_entries.ContainsKey(normalized))
                throw new ArgumentException($"Entry '{normalized}' already exists. Use ReplaceEntry to update it.", nameof(path));

            _entries[normalized] = data;
            return this;
        }

        /// <summary>
        /// Replaces an existing entry's data.
        /// </summary>
        /// <param name="path">The entry path to replace (case-insensitive).</param>
        /// <param name="data">The new entry data.</param>
        /// <returns>This builder for fluent chaining.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if no entry with the given path exists.</exception>
        public Pk3Builder ReplaceEntry(string path, byte[] data)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Entry path cannot be null or empty.", nameof(path));
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            var normalized = NormalizePath(path);
            if (!_entries.ContainsKey(normalized))
                throw new KeyNotFoundException($"Entry '{normalized}' not found.");

            _entries[normalized] = data;
            return this;
        }

        /// <summary>
        /// Removes an entry by path.
        /// </summary>
        /// <param name="path">The entry path to remove (case-insensitive).</param>
        /// <returns>This builder for fluent chaining.</returns>
        public Pk3Builder RemoveEntry(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                _entries.Remove(NormalizePath(path));
            }
            return this;
        }

        /// <summary>
        /// Returns the current entries as a read-only list of (Path, Size) tuples.
        /// </summary>
        public IReadOnlyList<(string Path, int Size)> GetEntries()
        {
            return _entries.Select(kv => (kv.Key, kv.Value.Length)).ToList().AsReadOnly();
        }

        /// <summary>
        /// Writes the PK3 as a valid ZIP archive to the specified stream.
        /// </summary>
        /// <param name="output">The stream to write to.</param>
        public void WriteTo(Stream output)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));

            using var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true);
            foreach (var kvp in _entries)
            {
                var entry = archive.CreateEntry(kvp.Key, CompressionLevel.Optimal);
                using var entryStream = entry.Open();
                entryStream.Write(kvp.Value, 0, kvp.Value.Length);
            }
        }

        /// <summary>
        /// Writes the PK3 as a valid ZIP archive to the specified file path.
        /// </summary>
        /// <param name="path">The output file path.</param>
        public void WriteToFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));

            using var stream = File.Create(path);
            WriteTo(stream);
        }

        /// <summary>
        /// Normalizes a path by replacing backslashes with forward slashes
        /// and trimming leading slashes.
        /// </summary>
        private static string NormalizePath(string path)
        {
            return path.Replace('\\', '/').TrimStart('/');
        }
    }
}
