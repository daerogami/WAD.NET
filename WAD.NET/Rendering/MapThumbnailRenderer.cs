using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using WAD.NET.Definitions.Classic.Map;
using WAD.NET.Maps;

namespace WAD.NET.Rendering
{
    /// <summary>
    /// Renders map geometry as a thumbnail image.
    /// </summary>
    public static class MapThumbnailRenderer
    {
        /// <summary>
        /// Renders a DoomMap to an Image&lt;Rgba32&gt;.
        /// </summary>
        /// <param name="map">The map to render.</param>
        /// <param name="options">Rendering options, or null for defaults.</param>
        /// <returns>The rendered image.</returns>
        public static Image<Rgba32> RenderToImage(DoomMap map, MapThumbnailOptions? options = null)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            options ??= new MapThumbnailOptions();

            var image = CreateBlankImage(options);

            if (map.Linedefs == null || map.Linedefs.Length == 0 ||
                map.Vertices == null || map.Vertices.Length == 0)
            {
                return image;
            }

            ComputeBounds(map.Linedefs, map.Vertices, out float minX, out float minY, out float maxX, out float maxY);
            if (!TryComputeTransform(options, minX, minY, maxX, maxY, out float scale, out float offsetX, out float offsetY))
            {
                return image;
            }

            foreach (var linedef in map.Linedefs)
            {
                if (linedef.StartVertex >= map.Vertices.Length ||
                    linedef.EndVertex >= map.Vertices.Length)
                    continue;

                var p1 = Transform(map.Vertices[linedef.StartVertex], minX, maxY, scale, offsetX, offsetY);
                var p2 = Transform(map.Vertices[linedef.EndVertex], minX, maxY, scale, offsetX, offsetY);

                Rgba32 color;
                if ((linedef.Flags & LinedefFlags.Secret) != 0)
                    color = options.SecretLineColor;
                else if (linedef.IsTwoSided)
                    color = options.TwoSidedLineColor;
                else
                    color = options.LineColor;

                DrawLine(image, (int)p1.x, (int)p1.y, (int)p2.x, (int)p2.y, color);
            }

            return image;
        }

        /// <summary>
        /// Renders a HexenMap to an Image&lt;Rgba32&gt;.
        /// </summary>
        /// <param name="map">The map to render.</param>
        /// <param name="options">Rendering options, or null for defaults.</param>
        /// <returns>The rendered image.</returns>
        public static Image<Rgba32> RenderToImage(HexenMap map, MapThumbnailOptions? options = null)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            options ??= new MapThumbnailOptions();

            var image = CreateBlankImage(options);

            if (map.Linedefs == null || map.Linedefs.Length == 0 ||
                map.Vertices == null || map.Vertices.Length == 0)
            {
                return image;
            }

            // Compute bounds from Hexen linedefs (same vertex type)
            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            foreach (var linedef in map.Linedefs)
            {
                if (linedef.StartVertex >= map.Vertices.Length ||
                    linedef.EndVertex >= map.Vertices.Length)
                    continue;

                var v1 = map.Vertices[linedef.StartVertex];
                var v2 = map.Vertices[linedef.EndVertex];

                minX = Math.Min(minX, Math.Min(v1.X, v2.X));
                minY = Math.Min(minY, Math.Min(v1.Y, v2.Y));
                maxX = Math.Max(maxX, Math.Max(v1.X, v2.X));
                maxY = Math.Max(maxY, Math.Max(v1.Y, v2.Y));
            }

            if (!TryComputeTransform(options, minX, minY, maxX, maxY, out float scale, out float offsetX, out float offsetY))
            {
                return image;
            }

            foreach (var linedef in map.Linedefs)
            {
                if (linedef.StartVertex >= map.Vertices.Length ||
                    linedef.EndVertex >= map.Vertices.Length)
                    continue;

                var p1 = Transform(map.Vertices[linedef.StartVertex], minX, maxY, scale, offsetX, offsetY);
                var p2 = Transform(map.Vertices[linedef.EndVertex], minX, maxY, scale, offsetX, offsetY);

                Rgba32 color;
                if ((linedef.Flags & Definitions.Hexen.HexenLinedefFlags.Secret) != 0)
                    color = options.SecretLineColor;
                else if (linedef.IsTwoSided)
                    color = options.TwoSidedLineColor;
                else
                    color = options.LineColor;

                DrawLine(image, (int)p1.x, (int)p1.y, (int)p2.x, (int)p2.y, color);
            }

