# Rendering

WAD.NET can render map geometry as thumbnail images, useful for generating previews in mod managers, level editors, or documentation tools.

## Map Thumbnails

`MapThumbnailRenderer` draws map linedefs onto an image using [Bresenham's line algorithm](https://en.wikipedia.org/wiki/Bresenham%27s_line_algorithm), producing automap-style previews.

### Basic Rendering

```csharp
using WAD.NET.Archives;
using WAD.NET.Maps;
using WAD.NET.Rendering;

using var wadReader = new WadArchiveReader("DOOM2.WAD");
var wad = wadReader.ReadWad();
var mapReader = new MapReader();

var map = mapReader.ReadMap(wad, "MAP01");

if (map is DoomMap doomMap)
{
    // Render to PNG bytes
    byte[] png = MapThumbnailRenderer.RenderToPng(doomMap);
    File.WriteAllBytes("map01.png", png);
}
```

### Rendering to an Image Object

For further processing with [ImageSharp](https://github.com/SixLabors/ImageSharp):

```csharp
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

if (map is DoomMap doomMap)
{
    using Image<Rgba32> image = MapThumbnailRenderer.RenderToImage(doomMap);

    // Save in any format ImageSharp supports
    image.SaveAsPng("map01.png");
    image.SaveAsJpeg("map01.jpg");
    image.SaveAsBmp("map01.bmp");

    // Or process further with ImageSharp APIs
}
```

### Hexen Maps

Hexen maps are rendered the same way:

```csharp
if (map is HexenMap hexenMap)
{
    byte[] png = MapThumbnailRenderer.RenderToPng(hexenMap);
}
```

## Customizing Rendering

Pass a `MapThumbnailOptions` object to control appearance:

```csharp
var options = new MapThumbnailOptions
{
    Width = 1024,                                         // Image width (default: 512)
    Height = 1024,                                        // Image height (default: 512)
    Padding = 32,                                         // Padding around geometry (default: 16)
    LineColor = new Rgba32(255, 255, 255, 255),           // One-sided walls (default: white)
    TwoSidedLineColor = new Rgba32(128, 128, 128, 255),  // Two-sided walls (default: gray)
    SecretLineColor = new Rgba32(255, 0, 255, 255),      // Secret walls (default: magenta)
    BackgroundColor = new Rgba32(0, 0, 0, 255)            // Background (default: black)
};

byte[] png = MapThumbnailRenderer.RenderToPng(doomMap, options);
```

### Line Coloring

Lines are colored based on their properties, matching classic automap conventions:

| Line Type | Default Color | Flag |
|-----------|---------------|------|
| One-sided (solid wall) | White | - |
| Two-sided (passable) | Gray | `IsTwoSided` |
| Secret | Magenta | `Secret` flag (0x0020) |

This matches how the [DOOM automap](https://doomwiki.org/wiki/Automap) displays walls.

## Rendering All Maps in a WAD

```csharp
using var wadReader = new WadArchiveReader("DOOM2.WAD");
var wad = wadReader.ReadWad();
var mapReader = new MapReader();
var maps = mapReader.ReadMaps(wad);

foreach (var map in maps)
{
    byte[]? png = map switch
    {
        DoomMap doom => MapThumbnailRenderer.RenderToPng(doom),
        HexenMap hexen => MapThumbnailRenderer.RenderToPng(hexen),
        _ => null
    };

    if (png != null)
    {
        File.WriteAllBytes($"{map.Name}.png", png);
        Console.WriteLine($"Rendered {map.Name}");
    }
}
```

## Image Export

The `ImageExporter` class converts DOOM's internal graphic formats to standard image formats:

```csharp
using WAD.NET.Export;

// Export a DOOM picture lump to PNG
var exporter = new ImageExporter();
// (See API reference for full details)
```

DOOM uses several custom graphic formats:

| Format | Usage | Reference |
|--------|-------|-----------|
| Picture | Sprites, patches, most graphics | [Doom Wiki - Picture format](https://doomwiki.org/wiki/Picture_format) |
| Flat | Floor/ceiling textures (64x64 raw) | [Doom Wiki - Flat](https://doomwiki.org/wiki/Flat) |
| PLAYPAL | 256-color palette (14 palettes x 768 bytes) | [Doom Wiki - PLAYPAL](https://doomwiki.org/wiki/PLAYPAL) |
| COLORMAP | Light-level color mapping | [Doom Wiki - COLORMAP](https://doomwiki.org/wiki/COLORMAP) |

## Format References

- [Doom Wiki - Automap](https://doomwiki.org/wiki/Automap) - How the DOOM automap works
- [Doom Wiki - Picture format](https://doomwiki.org/wiki/Picture_format) - DOOM's columnar image format
- [Doom Wiki - Flat](https://doomwiki.org/wiki/Flat) - Raw 64x64 texture format
- [Doom Wiki - PLAYPAL](https://doomwiki.org/wiki/PLAYPAL) - The 256-color palette
- [ImageSharp Documentation](https://docs.sixlabors.com/articles/imagesharp/) - The image library used internally
