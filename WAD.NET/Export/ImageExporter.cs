using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using WAD.NET.Concrete;
using WAD.NET.Definitions;

namespace WAD.NET.Export
{
    /// <summary>
    /// Utility for exporting DOOM graphics to common image formats.
    /// </summary>
    public static class ImageExporter
    {
        /// <summary>
        /// Exports a DoomPicture to a TGA file (Targa format with alpha support).
        /// </summary>
        /// <param name="picture">The picture to export.</param>
        /// <param name="palette">The palette to use for color lookup.</param>
        /// <param name="output">The output stream.</param>
        public static void ExportTga(DoomPicture picture, Palette palette, Stream output)
        {
            if (picture == null) throw new ArgumentNullException(nameof(picture));
            if (palette == null) throw new ArgumentNullException(nameof(palette));
            if (output == null) throw new ArgumentNullException(nameof(output));

            var rgba = picture.ToRgba(palette);
            WriteTga(output, picture.Width, picture.Height, rgba);
        }

        /// <summary>
        /// Exports a FlatLump to a TGA file.
        /// </summary>
        public static void ExportTga(FlatLump flat, Palette palette, Stream output)
        {
            if (flat == null) throw new ArgumentNullException(nameof(flat));
            if (palette == null) throw new ArgumentNullException(nameof(palette));
            if (output == null) throw new ArgumentNullException(nameof(output));

            var rgba = flat.ToRgba(palette);
            WriteTga(output, FlatLump.Width, FlatLump.Height, rgba);
        }

        /// <summary>
        /// Exports raw RGBA data to a TGA file.
        /// </summary>
        public static void ExportTga(int width, int height, byte[] rgba, Stream output)
        {
            if (rgba == null) throw new ArgumentNullException(nameof(rgba));
            if (output == null) throw new ArgumentNullException(nameof(output));
            if (rgba.Length != width * height * 4)
                throw new ArgumentException("RGBA array length must match width * height * 4");

            WriteTga(output, width, height, rgba);
        }

        /// <summary>
        /// Exports a DoomPicture to a BMP file (24-bit, no alpha).
        /// </summary>
        /// <remarks>
        /// Transparent pixels are rendered as magenta (255, 0, 255).
        /// </remarks>
        public static void ExportBmp(DoomPicture picture, Palette palette, Stream output)
        {
            if (picture == null) throw new ArgumentNullException(nameof(picture));
            if (palette == null) throw new ArgumentNullException(nameof(palette));
            if (output == null) throw new ArgumentNullException(nameof(output));

            var rgba = picture.ToRgba(palette);
            WriteBmp(output, picture.Width, picture.Height, rgba);
        }

        /// <summary>
        /// Exports a FlatLump to a BMP file.
        /// </summary>
        public static void ExportBmp(FlatLump flat, Palette palette, Stream output)
        {
            if (flat == null) throw new ArgumentNullException(nameof(flat));
            if (palette == null) throw new ArgumentNullException(nameof(palette));
            if (output == null) throw new ArgumentNullException(nameof(output));

            var rgba = flat.ToRgba(palette);
            WriteBmp(output, FlatLump.Width, FlatLump.Height, rgba);
        }

        /// <summary>
        /// Exports raw RGBA data to a BMP file.
        /// </summary>
        public static void ExportBmp(int width, int height, byte[] rgba, Stream output)
        {
            if (rgba == null) throw new ArgumentNullException(nameof(rgba));
            if (output == null) throw new ArgumentNullException(nameof(output));
            if (rgba.Length != width * height * 4)
                throw new ArgumentException("RGBA array length must match width * height * 4");

            WriteBmp(output, width, height, rgba);
        }

        /// <summary>
        /// Exports a DoomPicture to a TGA file at the specified path.
        /// </summary>
        public static void ExportTga(DoomPicture picture, Palette palette, string filePath)
        {
            using var fs = File.Create(filePath);
            ExportTga(picture, palette, fs);
        }

        /// <summary>
        /// Exports a FlatLump to a TGA file at the specified path.
        /// </summary>
        public static void ExportTga(FlatLump flat, Palette palette, string filePath)
        {
            using var fs = File.Create(filePath);
            ExportTga(flat, palette, fs);
        }

        /// <summary>
        /// Exports a DoomPicture to a BMP file at the specified path.
        /// </summary>
        public static void ExportBmp(DoomPicture picture, Palette palette, string filePath)
        {
            using var fs = File.Create(filePath);
            ExportBmp(picture, palette, fs);
        }

        /// <summary>
        /// Exports a FlatLump to a BMP file at the specified path.
        /// </summary>
        public static void ExportBmp(FlatLump flat, Palette palette, string filePath)
        {
            using var fs = File.Create(filePath);
            ExportBmp(flat, palette, fs);
        }

        /// <summary>
        /// Exports a DoomPicture to a PNG file (with alpha support).
        /// </summary>
        /// <param name="picture">The picture to export.</param>
        /// <param name="palette">The palette to use for color lookup.</param>
        /// <param name="output">The output stream.</param>
        public static void ExportPng(DoomPicture picture, Palette palette, Stream output)
        {
            if (picture == null) throw new ArgumentNullException(nameof(picture));
            if (palette == null) throw new ArgumentNullException(nameof(palette));
            if (output == null) throw new ArgumentNullException(nameof(output));

            var rgba = picture.ToRgba(palette);
            WritePng(output, picture.Width, picture.Height, rgba);
        }

