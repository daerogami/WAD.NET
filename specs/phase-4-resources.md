# Phase 4: Resource Parsing

## Overview

This phase implements parsing for graphical and audio resources in WAD files:
- Sprites and graphics (column-based format)
- Wall textures (composite patches)
- Flats (raw pixel data)
- Sound effects (DMX format)
- Music (MUS and MIDI formats)

## Priority: MEDIUM

Resource parsing enables visualization, extraction, and conversion tools.

---

## DOOM Graphics Format

DOOM uses a column-based picture format optimized for vertical drawing during rendering. This format is used for sprites, patches, and menu graphics.

### Picture Header
| Offset | Size | Type | Description |
|--------|------|------|-------------|
| 0 | 2 | uint16 | Width |
| 2 | 2 | uint16 | Height |
| 4 | 2 | int16 | Left offset (for sprites) |
| 6 | 2 | int16 | Top offset (for sprites) |
| 8 | 4×W | uint32[] | Column offsets (one per column) |

### Column Format
Each column consists of one or more "posts":
```
Post:
  1 byte: Top delta (row to start, 255 = end of column)
  1 byte: Length (number of pixels)
  1 byte: Padding (unused)
  N bytes: Pixel data (palette indices)
  1 byte: Padding (unused)
```

Columns can have multiple posts to handle transparent regions.

---

## Task 4.1: Picture/Sprite Parser

### Data Structure
```csharp
public class DoomPicture
{
    public ushort Width { get; init; }
    public ushort Height { get; init; }
    public short LeftOffset { get; init; }
    public short TopOffset { get; init; }

    /// <summary>
    /// Pixel data as palette indices.
    /// Value 255 indicates transparency.
    /// Access: pixels[y * Width + x]
    /// </summary>
    public byte[] Pixels { get; init; }

    public byte GetPixel(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return 255;  // Transparent
        return Pixels[y * Width + x];
    }
}

public sealed class PictureLump : Lump
{
    public DoomPicture Picture { get; }

    public PictureLump(string name, string source, ReadOnlySpan<byte> data)
        : base(name, source)
    {
        Picture = ParsePicture(data);
    }

    private static DoomPicture ParsePicture(ReadOnlySpan<byte> data)
    {
        var width = BinaryPrimitives.ReadUInt16LittleEndian(data);
        var height = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(2));
        var leftOffset = BinaryPrimitives.ReadInt16LittleEndian(data.Slice(4));
        var topOffset = BinaryPrimitives.ReadInt16LittleEndian(data.Slice(6));

        // Initialize with transparency
        var pixels = new byte[width * height];
        Array.Fill(pixels, (byte)255);

        // Read column offsets
        var columnOffsets = new uint[width];
        for (int i = 0; i < width; i++)
        {
            columnOffsets[i] = BinaryPrimitives.ReadUInt32LittleEndian(
                data.Slice(8 + i * 4, 4));
        }

        // Parse each column
        for (int x = 0; x < width; x++)
        {
            ParseColumn(data, columnOffsets[x], x, width, height, pixels);
        }

        return new DoomPicture
        {
            Width = width,
            Height = height,
            LeftOffset = leftOffset,
            TopOffset = topOffset,
            Pixels = pixels
        };
    }

    private static void ParseColumn(
        ReadOnlySpan<byte> data,
        uint offset,
        int x,
        int width,
        int height,
        byte[] pixels)
    {
        int pos = (int)offset;

        while (true)
        {
            byte topDelta = data[pos++];
            if (topDelta == 255)
                break;  // End of column

            byte length = data[pos++];
            pos++;  // Skip padding

            for (int i = 0; i < length; i++)
            {
                int y = topDelta + i;
                if (y >= 0 && y < height)
                {
                    pixels[y * width + x] = data[pos];
                }
                pos++;
            }

            pos++;  // Skip padding
        }
    }
}
```

---

## Task 4.2: Sprite Frame Parser

