# Phase 1: Core WAD Parsing

## Overview

This phase addresses critical bugs and implements actual lump content deserialization. The goal is to transform stub lump classes into functional parsers that extract meaningful data from WAD files.

## Priority: HIGH

This phase is foundational - all other phases depend on having correct, working lump parsing.

---

## Task 1.1: Fix PWAD Detection Bug

### Location
`WAD.NET/Concrete/WadReader.cs:168`

### Problem
```csharp
case "PWAD":
    return WadType.IWAD;  // BUG: Should return WadType.PWAD
```

### Solution
```csharp
case "PWAD":
    return WadType.PWAD;
```

### Test Case
```csharp
[Fact]
public void ShouldCorrectlyIdentifyPWAD()
{
    using var reader = new WadReader("path/to/test.pwad");
    var wad = reader.ReadWad();
    Assert.Equal(WadType.PWAD, wad.WadType);
}
```

---

## Task 1.2: Implement PLAYPAL Lump Parser

### Specification
- Lump name: `PLAYPAL`
- Size: 10,752 bytes (768 bytes × 14 palettes)
- Each palette: 256 RGB triplets (3 bytes each)

### Data Structure
```csharp
public class Palette
{
    public Color[] Colors { get; }  // 256 colors

    public Palette(ReadOnlySpan<byte> data)
    {
        Colors = new Color[256];
        for (int i = 0; i < 256; i++)
        {
            Colors[i] = Color.FromArgb(
                data[i * 3],      // R
                data[i * 3 + 1],  // G
                data[i * 3 + 2]   // B
            );
        }
    }
}

public sealed class PaletteLump : Lump
{
    public Palette[] Palettes { get; }  // 14 palettes

    public PaletteLump(string name, string source, ReadOnlySpan<byte> data)
        : base(name, source)
    {
        Palettes = new Palette[14];
        for (int i = 0; i < 14; i++)
        {
            Palettes[i] = new Palette(data.Slice(i * 768, 768));
        }
    }

    // Palette indices:
    // 0: Normal
    // 1-8: Pain (increasing red tint for damage)
    // 9: Bonus pickup (gold tint)
    // 10-12: Radiation suit (green tint)
    // 13: Berserk (red tint)
}
```

---

## Task 1.3: Implement COLORMAP Lump Parser

### Specification
- Lump name: `COLORMAP`
- Size: 8,704 bytes (256 bytes × 34 maps)
- Each map: 256 indices into the palette
- Maps 0-31: Light levels (0 = brightest, 31 = darkest)
- Map 32: Invulnerability effect
- Map 33: All black (unused)

### Data Structure
```csharp
public sealed class ColorMapLump : Lump
{
    public byte[][] Maps { get; }  // 34 maps, 256 entries each

    public ColorMapLump(string name, string source, ReadOnlySpan<byte> data)
        : base(name, source)
    {
        Maps = new byte[34][];
        for (int i = 0; i < 34; i++)
        {
            Maps[i] = data.Slice(i * 256, 256).ToArray();
        }
    }

    public byte RemapColor(int colorIndex, int lightLevel)
    {
        return Maps[lightLevel][colorIndex];
    }
}
```

---

## Task 1.4: Implement PNAMES Lump Parser

### Specification
- Lump name: `PNAMES`
- Format:
  - 4 bytes: Number of patches (int32)
  - N × 8 bytes: Patch names (8-char, null-padded)

### Data Structure
```csharp
public sealed class PatchNamesLump : Lump
{
    public string[] PatchNames { get; }

    public PatchNamesLump(string name, string source, ReadOnlySpan<byte> data)
        : base(name, source)
    {
        int count = BinaryPrimitives.ReadInt32LittleEndian(data);
        PatchNames = new string[count];

        for (int i = 0; i < count; i++)
        {
            var nameBytes = data.Slice(4 + i * 8, 8);
            PatchNames[i] = ReadNullTerminatedString(nameBytes);
        }
    }

    private static string ReadNullTerminatedString(ReadOnlySpan<byte> data)
    {
        int length = data.IndexOf((byte)0);
        if (length < 0) length = data.Length;
        return Encoding.ASCII.GetString(data.Slice(0, length));
    }
}
```

---

## Task 1.5: Implement TEXTURE1/TEXTURE2 Lump Parser

### Specification
- Lump names: `TEXTURE1`, `TEXTURE2`
- Format:
  - 4 bytes: Number of textures (int32)
  - N × 4 bytes: Offsets to texture definitions (int32[])
  - Texture definitions at each offset