        /// <summary>
        /// Exports a FlatLump to a PNG file.
        /// </summary>
        public static void ExportPng(FlatLump flat, Palette palette, Stream output)
        {
            if (flat == null) throw new ArgumentNullException(nameof(flat));
            if (palette == null) throw new ArgumentNullException(nameof(palette));
            if (output == null) throw new ArgumentNullException(nameof(output));

            var rgba = flat.ToRgba(palette);
            WritePng(output, FlatLump.Width, FlatLump.Height, rgba);
        }

        /// <summary>
        /// Exports raw RGBA data to a PNG file.
        /// </summary>
        public static void ExportPng(int width, int height, byte[] rgba, Stream output)
        {
            if (rgba == null) throw new ArgumentNullException(nameof(rgba));
            if (output == null) throw new ArgumentNullException(nameof(output));
            if (rgba.Length != width * height * 4)
                throw new ArgumentException("RGBA array length must match width * height * 4");

            WritePng(output, width, height, rgba);
        }

        /// <summary>
        /// Exports a DoomPicture to a PNG file at the specified path.
        /// </summary>
        public static void ExportPng(DoomPicture picture, Palette palette, string filePath)
        {
            using var fs = File.Create(filePath);
            ExportPng(picture, palette, fs);
        }

        /// <summary>
        /// Exports a FlatLump to a PNG file at the specified path.
        /// </summary>
        public static void ExportPng(FlatLump flat, Palette palette, string filePath)
        {
            using var fs = File.Create(filePath);
            ExportPng(flat, palette, fs);
        }

        /// <summary>
        /// Exports raw RGBA data to a PNG file at the specified path.
        /// </summary>
        public static void ExportPng(int width, int height, byte[] rgba, string filePath)
        {
            using var fs = File.Create(filePath);
            ExportPng(width, height, rgba, fs);
        }

        /// <summary>
        /// Writes TGA (Targa) format with 32-bit BGRA.
        /// </summary>
        private static void WriteTga(Stream output, int width, int height, byte[] rgba)
        {
            using var writer = new BinaryWriter(output, System.Text.Encoding.ASCII, leaveOpen: true);

            // TGA header (18 bytes)
            writer.Write((byte)0);           // ID length
            writer.Write((byte)0);           // Color map type (no color map)
            writer.Write((byte)2);           // Image type (uncompressed true-color)
            writer.Write((short)0);          // Color map first entry
            writer.Write((short)0);          // Color map length
            writer.Write((byte)0);           // Color map entry size
            writer.Write((short)0);          // X origin
            writer.Write((short)0);          // Y origin
            writer.Write((short)width);      // Width
            writer.Write((short)height);     // Height
            writer.Write((byte)32);          // Bits per pixel (32-bit BGRA)
            writer.Write((byte)0x28);        // Image descriptor (top-left origin, 8 alpha bits)

            // Write pixel data (BGRA format, top-to-bottom)
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int i = (y * width + x) * 4;
                    byte r = rgba[i + 0];
                    byte g = rgba[i + 1];
                    byte b = rgba[i + 2];
                    byte a = rgba[i + 3];

                    // TGA uses BGRA order
                    writer.Write(b);
                    writer.Write(g);
                    writer.Write(r);
                    writer.Write(a);
                }
            }
        }

        /// <summary>
        /// Writes BMP format with 24-bit BGR (no alpha).
        /// Transparent pixels become magenta.
        /// </summary>
        private static void WriteBmp(Stream output, int width, int height, byte[] rgba)
        {
            using var writer = new BinaryWriter(output, System.Text.Encoding.ASCII, leaveOpen: true);

            // BMP row padding to 4-byte boundary
            int rowStride = (width * 3 + 3) & ~3;
            int imageSize = rowStride * height;
            int fileSize = 54 + imageSize;

            // BMP file header (14 bytes)
            writer.Write((byte)'B');
            writer.Write((byte)'M');
            writer.Write(fileSize);          // File size
            writer.Write((short)0);          // Reserved
            writer.Write((short)0);          // Reserved
            writer.Write(54);                // Pixel data offset

            // DIB header (BITMAPINFOHEADER, 40 bytes)
            writer.Write(40);                // Header size
            writer.Write(width);             // Width
            writer.Write(height);            // Height (positive = bottom-up)
            writer.Write((short)1);          // Planes
            writer.Write((short)24);         // Bits per pixel
            writer.Write(0);                 // Compression (none)
            writer.Write(imageSize);         // Image size
            writer.Write(2835);              // X pixels per meter (~72 DPI)
            writer.Write(2835);              // Y pixels per meter
            writer.Write(0);                 // Colors used
            writer.Write(0);                 // Important colors

            // Write pixel data (BGR format, bottom-to-top)
            var rowPadding = new byte[rowStride - width * 3];

            for (int y = height - 1; y >= 0; y--)
            {
                for (int x = 0; x < width; x++)
                {
                    int i = (y * width + x) * 4;
                    byte r = rgba[i + 0];
                    byte g = rgba[i + 1];
                    byte b = rgba[i + 2];
                    byte a = rgba[i + 3];

                    // Replace transparent with magenta
                    if (a == 0)
                    {
                        r = 255;
                        g = 0;
                        b = 255;
                    }

                    // BMP uses BGR order
                    writer.Write(b);
                    writer.Write(g);
                    writer.Write(r);
                }

                // Row padding
                if (rowPadding.Length > 0)
                    writer.Write(rowPadding);
            }
        }

        /// <summary>
        /// Writes PNG format using ImageSharp with 32-bit RGBA.
        /// </summary>
        private static void WritePng(Stream output, int width, int height, byte[] rgba)
        {
            using var image = Image.LoadPixelData<Rgba32>(rgba, width, height);
            image.SaveAsPng(output);
        }
    }
}