### Sprite Naming Convention
Format: `XXXXYZ` where:
- `XXXX` = 4-letter sprite name (e.g., `POSS` for zombie)
- `Y` = Frame letter (A-Z)
- `Z` = Rotation (0-8, 0 = no rotation)

For mirrored sprites: `XXXXYZYZ` (e.g., `POSSA2A8`)

### Data Structure
```csharp
public class SpriteFrame
{
    public string SpriteName { get; init; }  // e.g., "POSS"
    public char Frame { get; init; }          // e.g., 'A'
    public int Rotation { get; init; }        // 0-8
    public bool IsMirrored { get; init; }
    public DoomPicture Picture { get; init; }
}

public class SpriteDefinition
{
    public string Name { get; init; }
    public Dictionary<char, SpriteFrame[]> Frames { get; } = new();

    public SpriteFrame GetFrame(char frame, int rotation)
    {
        if (!Frames.TryGetValue(frame, out var rotations))
            return null;

        // Rotation 0 = all angles use same frame
        if (rotations[0] != null && rotations[0].Rotation == 0)
            return rotations[0];

        return rotations[rotation];
    }
}

public static class SpriteParser
{
    public static SpriteFrame ParseSpriteLump(string name, ReadOnlySpan<byte> data)
    {
        if (name.Length < 6)
            throw new FormatException($"Invalid sprite name: {name}");

        var spriteName = name[..4];
        var frame = name[4];
        var rotation = name[5] - '0';

        var picture = PictureLump.ParsePicture(data);

        return new SpriteFrame
        {
            SpriteName = spriteName,
            Frame = frame,
            Rotation = rotation,
            IsMirrored = false,
            Picture = picture
        };
    }
}
```

---

## Task 4.3: Composite Texture Builder

Wall textures are composed of multiple patches arranged on a canvas.

### Implementation
```csharp
public class TextureBuilder
{
    private readonly TextureDefinition _definition;
    private readonly Dictionary<string, DoomPicture> _patches;
    private readonly Palette _palette;

    public TextureBuilder(
        TextureDefinition definition,
        Dictionary<string, DoomPicture> patches,
        Palette palette)
    {
        _definition = definition;
        _patches = patches;
        _palette = palette;
    }

    /// <summary>
    /// Build texture as palette-indexed pixels
    /// </summary>
    public byte[] BuildIndexed()
    {
        var pixels = new byte[_definition.Width * _definition.Height];
        Array.Fill(pixels, (byte)255);  // Transparent

        foreach (var patchRef in _definition.Patches)
        {
            if (!_patches.TryGetValue(patchRef.PatchName, out var patch))
                continue;

            ApplyPatch(pixels, patch, patchRef.OriginX, patchRef.OriginY);
        }

        return pixels;
    }

    /// <summary>
    /// Build texture as RGBA pixels
    /// </summary>
    public byte[] BuildRgba()
    {
        var indexed = BuildIndexed();
        var rgba = new byte[_definition.Width * _definition.Height * 4];

        for (int i = 0; i < indexed.Length; i++)
        {
            var colorIndex = indexed[i];
            if (colorIndex == 255)
            {
                // Transparent
                rgba[i * 4 + 0] = 0;
                rgba[i * 4 + 1] = 0;
                rgba[i * 4 + 2] = 0;
                rgba[i * 4 + 3] = 0;
            }
            else
            {
                var color = _palette.Colors[colorIndex];
                rgba[i * 4 + 0] = color.R;
                rgba[i * 4 + 1] = color.G;
                rgba[i * 4 + 2] = color.B;
                rgba[i * 4 + 3] = 255;
            }
        }

        return rgba;
    }

    private void ApplyPatch(byte[] pixels, DoomPicture patch, int originX, int originY)
    {
        for (int py = 0; py < patch.Height; py++)
        {
            int destY = originY + py;
            if (destY < 0 || destY >= _definition.Height)
                continue;

            for (int px = 0; px < patch.Width; px++)
            {
                int destX = originX + px;
                if (destX < 0 || destX >= _definition.Width)
                    continue;

                byte pixel = patch.GetPixel(px, py);
                if (pixel != 255)  // Not transparent
                {
                    pixels[destY * _definition.Width + destX] = pixel;
                }
            }
        }
    }
}
```

