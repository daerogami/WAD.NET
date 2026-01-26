using System;
using WAD.NET.Abstract;
using WAD.NET.Interfaces;

namespace WAD.NET.Concrete.Maps
{
    /// <summary>
    /// Lump containing the reject table for a DOOM format map.
    /// </summary>
    /// <remarks>
    /// The reject table is a bit array that determines which sectors
    /// can "see" other sectors for monster AI line-of-sight checks.
    /// </remarks>
    public sealed class RejectLump : Lump, IMapLump
    {
        /// <summary>
        /// Raw reject table data.
        /// </summary>
        public byte[] Data { get; }

        /// <summary>
        /// Number of sectors this reject table covers.
        /// </summary>
        public int SectorCount { get; }

        /// <summary>
        /// Creates a new RejectLump.
        /// </summary>
        /// <param name="name">Lump name.</param>
        /// <param name="source">Source WAD name.</param>
        /// <param name="data">Binary lump data.</param>
        /// <param name="sectorCount">Number of sectors in the map (optional, used for validation).</param>
        public RejectLump(string name, string source, ReadOnlySpan<byte> data, int sectorCount = 0)
            : base(name, source)
        {
            Data = data.ToArray();

            // Calculate sector count from data size if not provided
            // Size = ceil(sectorCount^2 / 8)
            if (sectorCount > 0)
            {
                SectorCount = sectorCount;
            }
            else if (data.Length > 0)
            {
                // Estimate sector count: n^2/8 = length, so n = sqrt(length * 8)
                SectorCount = (int)Math.Sqrt(data.Length * 8);
            }
        }

        /// <summary>
        /// Checks if monsters in sector A can see monsters in sector B.
        /// </summary>
        /// <param name="sectorA">First sector index.</param>
        /// <param name="sectorB">Second sector index.</param>
        /// <returns>True if line of sight is rejected (blocked), false if allowed.</returns>
        public bool IsRejected(int sectorA, int sectorB)
        {
            if (SectorCount == 0)
                return false;

            // The bit index in the table
            int bitIndex = sectorA * SectorCount + sectorB;
            int byteIndex = bitIndex / 8;
            int bitOffset = bitIndex % 8;

            if (byteIndex >= Data.Length)
                return false;

            return (Data[byteIndex] & (1 << bitOffset)) != 0;
        }

        /// <summary>
        /// Checks if monsters in sector A can see monsters in sector B.
        /// </summary>
        /// <param name="sectorA">First sector index.</param>
        /// <param name="sectorB">Second sector index.</param>
        /// <returns>True if line of sight is allowed.</returns>
        public bool CanSee(int sectorA, int sectorB) => !IsRejected(sectorA, sectorB);
    }
}
