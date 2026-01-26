# WAD.NET - DOOM WAD File Parser Library

## Project Overview

WAD.NET is a .NET Standard 2.1 library for parsing DOOM engine WAD files and related formats. The goal is to provide a comprehensive, cross-platform library for reading WAD, PK3, and related archives used by DOOM, Heretic, Hexen, Strife, and modern source ports.

## Architecture

### Solution Structure
```
WAD.NET.sln
├── WAD.NET/                 # Main library (netstandard2.1)
│   ├── Attributes/          # Engine-specific flag attributes
│   ├── Concrete/            # Core implementations (Wad, WadReader, etc.)
│   ├── Definitions/         # Data structures for WAD content
│   │   ├── Classic/Map/     # Classic DOOM binary map format
│   │   └── UDMF/Fields/     # UDMF 1.1 text format definitions
│   ├── Enums/               # WadType, ThingType, LinedefFlag, etc.
│   ├── Interfaces/          # IWadReader, ILump, IWadConfig, IWadValidator
│   └── Lump/                # Lump type implementations
│       ├── Abstract/        # Base Lump class
│       ├── Concrete/        # Specific lump types (FlatLump, MusicLump, etc.)
│       └── Interfaces/      # ILump, IMapLump
└── Wad.NET.Tests/           # XUnit tests (net6.0)
```

### Key Interfaces
- `IWadReader` - Base interface for archive readers (WAD, PK3, folder)
- `ILump` - Base interface for all lump types
- `IMapLump` - Extended interface for map-related lumps
- `IWadValidator` - Validation interface (not yet implemented)

### Coding Patterns
- Use `Queue<ILump>` for lump collections (maintains insertion order)
- Abstract base class `Lump` for common lump functionality
- Attribute decoration for engine-specific features (`HexenFlagAttribute`, `BoomFlagAttribute`, etc.)
- `BinaryReader` for WAD parsing with proper stream position management

## Build Commands

```bash
# Build the solution
dotnet build WAD.NET.sln

# Run tests
dotnet test Wad.NET.Tests/Wad.NET.Tests.csproj

# Build release
dotnet build WAD.NET.sln -c Release
```

## Development Guidelines

### Code Style
- Use C# 8.0+ features (nullable reference types, pattern matching, ranges)
- Prefer `ReadOnlySpan<byte>` and `Memory<T>` for zero-allocation parsing where possible
- Use XML documentation comments for public APIs
- Follow .NET naming conventions (PascalCase for public members, _camelCase for private fields)

### Error Handling
- Throw `FormatException` for invalid WAD/lump format errors
- Throw `NotSupportedException` for unimplemented features (e.g., compressed lumps)
- Validate file offsets and sizes before seeking/reading
- Provide detailed error messages with context (lump name, offset, expected vs actual)

### Testing
- Use XUnit for unit tests
- Include embedded test WAD data where possible (avoid external file dependencies)
- Test edge cases: empty lumps, marker lumps, maximum sizes, malformed data

## WAD Format Reference

### WAD Header (12 bytes)
| Offset | Size | Description |
|--------|------|-------------|
| 0 | 4 | Magic: "IWAD" or "PWAD" |
| 4 | 4 | Number of lumps (int32) |
| 8 | 4 | Directory offset (int32) |

### Directory Entry (16 bytes each)
| Offset | Size | Description |
|--------|------|-------------|
| 0 | 4 | Lump data offset (int32) |
| 4 | 4 | Lump size in bytes (int32) |
| 8 | 8 | Lump name (8 chars, null-padded) |

### Lump Name Conventions
- Map markers: `ExMy` (DOOM) or `MAPxx` (DOOM II)
- Map lumps: THINGS, LINEDEFS, SIDEDEFS, VERTEXES, SEGS, SSECTORS, NODES, SECTORS, REJECT, BLOCKMAP
- Flats: Between `F_START`/`F_END` or `FF_START`/`FF_END`
- Sprites: Between `S_START`/`S_END` or `SS_START`/`SS_END`
- Patches: Between `P_START`/`P_END` or `PP_START`/`PP_END`

## PK3/Archive Format Reference

PK3 files are standard ZIP archives with a specific folder structure:
```
mod.pk3 (ZIP format)
├── maps/           # Map files (WAD or UDMF)
├── sprites/        # Sprite graphics
├── flats/          # Floor/ceiling textures
├── textures/       # Wall textures
├── sounds/         # Sound effects
├── music/          # Music tracks
├── graphics/       # Menu/UI graphics
├── patches/        # Texture patches
├── acs/            # ACS compiled scripts
├── zscript/        # ZScript source files
├── decorate/       # DECORATE actor definitions
└── mapinfo         # Map metadata
```

## Known Issues & Technical Debt

### Critical Bugs
1. **PWAD detection bug** - `WadReader.cs:168` returns `WadType.IWAD` for PWAD files

### Missing Implementations
- LZSS compression (throws `NotImplementedException`)
- Actual lump content parsing (most lumps are stub containers)
- Map data deserialization
- UDMF text parsing
- `IWadValidator` implementation

### Code Quality Issues
- `CompressedWadReader` duplicates `WadReader` logic
- UDMF struct fields are private (inaccessible)
- `HandleMarkerLump()` is empty
- Test data paths are Windows-specific absolute paths

## Implementation Roadmap

See `/specs` directory for detailed implementation specifications:
- `specs/phase-1-core-parsing.md` - Bug fixes and lump deserialization
- `specs/phase-2-map-formats.md` - Classic binary and UDMF map parsing
- `specs/phase-3-pk3-support.md` - PK3/ZIP archive support
- `specs/phase-4-resources.md` - Texture, sprite, and sound parsing
- `specs/phase-5-modern-extensions.md` - Boom, MBF21, DECORATE, ZScript

## External References

- [DOOM Wiki - WAD](https://doomwiki.org/wiki/WAD)
- [DOOM Wiki - Lump](https://doomwiki.org/wiki/Lump)
- [ZDoom Wiki - Using ZIPs as WAD replacement](https://zdoom.org/wiki/Using_ZIPs_as_WAD_replacement)
- [UDMF Specification](https://github.com/coelckers/gzdoom/blob/master/specs/udmf.txt)
- [UDMF 1.1 Specification](https://github.com/coelckers/gzdoom/blob/master/specs/udmf11.txt)
- [ZDoom UDMF Extensions](https://github.com/coelckers/gzdoom/blob/master/specs/udmf_zdoom.txt)