---

## Task 4.4: Flat Parser

Flats are simple 64x64 raw pixel data.

### Implementation
```csharp
public sealed class FlatLump : Lump
{
    public const int Width = 64;
    public const int Height = 64;
    public const int Size = Width * Height;  // 4096 bytes

    public byte[] Pixels { get; }

    public FlatLump(string name, string source, ReadOnlySpan<byte> data)
        : base(name, source)
    {
        if (data.Length != Size)
            throw new FormatException(
                $"Flat '{name}' must be {Size} bytes, got {data.Length}");

        Pixels = data.ToArray();
    }

    public byte GetPixel(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            throw new ArgumentOutOfRangeException();
        return Pixels[y * Width + x];
    }

    /// <summary>
    /// Convert to RGBA using a palette
    /// </summary>
    public byte[] ToRgba(Palette palette)
    {
        var rgba = new byte[Size * 4];

        for (int i = 0; i < Size; i++)
        {
            var color = palette.Colors[Pixels[i]];
            rgba[i * 4 + 0] = color.R;
            rgba[i * 4 + 1] = color.G;
            rgba[i * 4 + 2] = color.B;
            rgba[i * 4 + 3] = 255;
        }

        return rgba;
    }
}
```

---

## Task 4.5: DMX Sound Parser

### DMX Sound Format
| Offset | Size | Type | Description |
|--------|------|------|-------------|
| 0 | 2 | uint16 | Format (always 3) |
| 2 | 2 | uint16 | Sample rate (typically 11025) |
| 4 | 4 | uint32 | Sample count (including padding) |
| 8 | 16 | - | Padding (zeros) |
| 24 | N | uint8[] | 8-bit unsigned PCM samples |
| end-16 | 16 | - | Padding (zeros) |

### Implementation
```csharp
public sealed class SoundLump : Lump
{
    public ushort Format { get; }
    public ushort SampleRate { get; }
    public byte[] Samples { get; }

    public SoundLump(string name, string source, ReadOnlySpan<byte> data)
        : base(name, source)
    {
        if (data.Length < 8)
            throw new FormatException($"Sound '{name}' too short");

        Format = BinaryPrimitives.ReadUInt16LittleEndian(data);
        SampleRate = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(2));
        var totalSamples = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(4));

        if (Format != 3)
            throw new FormatException($"Unknown sound format: {Format}");

        // Skip header padding, remove end padding
        int startOffset = 8 + 16;
        int actualSamples = (int)totalSamples - 32;

        if (actualSamples > 0 && startOffset + actualSamples <= data.Length)
        {
            Samples = data.Slice(startOffset, actualSamples).ToArray();
        }
        else
        {
            Samples = Array.Empty<byte>();
        }
    }

    /// <summary>
    /// Duration in seconds
    /// </summary>
    public double Duration => Samples.Length / (double)SampleRate;

    /// <summary>
    /// Convert to signed 16-bit PCM for modern audio APIs
    /// </summary>
    public short[] ToSigned16Bit()
    {
        var output = new short[Samples.Length];

        for (int i = 0; i < Samples.Length; i++)
        {
            // Convert 8-bit unsigned (0-255) to 16-bit signed (-32768 to 32767)
            output[i] = (short)((Samples[i] - 128) * 256);
        }

        return output;
    }

    /// <summary>
    /// Export as WAV file
    /// </summary>
    public void ExportWav(Stream output)
    {
        using var writer = new BinaryWriter(output, Encoding.ASCII, leaveOpen: true);

        // RIFF header
        writer.Write("RIFF"u8);
        writer.Write(36 + Samples.Length);  // File size - 8
        writer.Write("WAVE"u8);

        // Format chunk
        writer.Write("fmt "u8);
        writer.Write(16);           // Chunk size
        writer.Write((ushort)1);    // PCM format
        writer.Write((ushort)1);    // Mono
        writer.Write(SampleRate);   // Sample rate
        writer.Write(SampleRate);   // Byte rate (mono 8-bit)
        writer.Write((ushort)1);    // Block align
        writer.Write((ushort)8);    // Bits per sample

        // Data chunk
        writer.Write("data"u8);
        writer.Write(Samples.Length);
        writer.Write(Samples);
    }
}
```

