using System;

namespace WAD.NET.Definitions
{
    /// <summary>
    /// Represents a DOOM picture (sprite, patch, or menu graphic) in decoded form.
    /// Uses palette indices with 255 indicating transparency.
    /// </summary>
    public class DoomPicture
    {
        /// <summary>
        /// Palette index value indicating transparency.
        /// </summary>
        public const byte TransparentIndex = 255;

        /// <summary>
        /// Width of the picture in pixels.
        /// </summary>
        public ushort Width { get; }

        /// <summary>
        /// Height of the picture in pixels.
        /// </summary>
        public ushort Height { get; }

        /// <summary>
        /// Left offset for sprite rendering (distance from center).
        /// </summary>
        public short LeftOffset { get; }

        /// <summary>
        /// Top offset for sprite rendering (distance from bottom).
        /// </summary>
        public short TopOffset { get; }

        /// <summary>
        /// Pixel data as palette indices.
        /// Value 255 indicates transparency.
        /// Access: pixels[y * Width + x]
        /// </summary>
        public byte[] Pixels { get; }

        public DoomPicture(ushort width, ushort height, short leftOffset, short topOffset, byte[] pixels)
        {
            if (pixels == null)
                throw new ArgumentNullException(nameof(pixels));

            if (pixels.Length != width * height)
                throw new ArgumentException(
                    $"Pixel array length ({pixels.Length}) must match Width * Height ({width * height})");

            Width = width;
            Height = height;
            LeftOffset = leftOffset;
            TopOffset = topOffset;
            Pixels = pixels;
        }

        /// <summary>
        /// Gets the palette index at the specified coordinates.
        /// Returns TransparentIndex (255) for out-of-bounds coordinates.
        /// </summary>
        public byte GetPixel(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return TransparentIndex;
            return Pixels[y * Width + x];
        }

        /// <summary>
        /// Checks if the pixel at the specified coordinates is transparent.
        /// </summary>
        public bool IsTransparent(int x, int y)
        {
            return GetPixel(x, y) == TransparentIndex;
        }

        /// <summary>
        /// Convert to RGBA pixels using a palette.
        /// Transparent pixels will have alpha = 0.
        /// </summary>
        public byte[] ToRgba(Palette palette)
        {
            if (palette == null)
                throw new ArgumentNullException(nameof(palette));

            var rgba = new byte[Width * Height * 4];

            for (int i = 0; i < Pixels.Length; i++)
            {
                var colorIndex = Pixels[i];
                if (colorIndex == TransparentIndex)
                {
                    // Transparent pixel
                    rgba[i * 4 + 0] = 0;
                    rgba[i * 4 + 1] = 0;
                    rgba[i * 4 + 2] = 0;
                    rgba[i * 4 + 3] = 0;
                }
                else
                {
                    var color = palette.Colors[colorIndex];
                    rgba[i * 4 + 0] = color.R;
                    rgba[i * 4 + 1] = color.G;
                    rgba[i * 4 + 2] = color.B;
                    rgba[i * 4 + 3] = 255;
                }
            }

            return rgba;
        }
    }
}
