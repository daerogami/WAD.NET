using System;

namespace WAD.NET.Definitions
{
    /// <summary>
    /// Represents a 256-color palette used by DOOM.
    /// </summary>
    public class Palette
    {
        /// <summary>
        /// Size of a single palette in bytes (256 colors * 3 bytes per color).
        /// </summary>
        public const int ByteSize = 768;

        /// <summary>
        /// Number of colors in a palette.
        /// </summary>
        public const int ColorCount = 256;

        /// <summary>
        /// The 256 colors in this palette.
        /// </summary>
        public Color[] Colors { get; }

        public Palette(ReadOnlySpan<byte> data)
        {
            if (data.Length < ByteSize)
            {
                throw new FormatException(
                    $"Palette data must be at least {ByteSize} bytes, got {data.Length}");
            }

            Colors = new Color[ColorCount];
            for (int i = 0; i < ColorCount; i++)
            {
                Colors[i] = new Color(
                    data[i * 3],
                    data[i * 3 + 1],
                    data[i * 3 + 2]
                );
            }
        }

        /// <summary>
        /// Gets the color at the specified index.
        /// </summary>
        public Color this[int index] => Colors[index];
    }
}