---

## Task 4.6: MUS Music Parser

### MUS Format
DOOM's internal music format, based on MIDI.

| Offset | Size | Type | Description |
|--------|------|------|-------------|
| 0 | 4 | char[4] | Magic "MUS\x1a" |
| 4 | 2 | uint16 | Score length |
| 6 | 2 | uint16 | Score start offset |
| 8 | 2 | uint16 | Primary channels |
| 10 | 2 | uint16 | Secondary channels |
| 12 | 2 | uint16 | Instrument count |
| 14 | 2 | uint16 | Reserved |
| 16 | N×2 | uint16[] | Instrument patches |
| 16+N×2 | - | - | Music data |

### MUS Events
```
Event byte: [L][TTT][CCCC]
  L = Last event flag (delay follows if set)
  TTT = Event type (0-6)
  CCCC = Channel (0-15)
```

Event types:
- 0: Release note
- 1: Play note
- 2: Pitch bend
- 3: System event
- 4: Controller change
- 5: End of measure
- 6: End of track

### Implementation
```csharp
public sealed class MusicLump : Lump
{
    public bool IsMus { get; }
    public bool IsMidi { get; }
    public byte[] RawData { get; }

    // For MUS format
    public ushort[] Instruments { get; }

    public MusicLump(string name, string source, ReadOnlySpan<byte> data)
        : base(name, source)
    {
        RawData = data.ToArray();

        // Check for MUS magic
        if (data.Length >= 4 &&
            data[0] == 'M' && data[1] == 'U' &&
            data[2] == 'S' && data[3] == 0x1A)
        {
            IsMus = true;
            ParseMusHeader(data);
        }
        // Check for MIDI magic
        else if (data.Length >= 4 &&
                 data[0] == 'M' && data[1] == 'T' &&
                 data[2] == 'h' && data[3] == 'd')
        {
            IsMidi = true;
        }
    }

    private void ParseMusHeader(ReadOnlySpan<byte> data)
    {
        var instrumentCount = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(12));
        Instruments = new ushort[instrumentCount];

        for (int i = 0; i < instrumentCount; i++)
        {
            Instruments[i] = BinaryPrimitives.ReadUInt16LittleEndian(
                data.Slice(16 + i * 2, 2));
        }
    }

    /// <summary>
    /// Convert MUS to MIDI format
    /// </summary>
    public byte[] ToMidi()
    {
        if (!IsMus)
            return RawData;

        return MusToMidiConverter.Convert(RawData);
    }
}

public static class MusToMidiConverter
{
    // MUS to MIDI channel mapping
    private static readonly int[] MusToMidiChannel =
        { 0, 1, 2, 3, 4, 5, 6, 7, 8, 10, 11, 12, 13, 14, 15, 9 };

    public static byte[] Convert(byte[] musData)
    {
        using var output = new MemoryStream();
        using var writer = new BinaryWriter(output);

        // Parse MUS header
        var scoreLength = BinaryPrimitives.ReadUInt16LittleEndian(
            musData.AsSpan(4, 2));
        var scoreStart = BinaryPrimitives.ReadUInt16LittleEndian(
            musData.AsSpan(6, 2));

        // Write MIDI header
        writer.Write("MThd"u8);
        WriteBigEndian(writer, 6);     // Header length
        WriteBigEndian16(writer, 0);   // Format 0
        WriteBigEndian16(writer, 1);   // 1 track
        WriteBigEndian16(writer, 70);  // Ticks per beat

        // Write track header
        writer.Write("MTrk"u8);
        var trackLengthPos = output.Position;
        writer.Write(0);  // Placeholder for track length

        // Convert MUS events to MIDI
        int pos = scoreStart;
        var channelVolumes = new byte[16];
        Array.Fill(channelVolumes, (byte)127);

        while (pos < musData.Length)
        {
            byte eventByte = musData[pos++];
            bool hasDelay = (eventByte & 0x80) != 0;
            int eventType = (eventByte >> 4) & 0x07;
            int channel = eventByte & 0x0F;
            int midiChannel = MusToMidiChannel[channel];

            // Process event...
            // (Full implementation would convert each MUS event type)

            if (eventType == 6)  // End of track
                break;

            // Read delay if present
            if (hasDelay)
            {
                int delay = 0;
                byte delayByte;
                do
                {
                    delayByte = musData[pos++];
                    delay = (delay << 7) | (delayByte & 0x7F);
                } while ((delayByte & 0x80) != 0);

                WriteVariableLength(writer, delay);
            }
        }

        // Write end of track
        writer.Write((byte)0x00);
        writer.Write((byte)0xFF);
        writer.Write((byte)0x2F);
        writer.Write((byte)0x00);

        // Update track length
        var trackLength = (int)(output.Position - trackLengthPos - 4);
        output.Position = trackLengthPos;
        WriteBigEndian(writer, trackLength);

        return output.ToArray();
    }

    private static void WriteBigEndian(BinaryWriter writer, int value)
    {
        writer.Write((byte)(value >> 24));
        writer.Write((byte)(value >> 16));
        writer.Write((byte)(value >> 8));
        writer.Write((byte)value);
    }

    private static void WriteBigEndian16(BinaryWriter writer, ushort value)
    {
        writer.Write((byte)(value >> 8));
        writer.Write((byte)value);
    }

    private static void WriteVariableLength(BinaryWriter writer, int value)
    {
        // MIDI variable-length encoding
        var buffer = new Stack<byte>();
        buffer.Push((byte)(value & 0x7F));
        value >>= 7;

        while (value > 0)
        {
            buffer.Push((byte)((value & 0x7F) | 0x80));
            value >>= 7;
        }

        while (buffer.Count > 0)
            writer.Write(buffer.Pop());
    }
}
```

