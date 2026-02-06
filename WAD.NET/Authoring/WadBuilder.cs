using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using WAD.NET.Archives;
using WAD.NET.Definitions;
using WAD.NET.Enums;

namespace WAD.NET.Authoring
{
    /// <summary>
    /// Builds WAD files programmatically with a fluent API.
    /// </summary>
    public class WadBuilder
    {
        private const int HeaderSize = 12;
        private const int DirectoryEntrySize = 16;
        private const int MaxLumpNameLength = 8;

        private readonly WadType _wadType;
        private readonly List<BuilderLump> _lumps = new List<BuilderLump>();

        /// <summary>
        /// Initializes a new <see cref="WadBuilder"/> for the specified WAD type.
        /// </summary>
        /// <param name="wadType">The WAD type to write. Defaults to <see cref="WadType.PWAD"/>.</param>
        public WadBuilder(WadType wadType = WadType.PWAD)
        {
            _wadType = wadType;
        }

        /// <summary>
        /// Creates a <see cref="WadBuilder"/> pre-populated with lumps from an existing archive.
        /// </summary>
        /// <param name="reader">The archive reader to read lumps from.</param>
        /// <param name="wadType">The WAD type for the output. Defaults to <see cref="WadType.PWAD"/>.</param>
        /// <returns>A new <see cref="WadBuilder"/> containing all lumps from the archive.</returns>
        public static WadBuilder FromArchive(IArchiveReader reader, WadType wadType = WadType.PWAD)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            var builder = new WadBuilder(wadType);
            foreach (var entry in reader.GetEntries())
            {
                if (entry.IsMarker)
                {
                    builder.AddMarker(entry.Name);
                }
                else
                {
                    builder.AddLump(entry.Name, reader.ReadLump(entry));
                }
            }
            return builder;
        }

        /// <summary>
        /// Adds a lump with the specified name and data.
        /// </summary>
        /// <param name="name">The lump name (max 8 ASCII characters).</param>
        /// <param name="data">The lump data.</param>
        /// <returns>This builder for fluent chaining.</returns>
        public WadBuilder AddLump(string name, byte[] data)
        {
            ValidateLumpName(name);
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            _lumps.Add(new BuilderLump(name.ToUpperInvariant(), data));
            return this;
        }

        /// <summary>
        /// Adds a zero-length marker lump.
        /// </summary>
        /// <param name="name">The marker name (max 8 ASCII characters).</param>
        /// <returns>This builder for fluent chaining.</returns>
        public WadBuilder AddMarker(string name)
        {
            ValidateLumpName(name);
            _lumps.Add(new BuilderLump(name.ToUpperInvariant(), Array.Empty<byte>()));
            return this;
        }

        /// <summary>
        /// Replaces the data of an existing lump.
        /// </summary>
        /// <param name="name">The lump name to replace (case-insensitive).</param>
        /// <param name="data">The new lump data.</param>
        /// <returns>This builder for fluent chaining.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if no lump with the given name exists.</exception>
        public WadBuilder ReplaceLump(string name, byte[] data)
        {
            ValidateLumpName(name);
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            var upperName = name.ToUpperInvariant();
            var index = _lumps.FindIndex(l => l.Name == upperName);
            if (index < 0)
                throw new KeyNotFoundException($"Lump '{upperName}' not found.");

            _lumps[index] = new BuilderLump(upperName, data);
            return this;
        }

        /// <summary>
        /// Removes the first lump with the specified name.
        /// </summary>
        /// <param name="name">The lump name to remove (case-insensitive).</param>
        /// <returns>This builder for fluent chaining.</returns>
        public WadBuilder RemoveLump(string name)
        {
            var upperName = name.ToUpperInvariant();
            var index = _lumps.FindIndex(l => l.Name == upperName);
            if (index >= 0)
                _lumps.RemoveAt(index);
            return this;
        }

        /// <summary>
        /// Inserts a lump at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which to insert.</param>
        /// <param name="name">The lump name (max 8 ASCII characters).</param>
        /// <param name="data">The lump data.</param>
        /// <returns>This builder for fluent chaining.</returns>
        public WadBuilder InsertLump(int index, string name, byte[] data)
        {
            ValidateLumpName(name);
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (index < 0 || index > _lumps.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            _lumps.Insert(index, new BuilderLump(name.ToUpperInvariant(), data));
            return this;
        }

        /// <summary>
        /// Returns the current directory listing as a read-only list of (Name, Size) tuples.
        /// </summary>
        public IReadOnlyList<(string Name, int Size)> GetDirectory()
        {
            return _lumps.Select(l => (l.Name, l.Data.Length)).ToList().AsReadOnly();
        }

        /// <summary>
        /// Writes a valid WAD file to the specified stream.
        /// Format: 12-byte header, lump data, then 16-byte directory entries.
        /// </summary>
        /// <param name="output">The stream to write to.</param>
        public void WriteTo(Stream output)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));

            using var writer = new BinaryWriter(output, Encoding.ASCII, leaveOpen: true);

            // Write header magic
            string magic = _wadType == WadType.IWAD ? BinaryConstants.IwadMarker : BinaryConstants.PwadMarker;
            writer.Write(Encoding.ASCII.GetBytes(magic));

            // Write lump count
            writer.Write(_lumps.Count);

            // Placeholder for directory offset - will fill in after writing lump data
            long directoryOffsetPosition = output.Position;
            writer.Write(0); // placeholder

            // Write lump data, tracking offsets
            var offsets = new int[_lumps.Count];
            for (int i = 0; i < _lumps.Count; i++)
            {
                offsets[i] = (int)output.Position;
                if (_lumps[i].Data.Length > 0)
                {
                    writer.Write(_lumps[i].Data);
                }
            }

            // Record directory offset and patch header
            int directoryOffset = (int)output.Position;
            long endPosition = output.Position;
            output.Seek(directoryOffsetPosition, SeekOrigin.Begin);
            writer.Write(directoryOffset);
            output.Seek(endPosition, SeekOrigin.Begin);

            // Write directory entries
            for (int i = 0; i < _lumps.Count; i++)
            {
                writer.Write(offsets[i]);
                writer.Write(_lumps[i].Data.Length);
                writer.Write(GetPaddedName(_lumps[i].Name));
            }
        }

        /// <summary>
        /// Writes a valid WAD file to the specified path.
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
        /// Validates that a lump name is no more than 8 ASCII characters.
        /// </summary>
        private static void ValidateLumpName(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Lump name cannot be null or empty.", nameof(name));

            if (name.Length > MaxLumpNameLength)
                throw new ArgumentException(
                    $"Lump name '{name}' exceeds maximum length of {MaxLumpNameLength} characters.", nameof(name));
        }

        /// <summary>
        /// Returns the lump name as an 8-byte null-padded ASCII array.
        /// </summary>
        private static byte[] GetPaddedName(string name)
        {
            var bytes = new byte[MaxLumpNameLength];
            var nameBytes = Encoding.ASCII.GetBytes(name);
            Array.Copy(nameBytes, bytes, Math.Min(nameBytes.Length, MaxLumpNameLength));
            return bytes;
        }

        private readonly struct BuilderLump
        {
            public string Name { get; }
            public byte[] Data { get; }

            public BuilderLump(string name, byte[] data)
            {
                Name = name;
                Data = data;
            }
        }
    }
}
