using System;
using WAD.NET.Abstract;
using WAD.NET.Definitions;

namespace WAD.NET.Concrete
{
    /// <summary>
    /// Represents a flat (floor/ceiling texture) lump.
    /// Flats are 64x64 raw pixel data using palette indices.
    /// </summary>
    public sealed class FlatLump : Lump
    {
        /// <summary>
        /// Width of a flat in pixels.
        /// </summary>
        public const int Width = 64;

        /// <summary>
        /// Height of a flat in pixels.
        /// </summary>
        public const int Height = 64;

        /// <summary>
        /// Size of a flat in bytes (64 * 64 = 4096).
        /// </summary>
        public const int Size = Width * Height;

        /// <summary>
        /// Raw pixel data as palette indices.
        /// </summary>
        public byte[] Pixels { get; }

        public FlatLump(string lumpName, string sourceWadFileName, ReadOnlySpan<byte> data)
            : base(lumpName, sourceWadFileName)
        {
            if (data.Length < Size)
            {
                throw new FormatException(
                    $"Flat '{lumpName}' must be at least {Size} bytes, got {data.Length}");
            }

            Pixels = data.Slice(0, Size).ToArray();
        }

        /// <summary>
        /// Gets the pixel value (palette index) at the specified coordinates.
        /// </summary>
        /// <param name="x">X coordinate (0-63).</param>
        /// <param name="y">Y coordinate (0-63).</param>
        /// <returns>Palette index at the specified position.</returns>
        public byte GetPixel(int x, int y)
        {
            if (x < 0 || x >= Width)
                throw new ArgumentOutOfRangeException(nameof(x), $"X must be between 0 and {Width - 1}");
            if (y < 0 || y >= Height)
                throw new ArgumentOutOfRangeException(nameof(y), $"Y must be between 0 and {Height - 1}");

            return Pixels[y * Width + x];
        }

        /// <summary>
        /// Converts the flat to RGBA pixel data using the specified palette.
        /// </summary>
        /// <param name="palette">The palette to use for color lookup.</param>
        /// <returns>RGBA pixel data (4 bytes per pixel).</returns>
        public byte[] ToRgba(Palette palette)
        {
            var rgba = new byte[Size * 4];

            for (int i = 0; i < Size; i++)
            {
                var color = palette.Colors[Pixels[i]];
                rgba[i * 4 + 0] = color.R;
                rgba[i * 4 + 1] = color.G;
                rgba[i * 4 + 2] = color.B;
                rgba[i * 4 + 3] = 255; // Flats are always opaque
            }

            return rgba;
        }
    }
}