---

## Task 4.7: Image Export Utilities

### PNG Export
```csharp
public static class ImageExporter
{
    /// <summary>
    /// Export picture to PNG format
    /// </summary>
    public static void ExportPng(
        DoomPicture picture,
        Palette palette,
        Stream output)
    {
        // Using System.Drawing or ImageSharp
        // Example with raw PNG writing:

        var rgba = new byte[picture.Width * picture.Height * 4];
        for (int i = 0; i < picture.Pixels.Length; i++)
        {
            var colorIndex = picture.Pixels[i];
            if (colorIndex == 255)
            {
                // Transparent
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

        WritePng(output, picture.Width, picture.Height, rgba);
    }

    /// <summary>
    /// Export flat to PNG format
    /// </summary>
    public static void ExportFlatPng(
        FlatLump flat,
        Palette palette,
        Stream output)
    {
        var rgba = flat.ToRgba(palette);
        WritePng(output, FlatLump.Width, FlatLump.Height, rgba);
    }

    private static void WritePng(Stream output, int width, int height, byte[] rgba)
    {
        // Implementation using a PNG library like ImageSharp
        // or manual PNG encoding
    }
}
```

---

## Task 4.8: Resource Manager

### Unified Resource Access
```csharp
public class ResourceManager : IDisposable
{
    private readonly IArchiveReader _reader;
    private Palette _palette;
    private ColorMap _colorMap;
    private Dictionary<string, DoomPicture> _patches;
    private Dictionary<string, TextureDefinition> _textures;

    public ResourceManager(IArchiveReader reader)
    {
        _reader = reader;
        LoadSystemLumps();
    }

    private void LoadSystemLumps()
    {
        // Load PLAYPAL
        var playpal = _reader.GetEntry("PLAYPAL");
        if (playpal != null)
        {
            var data = _reader.ReadLump(playpal);
            _palette = new PaletteLump("PLAYPAL", "", data).Palettes[0];
        }

        // Load COLORMAP
        var colormap = _reader.GetEntry("COLORMAP");
        if (colormap != null)
        {
            var data = _reader.ReadLump(colormap);
            _colorMap = new ColorMapLump("COLORMAP", "", data);
        }

        // Load PNAMES and TEXTURE1/2
        LoadTextureDefinitions();
    }

    public Palette GetPalette(int index = 0)
    {
        var playpal = _reader.GetEntry("PLAYPAL");
        var data = _reader.ReadLump(playpal);
        var lump = new PaletteLump("PLAYPAL", "", data);
        return lump.Palettes[index];
    }

    public DoomPicture GetSprite(string name)
    {
        var entry = _reader.GetEntry(name);
        if (entry == null)
            return null;

        var data = _reader.ReadLump(entry);
        return new PictureLump(name, "", data).Picture;
    }

    public DoomPicture GetPatch(string name)
    {
        if (_patches.TryGetValue(name, out var cached))
            return cached;

        var entry = _reader.GetEntry(name);
        if (entry == null)
            return null;

        var data = _reader.ReadLump(entry);
        var picture = new PictureLump(name, "", data).Picture;
        _patches[name] = picture;
        return picture;
    }

    public byte[] GetTexture(string name)
    {
        if (!_textures.TryGetValue(name, out var definition))
            return null;

        var builder = new TextureBuilder(definition, _patches, _palette);
        return builder.BuildIndexed();
    }

    public FlatLump GetFlat(string name)
    {
        var entry = _reader.GetEntry(name);
        if (entry == null || entry.Size != 4096)
            return null;

        var data = _reader.ReadLump(entry);
        return new FlatLump(name, "", data);
    }

    public void Dispose() => _reader?.Dispose();
}
```

