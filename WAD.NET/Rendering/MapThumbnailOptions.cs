using SixLabors.ImageSharp.PixelFormats;

namespace WAD.NET.Rendering
{
    /// <summary>
    /// Options for rendering a map thumbnail.
    /// </summary>
    public class MapThumbnailOptions
    {
        /// <summary>
        /// Width of the output image in pixels.
        /// </summary>
        public int Width { get; set; } = 512;

        /// <summary>
        /// Height of the output image in pixels.
        /// </summary>
        public int Height { get; set; } = 512;

        /// <summary>
        /// Color for one-sided (solid wall) linedefs.
        /// </summary>
        public Rgba32 LineColor { get; set; } = new Rgba32(255, 255, 255, 255);

        /// <summary>
        /// Background color of the image.
        /// </summary>
        public Rgba32 BackgroundColor { get; set; } = new Rgba32(0, 0, 0, 255);

        /// <summary>
        /// Color for secret linedefs (flag 0x0020).
        /// </summary>
        public Rgba32 SecretLineColor { get; set; } = new Rgba32(255, 0, 255, 255);

        /// <summary>
        /// Color for two-sided linedefs.
        /// </summary>
        public Rgba32 TwoSidedLineColor { get; set; } = new Rgba32(128, 128, 128, 255);

        /// <summary>
        /// Padding in pixels around the rendered geometry.
        /// </summary>
        public int Padding { get; set; } = 16;
    }
}