### Texture Definition Format (22 bytes + patches)
| Offset | Size | Description |
|--------|------|-------------|
| 0 | 8 | Texture name |
| 8 | 4 | Masked flag (unused) |
| 12 | 2 | Width |
| 14 | 2 | Height |
| 16 | 4 | Column directory (unused) |
| 20 | 2 | Patch count |
| 22 | N×10 | Patch descriptors |

### Patch Descriptor Format (10 bytes)
| Offset | Size | Description |
|--------|------|-------------|
| 0 | 2 | X offset |
| 2 | 2 | Y offset |
| 4 | 2 | Patch index (into PNAMES) |
| 6 | 2 | Step dir (unused) |
| 8 | 2 | Colormap (unused) |

### Data Structure
```csharp
public readonly struct TexturePatch
{
    public short OriginX { get; init; }
    public short OriginY { get; init; }
    public ushort PatchIndex { get; init; }
}

public class TextureDefinition
{
    public string Name { get; init; }
    public ushort Width { get; init; }
    public ushort Height { get; init; }
    public TexturePatch[] Patches { get; init; }
}

public sealed class TextureLump : Lump
{
    public TextureDefinition[] Textures { get; }

    public TextureLump(string name, string source, ReadOnlySpan<byte> data)
        : base(name, source)
    {
        int count = BinaryPrimitives.ReadInt32LittleEndian(data);
        Textures = new TextureDefinition[count];

        // Read offset table
        var offsets = new int[count];
        for (int i = 0; i < count; i++)
        {
            offsets[i] = BinaryPrimitives.ReadInt32LittleEndian(
                data.Slice(4 + i * 4, 4));
        }

        // Parse each texture definition
        for (int i = 0; i < count; i++)
        {
            Textures[i] = ParseTextureDefinition(data.Slice(offsets[i]));
        }
    }

    private static TextureDefinition ParseTextureDefinition(ReadOnlySpan<byte> data)
    {
        var name = ReadNullTerminatedString(data.Slice(0, 8));
        var width = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(12, 2));
        var height = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(14, 2));
        var patchCount = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(20, 2));

        var patches = new TexturePatch[patchCount];
        for (int i = 0; i < patchCount; i++)
        {
            var patchData = data.Slice(22 + i * 10, 10);
            patches[i] = new TexturePatch
            {
                OriginX = BinaryPrimitives.ReadInt16LittleEndian(patchData),
                OriginY = BinaryPrimitives.ReadInt16LittleEndian(patchData.Slice(2)),
                PatchIndex = BinaryPrimitives.ReadUInt16LittleEndian(patchData.Slice(4))
            };
        }

        return new TextureDefinition
        {
            Name = name,
            Width = width,
            Height = height,
            Patches = patches
        };
    }
}
```

---

## Task 1.6: Implement Sound Effect Lump Parser

### Specification
- Lump names: `DS*` prefix (e.g., `DSPISTOL`, `DSSHOTGN`)
- DMX sound format:
  - 2 bytes: Format (always 3)
  - 2 bytes: Sample rate (usually 11025)
  - 4 bytes: Number of samples
  - N bytes: 8-bit unsigned PCM samples

### Data Structure
```csharp
public sealed class SoundLump : Lump
{
    public ushort SampleRate { get; }
    public byte[] Samples { get; }

    public SoundLump(string name, string source, ReadOnlySpan<byte> data)
        : base(name, source)
    {
        var format = BinaryPrimitives.ReadUInt16LittleEndian(data);
        if (format != 3)
            throw new FormatException($"Unknown sound format: {format}");

        SampleRate = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(2));
        var sampleCount = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(4));

        // Samples start at offset 8, but first/last 16 are padding
        Samples = data.Slice(8 + 16, sampleCount - 32).ToArray();
    }
}
```

---

## Task 1.7: Implement Flat Lump Parser

### Specification
- Lumps between `F_START`/`F_END` markers
- Size: 4,096 bytes (64 × 64 pixels)
- Format: Raw 8-bit palette indices, row by row

### Data Structure
```csharp
public sealed class FlatLump : Lump
{
    public const int Width = 64;
    public const int Height = 64;

    public byte[] Pixels { get; }  // 64x64 = 4096 bytes

    public FlatLump(string name, string source, ReadOnlySpan<byte> data)
        : base(name, source)
    {
        if (data.Length != Width * Height)
            throw new FormatException(
                $"Flat must be {Width * Height} bytes, got {data.Length}");

        Pixels = data.ToArray();
    }

    public byte GetPixel(int x, int y) => Pixels[y * Width + x];
}
```