---

## Acceptance Criteria

1. Sprites/pictures parse correctly with transparency
2. Composite textures build from multiple patches
3. Flats load as 64x64 pixel data
4. Sound effects export to valid WAV files
5. MUS music converts to valid MIDI
6. PNG export produces valid images
7. Resource manager provides unified access

---

## Test Cases

```csharp
[Fact]
public void ShouldParseSpriteWithTransparency()
{
    byte[] data = { /* PLAYA1 sprite data */ };
    var lump = new PictureLump("PLAYA1", "DOOM.WAD", data);

    Assert.True(lump.Picture.Width > 0);
    Assert.Contains((byte)255, lump.Picture.Pixels);  // Has transparency
}

[Fact]
public void ShouldBuildCompositeTexture()
{
    var definition = new TextureDefinition
    {
        Name = "AASHITTY",
        Width = 64,
        Height = 64,
        Patches = new[] { new TexturePatch { PatchIndex = 0, OriginX = 0, OriginY = 0 } }
    };

    var patches = new Dictionary<string, DoomPicture>
    {
        ["WALL00_1"] = CreateTestPatch(64, 64)
    };

    var builder = new TextureBuilder(definition, patches, TestPalette);
    var result = builder.BuildIndexed();

    Assert.Equal(64 * 64, result.Length);
}

[Fact]
public void ShouldExportSoundAsWav()
{
    byte[] data = { /* DSPISTOL data */ };
    var lump = new SoundLump("DSPISTOL", "DOOM.WAD", data);

    using var output = new MemoryStream();
    lump.ExportWav(output);

    output.Position = 0;
    var magic = new byte[4];
    output.Read(magic, 0, 4);
    Assert.Equal("RIFF"u8.ToArray(), magic);
}

[Fact]
public void ShouldConvertMusToMidi()
{
    byte[] musData = { 'M', 'U', 'S', 0x1A, /* ... */ };
    var lump = new MusicLump("D_E1M1", "DOOM.WAD", musData);

    var midi = lump.ToMidi();

    Assert.Equal((byte)'M', midi[0]);
    Assert.Equal((byte)'T', midi[1]);
    Assert.Equal((byte)'h', midi[2]);
    Assert.Equal((byte)'d', midi[3]);
}
```
