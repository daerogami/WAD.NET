# Reading Archives

WAD.NET supports four archive formats through a unified `IArchiveReader` interface. This guide covers each reader's capabilities and usage patterns.

## Auto-Detection with ArchiveReaderFactory

The recommended way to open an archive is through `ArchiveReaderFactory.Open`, which inspects magic bytes (not the file extension) to determine format:

```csharp
using WAD.NET.Archives;

using var archive = ArchiveReaderFactory.Open("somefile.wad");
Console.WriteLine(archive.Type); // WAD, PK3, PK7, or Folder
```

Magic bytes detected:

| Bytes | Format |
|-------|--------|
| `IWAD` / `PWAD` | WAD |
| `PK\x03\x04` | ZIP (PK3) |
| `7z\xBC\xAF\x27\x1C` | 7-Zip (PK7) |
| *(directory)* | Folder |

You can also detect the format without opening:

```csharp
var type = ArchiveReaderFactory.DetectArchiveType("somefile.dat");
// Or from a stream:
var type = ArchiveReaderFactory.DetectArchiveType(stream);
```

## WAD Files

[WAD](https://doomwiki.org/wiki/WAD) (Where's All the Data) is the original DOOM archive format. A WAD file contains a flat list of named lumps with a directory at the end.

### WadArchiveReader

```csharp
using var wad = new WadArchiveReader("DOOM2.WAD");

// WAD-specific properties
Console.WriteLine(wad.WadType);   // IWAD or PWAD
Console.WriteLine(wad.Path);

// List all entries
foreach (var entry in wad.GetEntries())
{
    Console.WriteLine($"{entry.Name}: {entry.Size} bytes, category={entry.Category}");
}

// Look up by name (case-insensitive)
var playpal = wad.GetEntry("PLAYPAL");
byte[] data = wad.ReadLump(playpal);

// Check existence
bool hasMusic = wad.Contains("D_RUNNIN");

// Filter by category
var maps = wad.GetEntriesByCategory(LumpCategory.Map);
var flats = wad.GetEntriesByCategory(LumpCategory.Flat);
```

### Lump Categories

WAD entries are automatically categorized based on name patterns and marker sections:

| Category | Detection Method |
|----------|-----------------|
| `Map` | Map markers (E1M1, MAP01, etc.) and map sub-lumps (THINGS, LINEDEFS, etc.) |
| `Flat` | Between `F_START`/`F_END` or `FF_START`/`FF_END` markers |
| `Sprite` | Between `S_START`/`S_END` or `SS_START`/`SS_END` markers |
| `Music` | Names starting with `D_` |
| `Sound` | Names starting with `DS` or `DP`, plus `GENMIDI`, `DMXGUS` |
| `Texture` | `PNAMES`, `TEXTURE1`, `TEXTURE2` |
| `Graphic` | `PLAYPAL`, `COLORMAP` |

For a full explanation of WAD lump organization, see the [Doom Wiki - WAD](https://doomwiki.org/wiki/WAD).

### Stream Construction

You can construct a reader from a stream instead of a file path:

```csharp
var stream = File.OpenRead("DOOM2.WAD");
using var wad = new WadArchiveReader(stream, leaveOpen: false, wadName: "DOOM2.WAD");
```

### Legacy Wad API

For full lump deserialization (parsing THINGS, LINEDEFS, etc. into typed objects), use `ReadWad()`:

```csharp
using var wadReader = new WadArchiveReader("DOOM2.WAD");
Wad wad = wadReader.ReadWad(); // Returns a Wad with parsed ILump objects

foreach (var lump in wad.Lumps)
{
    Console.WriteLine($"{lump.Name}: {lump.GetType().Name}");
}
```

This is required for `MapReader` and `WadValidator`, which operate on parsed `ILump` objects.

## PK3 Archives

[PK3](https://zdoom.org/wiki/Using_ZIPs_as_WAD_replacement) files are standard ZIP archives with a specific folder structure, used by [ZDoom](https://zdoom.org/wiki/ZDoom)-family source ports.

```csharp
using var pk3 = new Pk3Reader("mymod.pk3");

foreach (var entry in pk3.GetEntries())
{
    Console.WriteLine($"{entry.FullPath}: {entry.Size} bytes");
}
```

PK3 entries use `FullPath` for the path within the archive (e.g., `maps/MAP01.wad`) and `Name` for the filename portion.

### Embedded WADs

PK3 archives commonly contain WAD files in their `maps/` directory for individual maps. The `Pk3Reader` detects these and makes their contents available.

### Expected PK3 Structure

```
mod.pk3
├── maps/           Map files (WAD or UDMF text)
├── sprites/        Sprite graphics
├── flats/          Floor/ceiling textures
├── textures/       Wall textures
├── sounds/         Sound effects
├── music/          Music tracks
├── graphics/       Menu/UI graphics
├── patches/        Texture patches
├── acs/            Compiled ACS scripts
├── zscript/        ZScript source files
├── decorate/       DECORATE actor definitions
└── mapinfo         Map metadata
```

See the [ZDoom Wiki - Using ZIPs as WAD replacement](https://zdoom.org/wiki/Using_ZIPs_as_WAD_replacement) for the complete specification.

## PK7 Archives

PK7 files are 7-Zip archives, supported by some source ports. Usage is identical to PK3:

```csharp
using var pk7 = new Pk7Reader("mymod.pk7");
```

## Folder Reader

The `FolderReader` reads a directory on disk as though it were a PK3 archive, using the same folder structure conventions:

```csharp
using var folder = new FolderReader("./mymod/");

foreach (var entry in folder.GetEntries())
{
    Console.WriteLine($"{entry.FullPath}: {entry.Size} bytes");
}
```

This is useful during mod development when you want to test content without zipping it into a PK3.

## LumpEntry Properties

All archive readers return `LumpEntry` objects with these properties:

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Lump name (8-char max for WAD, filename for PK3) |
| `FullPath` | `string` | Full path within archive (same as Name for WAD) |
| `Size` | `long` | Uncompressed size in bytes |
| `CompressedSize` | `long` | Compressed size (same as Size for WAD) |
| `Category` | `LumpCategory` | Auto-detected content category |
| `Offset` | `long` | Byte offset within archive (WAD only) |
| `IsMarker` | `bool` | True if size is zero (marker lumps) |

## Format References

- [Doom Wiki - WAD](https://doomwiki.org/wiki/WAD) - The WAD file format specification
- [Doom Wiki - Lump](https://doomwiki.org/wiki/Lump) - How lumps work
- [ZDoom Wiki - Using ZIPs as WAD replacement](https://zdoom.org/wiki/Using_ZIPs_as_WAD_replacement) - PK3 format
- [Doom Wiki - IWAD](https://doomwiki.org/wiki/IWAD) - Internal WAD (base game data)
- [Doom Wiki - PWAD](https://doomwiki.org/wiki/PWAD) - Patch WAD (mod data)