---

## Task 1.8: Make UDMF Struct Fields Accessible

### Problem
All UDMF struct fields are private and inaccessible.

### Location
`WAD.NET/Definitions/UDMF/Fields/`

### Solution
Convert private fields to public properties with init setters:

```csharp
// Before
public struct Vertex
{
    float x;
    float y;
}

// After
public readonly struct Vertex
{
    public float X { get; init; }
    public float Y { get; init; }
}
```

Apply to all UDMF structs: `Vertex`, `Sector`, `Thing`, `LineDefinition`, `SideDefinition`.

---

## Task 1.9: Refactor WadReader for Lump Data Passing

### Problem
`WadReader.HandleDataLump()` reads lump data but doesn't pass it to all lump constructors.

### Solution
Update all lump classes to accept and parse their data:

```csharp
private void HandleDataLump(Wad wad, int lumpPtr, string lumpName, int lumpSize)
{
    var lastPosition = _reader.BaseStream.Position;
    _reader.BaseStream.Seek(lumpPtr, SeekOrigin.Begin);
    var data = _reader.ReadBytes(lumpSize);

    ILump lump = lumpName switch
    {
        "PLAYPAL" => new PaletteLump(lumpName, _sourceWadName, data),
        "COLORMAP" => new ColorMapLump(lumpName, _sourceWadName, data),
        "PNAMES" => new PatchNamesLump(lumpName, _sourceWadName, data),
        "TEXTURE1" or "TEXTURE2" => new TextureLump(lumpName, _sourceWadName, data),
        _ when lumpName.StartsWith("DS") => new SoundLump(lumpName, _sourceWadName, data),
        _ when lumpName.StartsWith("D_") => new MusicLump(lumpName, _sourceWadName, data),
        _ when _readingFlats => new FlatLump(lumpName, _sourceWadName, data),
        _ => new BinaryLump(lumpName, _sourceWadName, data)
    };

    wad.Lumps.Enqueue(lump);
    _reader.BaseStream.Seek(lastPosition, SeekOrigin.Begin);
}
```

---

## Task 1.10: Add Input Validation

### Requirements
1. Validate WAD header before parsing
2. Check directory entry offsets are within file bounds
3. Validate lump sizes don't exceed file size
4. Handle truncated files gracefully

### Implementation
```csharp
private void ValidateHeader(long fileSize, int lumpCount, int directoryOffset)
{
    if (directoryOffset < 12)
        throw new FormatException("Directory offset cannot be before header");

    if (directoryOffset > fileSize)
        throw new FormatException(
            $"Directory offset {directoryOffset} exceeds file size {fileSize}");

    long requiredSize = directoryOffset + (lumpCount * 16L);
    if (requiredSize > fileSize)
        throw new FormatException(
            $"Directory extends beyond file (need {requiredSize}, have {fileSize})");
}

private void ValidateLumpEntry(int offset, int size, long fileSize, string name)
{
    if (size > 0)
    {
        if (offset < 0 || offset > fileSize)
            throw new FormatException(
                $"Lump '{name}' has invalid offset {offset}");

        if (offset + size > fileSize)
            throw new FormatException(
                $"Lump '{name}' extends beyond file");
    }
}
```

---

## Acceptance Criteria

1. All unit tests pass
2. PWAD files correctly identified as `WadType.PWAD`
3. `PLAYPAL` lump returns 14 usable palettes with 256 colors each
4. `COLORMAP` lump returns 34 maps with 256 entries each
5. `PNAMES` lump returns list of patch names
6. `TEXTURE1`/`TEXTURE2` lumps return texture definitions with patch references
7. Flat lumps return 64×64 pixel data
8. Sound lumps return sample rate and PCM data
9. UDMF struct fields are publicly accessible
10. Invalid WAD files throw descriptive `FormatException`

---

## Test Data Requirements

Create embedded test resources in `Wad.NET.Tests/TestData/`:
- `minimal.wad` - Valid IWAD with minimal lumps
- `minimal.pwad` - Valid PWAD for type detection test
- `truncated.wad` - Truncated file for error handling test
- `badoffset.wad` - Invalid directory offset for validation test
