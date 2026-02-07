# Architecture

This page describes the high-level design of WAD.NET and how its components fit together.

## Design Goals

- **Format-agnostic access** - A unified `IArchiveReader` interface for WAD, PK3, PK7, and folder archives
- **.NET Standard 2.1** - Maximum compatibility across .NET runtimes including Unity
- **Composability** - Components work independently and together (e.g., archive readers feed map parsers feed validators)
- **Zero external format dependencies** - WAD/DOOM format handling is self-contained; third-party libraries are used only for ZIP/7z decompression (SharpCompress) and image processing (ImageSharp)

## Namespace Map

```
WAD.NET
├── Archives/          IArchiveReader implementations (WAD, PK3, PK7, Folder)
├── Authoring/         WadBuilder, Pk3Builder (fluent file creation)
├── Composite/         CompositeArchive (multi-archive layering)
├── Concrete/          Core types: Wad, WadReader, Engine, Author
├── Definitions/       Binary data structures
│   ├── Classic/Map/   DOOM binary map structures (DoomThing, DoomLinedef, etc.)
│   ├── GameData/      Thing/Linedef/Sector databases
│   ├── Hexen/         Hexen extended format structures
│   └── UDMF/Fields/   UDMF field definitions
├── Detection/         CompatibilityAnalyzer, ModCompatibility, script scanners
├── Enums/             WadType, MapFormat, LumpCategory, ArchiveType, etc.
├── Export/            ImageExporter (DOOM graphics to PNG)
├── Lump/              Lump type implementations
│   ├── Abstract/      Base Lump class
│   ├── Concrete/      FlatLump, PictureLump, MusicLump, ThingsLump, etc.
│   └── Interfaces/    ILump, IMapLump
├── Maps/              MapReader, MapDetector, DoomMap, HexenMap, UdmfMapWrapper
├── Parsers/           Text format parsers
│   ├── Dehacked/      DehackedParser, DehackedPatch
│   ├── MapInfo/       MapInfoParser, MapInfo
│   └── SndInfo/       SndInfoParser, SndInfo
├── Rendering/         MapThumbnailRenderer
├── Resources/         ResourceManager
├── SourcePorts/       Port-specific features (Boom, MBF21, Zandronum)
├── Sprites/           Sprite parsing
├── Validation/        WadValidator, ValidationResult
└── ZScript/           Full ZScript/DECORATE parser
    ├── Diagnostics/   Linting analyzers
    ├── Emit/          C# code generation
    ├── Formatting/    Code formatting
    ├── Semantics/     Symbol tables, type resolution
    └── Syntax/        Lexer, Parser, AST nodes
```

## Core Interfaces

### IArchiveReader

The central abstraction for reading any archive format:

```csharp
public interface IArchiveReader : IDisposable
{
    string Path { get; }
    ArchiveType Type { get; }

    IEnumerable<LumpEntry> GetEntries();
    LumpEntry? GetEntry(string name);
    byte[] ReadLump(LumpEntry entry);
    Stream OpenLump(LumpEntry entry);
    bool Contains(string name);
}
```

Four implementations exist:

| Class | Format | Notes |
|-------|--------|-------|
| `WadArchiveReader` | WAD (IWAD/PWAD) | Also exposes `WadType`, `GetMapNames()`, `ReadWad()` |
| `Pk3Reader` | PK3 (ZIP) | Handles embedded WADs in `maps/` folder |
| `Pk7Reader` | PK7 (7-Zip) | Via SharpCompress |
| `FolderReader` | Loose files | Mirrors PK3 folder structure |

### ILump

The interface for parsed lump data within a WAD. Used by the legacy `Wad`/`WadReader` API and by `MapReader`:

```csharp
public interface ILump
{
    string Name { get; }
    int Size { get; }
    string SourceWad { get; }
}
```

Concrete implementations include `ThingsLump`, `LinedefsLump`, `SidedefsLump`, `SectorsLump`, `VertexesLump`, `FlatLump`, `PictureLump`, `MusicLump`, `TextMapLump`, and more.

### IMap

The interface for parsed map data:

```csharp
public interface IMap
{
    string Name { get; }
    MapFormat Format { get; }
    int ThingCount { get; }
    int VertexCount { get; }
    int LinedefCount { get; }
    int SidedefCount { get; }
    int SectorCount { get; }
}
```

Three implementations cover the three map formats:

| Class | Format | Reference |
|-------|--------|-----------|
| `DoomMap` | Classic DOOM binary | [Doom Wiki - WAD](https://doomwiki.org/wiki/WAD) |
| `HexenMap` | Hexen extended binary | [Doom Wiki - Hexen](https://doomwiki.org/wiki/Hexen) |
| `UdmfMapWrapper` | UDMF text format | [UDMF Spec](https://doomwiki.org/wiki/UDMF) |

## Data Flow

A typical workflow proceeds through these stages:

```
File on disk
    │
    ▼
ArchiveReaderFactory.Open()  ──▶  IArchiveReader
    │                                    │
    │  (for WAD files)                   │  GetEntries() / ReadLump()
    ▼                                    ▼
WadArchiveReader.ReadWad()  ──▶  Wad (parsed lumps)
    │                                    │
    ▼                                    ▼
MapReader.ReadMaps()        ──▶  IMap (DoomMap / HexenMap / UdmfMapWrapper)
    │                                    │
    ├── WadValidator.Validate()          │
    ├── MapThumbnailRenderer.Render()    │
    └── CompatibilityAnalyzer.Analyze()  │
```

For multi-archive scenarios:

```
DOOM2.WAD + mod.wad + textures.pk3
    │
    ▼
CompositeArchive.Load(...)
    │
    ▼
EffectiveLumps (resolved set with override tracking)
    │
    ├── ReadLump() / OpenLump()
    ├── GetOverrideChain()
    └── Analyze() (conflicts + compatibility)
```

## Dependencies

| Package | Purpose |
|---------|---------|
| [SharpCompress](https://github.com/adamhathcock/sharpcompress) | ZIP/7-Zip decompression for PK3/PK7 |
| [SixLabors.ImageSharp](https://github.com/SixLabors/ImageSharp) | Image rendering for map thumbnails and graphic export |
| [System.Collections.Immutable](https://www.nuget.org/packages/System.Collections.Immutable) | Immutable collections for the ZScript AST |

## External Format References

- [WAD format](https://doomwiki.org/wiki/WAD) - The binary WAD file structure
- [Lump](https://doomwiki.org/wiki/Lump) - What lumps are and how they work
- [DOOM map format](https://doomwiki.org/wiki/Doom_level_format) - Classic binary map structures
- [Hexen map format](https://doomwiki.org/wiki/Hexen_level_format) - Extended binary format with ACS
- [UDMF specification](https://doomwiki.org/wiki/UDMF) - Universal Doom Map Format
- [PK3/ZIP archives](https://zdoom.org/wiki/Using_ZIPs_as_WAD_replacement) - ZIP-based archive layout
