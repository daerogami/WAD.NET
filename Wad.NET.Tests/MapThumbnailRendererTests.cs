using Xunit;
using WAD.NET.Definitions.Classic.Map;
using WAD.NET.Maps;
using WAD.NET.Rendering;

namespace WAD.NET.Tests
{
    public class MapThumbnailRendererTests
    {
        [Fact]
        public void EmptyMap_ProducesValidPng_NoException()
        {
            var map = new DoomMap();
            var png = MapThumbnailRenderer.RenderToPng(map);

            Assert.NotEmpty(png);
            // PNG magic bytes
            Assert.Equal(0x89, png[0]);
            Assert.Equal((byte)'P', png[1]);
            Assert.Equal((byte)'N', png[2]);
            Assert.Equal((byte)'G', png[3]);
        }

        [Fact]
        public void SimpleMap_RendersValidPng()
        {
            var map = new DoomMap
            {
                Name = "TEST",
                Vertices = new[]
                {
                    new MapVertex(0, 0),
                    new MapVertex(100, 0),
                    new MapVertex(100, 100),
                    new MapVertex(0, 100)
                },
                Linedefs = new[]
                {
                    new DoomLinedef { StartVertex = 0, EndVertex = 1, Flags = LinedefFlags.Impassable, FrontSidedef = 0, BackSidedef = DoomLinedef.NoSidedef },
                    new DoomLinedef { StartVertex = 1, EndVertex = 2, Flags = LinedefFlags.Impassable, FrontSidedef = 0, BackSidedef = DoomLinedef.NoSidedef },
                    new DoomLinedef { StartVertex = 2, EndVertex = 3, Flags = LinedefFlags.Impassable, FrontSidedef = 0, BackSidedef = DoomLinedef.NoSidedef },
                    new DoomLinedef { StartVertex = 3, EndVertex = 0, Flags = LinedefFlags.Impassable, FrontSidedef = 0, BackSidedef = DoomLinedef.NoSidedef },
                }
            };

            var png = MapThumbnailRenderer.RenderToPng(map);

            Assert.NotEmpty(png);
            Assert.Equal(0x89, png[0]);
            Assert.Equal((byte)'P', png[1]);
            Assert.Equal((byte)'N', png[2]);
            Assert.Equal((byte)'G', png[3]);
        }

        [Fact]
        public void RenderToImage_ReturnsCorrectDimensions()
        {
            var map = new DoomMap
            {
                Vertices = new[]
                {
                    new MapVertex(0, 0),
                    new MapVertex(100, 0),
                },
                Linedefs = new[]
                {
                    new DoomLinedef { StartVertex = 0, EndVertex = 1, Flags = LinedefFlags.Impassable, FrontSidedef = 0, BackSidedef = DoomLinedef.NoSidedef }
                }
            };

            var options = new MapThumbnailOptions { Width = 256, Height = 128 };
            using var image = MapThumbnailRenderer.RenderToImage(map, options);

            Assert.Equal(256, image.Width);
            Assert.Equal(128, image.Height);
        }

        [Fact]
        public void EmptyMap_RenderToImage_ReturnsCorrectDimensions()
        {
            var map = new DoomMap();
            var options = new MapThumbnailOptions { Width = 64, Height = 64 };
            using var image = MapThumbnailRenderer.RenderToImage(map, options);

            Assert.Equal(64, image.Width);
            Assert.Equal(64, image.Height);
        }

        [Fact]
        public void MapWithSecretAndTwoSidedLines_RendersWithoutError()
        {
            var map = new DoomMap
            {
                Vertices = new[]
                {
                    new MapVertex(0, 0),
                    new MapVertex(100, 0),
                    new MapVertex(50, 50),
                },
                Linedefs = new[]
                {
                    // One-sided wall
                    new DoomLinedef { StartVertex = 0, EndVertex = 1, Flags = LinedefFlags.Impassable, FrontSidedef = 0, BackSidedef = DoomLinedef.NoSidedef },
                    // Two-sided
                    new DoomLinedef { StartVertex = 1, EndVertex = 2, Flags = LinedefFlags.TwoSided, FrontSidedef = 0, BackSidedef = 1 },
                    // Secret
                    new DoomLinedef { StartVertex = 2, EndVertex = 0, Flags = LinedefFlags.Secret, FrontSidedef = 0, BackSidedef = DoomLinedef.NoSidedef },
                }
            };

            var png = MapThumbnailRenderer.RenderToPng(map);
            Assert.NotEmpty(png);
            // Should be larger than empty image since lines are drawn
            var emptyPng = MapThumbnailRenderer.RenderToPng(new DoomMap());
            Assert.True(png.Length >= emptyPng.Length);
        }

        [Fact]
        public void DefaultOptions_HaveExpectedValues()
        {
            var opts = new MapThumbnailOptions();
            Assert.Equal(512, opts.Width);
            Assert.Equal(512, opts.Height);
            Assert.Equal(16, opts.Padding);
        }
    }
}
