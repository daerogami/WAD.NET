using System;
using WAD.NET.Abstract;

namespace WAD.NET.Concrete
{
    /// <summary>
    /// Represents the COLORMAP lump containing light diminishing tables.
    /// </summary>
    /// <remarks>
    /// Contains 34 maps of 256 bytes each:
    /// - Maps 0-31: Light levels (0 = brightest, 31 = darkest)
    /// - Map 32: Invulnerability effect (grayscale)
    /// - Map 33: All black (unused)
    /// </remarks>
    public sealed class ColorMapLump : Lump
    {
        /// <summary>
        /// Number of color maps.
        /// </summary>
        public const int MapCount = 34;

        /// <summary>
        /// Number of entries per map (one per palette color).
        /// </summary>
        public const int EntriesPerMap = 256;

        /// <summary>
        /// Expected size of COLORMAP lump in bytes.
        /// </summary>
        public const int ExpectedSize = MapCount * EntriesPerMap;

        /// <summary>
        /// The 34 color maps, each containing 256 palette index remappings.
        /// </summary>
        public byte[][] Maps { get; }

        /// <summary>
        /// Number of light levels available (0-31).
        /// </summary>
        public const int LightLevelCount = 32;

        public ColorMapLump(string lumpName, string sourceWadFileName, ReadOnlySpan<byte> data)
            : base(lumpName, sourceWadFileName)
        {
            if (data.Length < ExpectedSize)
            {
                throw new FormatException(
                    $"COLORMAP lump must be at least {ExpectedSize} bytes, got {data.Length}");
            }

            Maps = new byte[MapCount][];
            for (int i = 0; i < MapCount; i++)
            {
                Maps[i] = data.Slice(i * EntriesPerMap, EntriesPerMap).ToArray();
            }
        }

        /// <summary>
        /// Remaps a color index based on light level.
        /// </summary>
        /// <param name="colorIndex">Original palette color index (0-255).</param>
        /// <param name="lightLevel">Light level (0 = brightest, 31 = darkest).</param>
        /// <returns>Remapped palette color index.</returns>
        public byte RemapColor(int colorIndex, int lightLevel)
        {
            if (colorIndex < 0 || colorIndex >= EntriesPerMap)
                throw new ArgumentOutOfRangeException(nameof(colorIndex));
            if (lightLevel < 0 || lightLevel >= LightLevelCount)
                throw new ArgumentOutOfRangeException(nameof(lightLevel));

            return Maps[lightLevel][colorIndex];
        }

        /// <summary>
        /// Gets the invulnerability colormap (map index 32).
        /// </summary>
        public byte[] InvulnerabilityMap => Maps[32];

        /// <summary>
        /// Gets the colormap at the specified index.
        /// </summary>
        public byte[] this[int index] => Maps[index];
    }
}
