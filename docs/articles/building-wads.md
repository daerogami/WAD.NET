# Building WADs & PK3s

WAD.NET provides fluent builder APIs for creating WAD and PK3 files programmatically. This is useful for mod build pipelines, WAD manipulation tools, and automated testing.

## WadBuilder

### Creating a WAD from Scratch

```csharp
using WAD.NET.Authoring;
using WAD.NET.Enums;

var builder = new WadBuilder(WadType.PWAD);

// Add lumps with raw byte data
builder.AddLump("PLAYPAL", playpalData);
builder.AddLump("COLORMAP", colormapData);

// Add marker-bounded sections
builder.AddMarker("FF_START");
builder.AddLump("FLOOR4_8", flatData);
builder.AddMarker("FF_END");

// Add definition lumps
builder.AddLump("DEHACKED", Encoding.ASCII.GetBytes(dehContent));

// Write to file
builder.WriteToFile("output.wad");

// Or write to a stream
using var stream = new MemoryStream();
builder.WriteTo(stream);
```

All methods return `this` for fluent chaining:

```csharp
new WadBuilder(WadType.PWAD)
    .AddMarker("FF_START")
    .AddLump("FLAT01", data1)
    .AddLump("FLAT02", data2)
    .AddMarker("FF_END")
    .WriteToFile("flats.wad");
```

### Building from an Existing Archive

Pre-populate a builder with lumps from any archive, then modify:

```csharp
using var reader = new WadArchiveReader("original.wad");

var builder = WadBuilder.FromArchive(reader)
    .ReplaceLump("DEHACKED", newDehData)
    .RemoveLump("DEMO1")
    .AddLump("NEWLUMP", newData);

builder.WriteToFile("modified.wad");
```

### Inserting at a Position

```csharp
builder.InsertLump(5, "INSERTED", data); // Insert at index 5
```

### Inspecting the Directory

```csharp
var directory = builder.GetDirectory();
foreach (var (name, size) in directory)
{
    Console.WriteLine($"{name}: {size} bytes");
}
```

### Lump Name Rules

WAD lump names must be:
- 1-8 characters
- ASCII only
- Names are automatically uppercased

Names exceeding 8 characters throw `ArgumentException`.

## Pk3Builder

### Creating a PK3

```csharp
using WAD.NET.Authoring;

var builder = new Pk3Builder();

// Add entries with folder paths (matching PK3 structure)
builder.AddEntry("maps/MAP01.wad", mapWadBytes);
builder.AddEntry("sprites/PLAYA1.png", spriteData);
builder.AddEntry("sounds/DSPISTOL.wav", soundData);
builder.AddEntry("mapinfo", mapInfoBytes);
builder.AddEntry("zscript.zs", zscriptBytes);

builder.WriteToFile("mymod.pk3");
```

Backslashes are automatically normalized to forward slashes. Leading slashes are trimmed.

### Building from an Existing Archive

```csharp
using var reader = new Pk3Reader("original.pk3");

var builder = Pk3Builder.FromArchive(reader)
    .ReplaceEntry("mapinfo", newMapInfoData)
    .RemoveEntry("sounds/unused.wav")
    .AddEntry("sprites/NEWSP.png", newSpriteData);

builder.WriteToFile("modified.pk3");
```

### PK3 Folder Structure

Follow the [ZDoom PK3 conventions](https://zdoom.org/wiki/Using_ZIPs_as_WAD_replacement) for folder names:

| Folder | Purpose |
|--------|---------|
| `maps/` | Map files (WAD or UDMF) |
| `sprites/` | Sprite graphics |
| `flats/` | Floor/ceiling textures |
| `textures/` | Wall textures |
| `sounds/` | Sound effects |
| `music/` | Music tracks |
| `graphics/` | Menu/UI graphics |
| `patches/` | Texture patches |
| `acs/` | Compiled [ACS scripts](https://zdoom.org/wiki/ACS) |
| `zscript/` | [ZScript](https://zdoom.org/wiki/ZScript) source files |
| `decorate/` | [DECORATE](https://zdoom.org/wiki/DECORATE) actor definitions |

### Inspecting Entries

```csharp
var entries = builder.GetEntries();
foreach (var (path, size) in entries)
{
    Console.WriteLine($"{path}: {size} bytes");
}
```

## Output Format

### WAD Binary Layout

The `WadBuilder` writes a valid [WAD file](https://doomwiki.org/wiki/WAD) with this layout:

| Section | Description |
|---------|-------------|
| Header (12 bytes) | Magic (`IWAD`/`PWAD`), lump count, directory offset |
| Lump data | Concatenated lump data |
| Directory | 16-byte entries: offset, size, 8-char name |

### PK3 Format

The `Pk3Builder` writes a standard ZIP archive using `System.IO.Compression.ZipArchive` with optimal compression.

## Format References

- [Doom Wiki - WAD](https://doomwiki.org/wiki/WAD) - WAD file structure
- [ZDoom Wiki - Using ZIPs as WAD replacement](https://zdoom.org/wiki/Using_ZIPs_as_WAD_replacement) - PK3 specification
- [Doom Wiki - PWAD](https://doomwiki.org/wiki/PWAD) - Patch WAD conventions
