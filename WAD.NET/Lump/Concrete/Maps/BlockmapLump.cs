using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using WAD.NET.Abstract;
using WAD.NET.Definitions;
using WAD.NET.Interfaces;

namespace WAD.NET.Concrete.Maps
{
    /// <summary>
    /// Lump containing the blockmap for a DOOM format map.
    /// </summary>
    /// <remarks>
    /// The blockmap divides the map into a grid of 128x128 unit blocks
    /// for efficient collision detection.
    /// </remarks>
    public sealed class BlockmapLump : Lump, IMapLump
    {
        /// <summary>
        /// Size of each block in map units.
        /// </summary>
        public const int BlockSize = 128;

        /// <summary>
        /// X origin of the blockmap grid.
        /// </summary>
        public short OriginX { get; }

        /// <summary>
        /// Y origin of the blockmap grid.
        /// </summary>
        public short OriginY { get; }

        /// <summary>
        /// Number of columns in the blockmap.
        /// </summary>
        public short Columns { get; }

        /// <summary>
        /// Number of rows in the blockmap.
        /// </summary>
        public short Rows { get; }

        /// <summary>
        /// Raw blockmap data.
        /// </summary>
        public byte[] Data { get; }

        /// <summary>
        /// Total number of blocks in the blockmap.
        /// </summary>
        public int BlockCount => Columns * Rows;

        /// <summary>
        /// Creates a new BlockmapLump by parsing binary data.
        /// </summary>
        /// <param name="name">Lump name.</param>
        /// <param name="source">Source WAD name.</param>
        /// <param name="data">Binary lump data.</param>
        public BlockmapLump(string name, string source, ReadOnlySpan<byte> data)
            : base(name, source)
        {
            if (data.Length < 8)
            {
                // Empty or invalid blockmap
                OriginX = 0;
                OriginY = 0;
                Columns = 0;
                Rows = 0;
                Data = Array.Empty<byte>();
                return;
            }

            OriginX = BinaryPrimitives.ReadInt16LittleEndian(data);
            OriginY = BinaryPrimitives.ReadInt16LittleEndian(data.Slice(2));
            Columns = BinaryPrimitives.ReadInt16LittleEndian(data.Slice(4));
            Rows = BinaryPrimitives.ReadInt16LittleEndian(data.Slice(6));
            Data = data.ToArray();
        }

        /// <summary>
        /// Gets the block index for a given map coordinate.
        /// </summary>
        /// <param name="x">X coordinate in map units.</param>
        /// <param name="y">Y coordinate in map units.</param>
        /// <returns>Block index, or -1 if outside the blockmap.</returns>
        public int GetBlockIndex(int x, int y)
        {
            int col = (x - OriginX) / BlockSize;
            int row = (y - OriginY) / BlockSize;

            if (col < 0 || col >= Columns || row < 0 || row >= Rows)
                return -1;

            return row * Columns + col;
        }

        /// <summary>
        /// Gets the linedefs in a specific block.
        /// </summary>
        /// <param name="blockIndex">The block index.</param>
        /// <returns>Array of linedef indices in this block.</returns>
        public ushort[] GetLinedefs(int blockIndex)
        {
            if (blockIndex < 0 || blockIndex >= BlockCount)
                return Array.Empty<ushort>();

            // Get offset for this block (offsets start at byte 8)
            int offsetIndex = 8 + blockIndex * 2;
            if (offsetIndex + 2 > Data.Length)
                return Array.Empty<ushort>();

            int offset = BinaryPrimitives.ReadUInt16LittleEndian(Data.AsSpan(offsetIndex)) * 2;
            if (offset >= Data.Length)
                return Array.Empty<ushort>();

            var linedefs = new List<ushort>();

            // Skip the initial 0x0000 marker
            offset += 2;

            // Read linedefs until 0xFFFF terminator
            while (offset + 2 <= Data.Length)
            {
                ushort linedef = BinaryPrimitives.ReadUInt16LittleEndian(Data.AsSpan(offset));
                if (linedef == BinaryConstants.BlockmapTerminator)
                    break;

                linedefs.Add(linedef);
                offset += 2;
            }

            return linedefs.ToArray();
        }

        /// <summary>
        /// Gets the linedefs at a specific map coordinate.
        /// </summary>
        /// <param name="x">X coordinate in map units.</param>
        /// <param name="y">Y coordinate in map units.</param>
        /// <returns>Array of linedef indices at this location.</returns>
        public ushort[] GetLinedefsAt(int x, int y)
        {
            int blockIndex = GetBlockIndex(x, y);
            return GetLinedefs(blockIndex);
        }
    }
}