            return image;
        }

        /// <summary>
        /// Renders a DoomMap to PNG bytes.
        /// </summary>
        public static byte[] RenderToPng(DoomMap map, MapThumbnailOptions? options = null)
        {
            using var image = RenderToImage(map, options);
            using var ms = new MemoryStream();
            image.SaveAsPng(ms);
            return ms.ToArray();
        }

        /// <summary>
        /// Renders a HexenMap to PNG bytes.
        /// </summary>
        public static byte[] RenderToPng(HexenMap map, MapThumbnailOptions? options = null)
        {
            using var image = RenderToImage(map, options);
            using var ms = new MemoryStream();
            image.SaveAsPng(ms);
            return ms.ToArray();
        }

        private static Image<Rgba32> CreateBlankImage(MapThumbnailOptions options)
        {
            var image = new Image<Rgba32>(options.Width, options.Height);

            for (int y = 0; y < options.Height; y++)
            {
                for (int x = 0; x < options.Width; x++)
                {
                    image[x, y] = options.BackgroundColor;
                }
            }

            return image;
        }

        private static void ComputeBounds(DoomLinedef[] linedefs, MapVertex[] vertices,
            out float minX, out float minY, out float maxX, out float maxY)
        {
            minX = float.MaxValue;
            minY = float.MaxValue;
            maxX = float.MinValue;
            maxY = float.MinValue;

            foreach (var linedef in linedefs)
            {
                if (linedef.StartVertex >= vertices.Length ||
                    linedef.EndVertex >= vertices.Length)
                    continue;

                var v1 = vertices[linedef.StartVertex];
                var v2 = vertices[linedef.EndVertex];

                minX = Math.Min(minX, Math.Min(v1.X, v2.X));
                minY = Math.Min(minY, Math.Min(v1.Y, v2.Y));
                maxX = Math.Max(maxX, Math.Max(v1.X, v2.X));
                maxY = Math.Max(maxY, Math.Max(v1.Y, v2.Y));
            }
        }

        private static bool TryComputeTransform(MapThumbnailOptions options,
            float minX, float minY, float maxX, float maxY,
            out float scale, out float offsetX, out float offsetY)
        {
            float mapWidth = maxX - minX;
            float mapHeight = maxY - minY;

            scale = 0;
            offsetX = 0;
            offsetY = 0;

            if (mapWidth <= 0 && mapHeight <= 0)
                return false;

            float drawWidth = options.Width - options.Padding * 2;
            float drawHeight = options.Height - options.Padding * 2;

            if (drawWidth <= 0 || drawHeight <= 0)
                return false;

            float scaleX = mapWidth > 0 ? drawWidth / mapWidth : float.MaxValue;
            float scaleY = mapHeight > 0 ? drawHeight / mapHeight : float.MaxValue;
            scale = Math.Min(scaleX, scaleY);

            float scaledWidth = mapWidth * scale;
            float scaledHeight = mapHeight * scale;
            offsetX = options.Padding + (drawWidth - scaledWidth) / 2f;
            offsetY = options.Padding + (drawHeight - scaledHeight) / 2f;

            return true;
        }

        private static (float x, float y) Transform(MapVertex v, float minX, float maxY, float scale, float offsetX, float offsetY)
        {
            float x = (v.X - minX) * scale + offsetX;
            float y = (maxY - v.Y) * scale + offsetY; // DOOM Y-axis is inverted
            return (x, y);
        }

        /// <summary>
        /// Draws a line using Bresenham's algorithm.
        /// </summary>
        private static void DrawLine(Image<Rgba32> image, int x0, int y0, int x1, int y1, Rgba32 color)
        {
            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                if (x0 >= 0 && x0 < image.Width && y0 >= 0 && y0 < image.Height)
                {
                    image[x0, y0] = color;
                }

                if (x0 == x1 && y0 == y1)
                    break;

                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }
    }
}
