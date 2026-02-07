# Compatibility Detection

WAD.NET can analyze a mod's contents and determine the minimum source port required to run it. This is useful for mod managers, archive browsers, and automated testing pipelines.

## Basic Usage

```csharp
using WAD.NET.Archives;
using WAD.NET.Detection;

using var archive = ArchiveReaderFactory.Open("mymod.pk3");

var analyzer = new CompatibilityAnalyzer();
ModCompatibility compat = analyzer.Analyze(archive);

Console.WriteLine($"Minimum port: {compat.MinimumPort}");
Console.WriteLine(compat.Description);
Console.WriteLine($"Features: {string.Join(", ", compat.RequiredFeatures)}");
```

## Source Port Hierarchy

The analyzer classifies mods along a compatibility hierarchy from least to most restrictive:

| Level | Port | Description | Reference |
|-------|------|-------------|-----------|
| 0 | **Vanilla** | Original DOOM engine, no extensions | [Doom Wiki - Vanilla](https://doomwiki.org/wiki/Vanilla_Doom) |
| 1 | **Boom** | Extended limits, generalized linedefs, scrollers | [Doom Wiki - Boom](https://doomwiki.org/wiki/Boom) |
| 2 | **MBF** | Marine's Best Friend, helper dog, beta code pointers | [Doom Wiki - MBF](https://doomwiki.org/wiki/MBF) |
| 3 | **MBF21** | Modern Boom-compatible standard | [Doom Wiki - MBF21](https://doomwiki.org/wiki/MBF21) |
| 4 | **ZDoom** | ACS, DECORATE, MAPINFO, SNDINFO | [ZDoom Wiki](https://zdoom.org/wiki/ZDoom) |
| 5 | **GZDoom** | ZScript, OpenGL, 3D models, shaders | [GZDoom](https://zdoom.org/wiki/GZDoom) |
| 6 | **Eternity** | Eternity Engine-specific features | [Eternity Engine](https://eternity.youfailit.net/) |

## What Gets Detected

### Scripting Languages

| Lump | Detection | Minimum Port |
|------|-----------|--------------|
| `ZSCRIPT` | Presence + version scanning | GZDoom |
| `DECORATE` | Presence + actor counting | ZDoom |
| `BEHAVIOR` / `SCRIPTS` | Compiled ACS scripts | ZDoom |
| `LOADACS` | ACS library loading | ZDoom |

[ZScript](https://zdoom.org/wiki/ZScript) is the most capable scripting language, available only in [GZDoom](https://zdoom.org/wiki/GZDoom). [DECORATE](https://zdoom.org/wiki/DECORATE) is supported by all ZDoom-family ports. [ACS](https://zdoom.org/wiki/ACS) is compiled script code for triggers, events, and HUDs.

### Definition Lumps

| Lump | Detection | Minimum Port |
|------|-----------|--------------|
| `MAPINFO` / `ZMAPINFO` | Map metadata | ZDoom |
| `EMAPINFO` | Eternity map metadata | Eternity |
| `UMAPINFO` | Universal map metadata | MBF21 |
| `SNDINFO` / `SNDSEQ` | Sound definitions | ZDoom |
| `DEHACKED` | Content + BEX/MBF/MBF21 detection | Varies |

DEHACKED analysis examines the content to distinguish between:
- Standard DEH (any source port)
- [BEX extensions](https://doomwiki.org/wiki/BEX) (`[STRINGS]`, `[PARS]`, etc.) - requires [Boom](https://doomwiki.org/wiki/Boom)
- MBF extensions (`[CODEPTR]`, `HELPER`) - requires [MBF](https://doomwiki.org/wiki/MBF)
- [MBF21 features](https://doomwiki.org/wiki/MBF21) (new flags, code pointers) - requires MBF21-compliant port

### Resource Lumps

| Lump | Detection | Minimum Port |
|------|-----------|--------------|
| `GLDEFS` / `DOOMDEFS` | OpenGL lighting | GZDoom |
| `MODELDEF` | 3D model definitions | GZDoom |
| `VOXELDEF` | Voxel definitions | GZDoom |
| `ANIMDEFS` | Animated textures | Boom |
| `TEXTURES` | ZDoom texture definitions | ZDoom |
| `TERRAIN` | Terrain splash/footstep | ZDoom |
| `LANGUAGE` | Localization strings | ZDoom |
| `KEYCONF` | Key binding overrides | ZDoom |
| `LOCKDEFS` | Lock definitions | ZDoom |
| `MENUDEF` | Menu customization | ZDoom |
| `CVARINFO` | Custom CVars | ZDoom |

### Map Formats

| Format | Detection | Minimum Port |
|--------|-----------|--------------|
| DOOM binary | Default | Vanilla |
| Hexen binary | `BEHAVIOR` lump present | ZDoom / Hexen |
| [UDMF](https://doomwiki.org/wiki/UDMF) | `TEXTMAP` lump present | ZDoom |

## ModCompatibility Properties

```csharp
ModCompatibility compat = analyzer.Analyze(archive);

// Primary result
compat.MinimumPort        // SourcePort enum value
compat.Description        // Human-readable: "Requires GZDoom"
compat.RequiredFeatures   // List of feature strings: ["ZScript", "MAPINFO", ...]

// Feature flags
compat.UsesZScript        // bool
compat.UsesDecorate       // bool
compat.UsesDehacked       // bool
compat.UsesMapInfo        // bool
compat.UsesUMapInfo       // bool
compat.UsesSndInfo        // bool
compat.UsesACS            // bool

// Statistics
compat.ActorCount         // Number of actors defined in DECORATE/ZScript
compat.MapFormats         // Set of detected map formats

// Convenience properties
compat.IsBoomCompatible       // true if MinimumPort <= MBF21
compat.RequiresZDoomFamily    // true if MinimumPort >= ZDoom

// Warnings
compat.Warnings           // List of non-fatal analysis warnings
```

## Composite Archive Analysis

When using `CompositeArchive`, analysis runs against the combined effective lump set:

```csharp
using WAD.NET.Composite;

using var composite = CompositeArchive.Load("DOOM2.WAD", "mymod.wad", "zscript-addon.pk3");
var analysis = composite.Analyze();

Console.WriteLine($"Combined minimum port: {analysis.Compatibility.MinimumPort}");
Console.WriteLine($"Conflicts: {analysis.Conflicts.Count}");
```

## Format References

- [Doom Wiki - Boom](https://doomwiki.org/wiki/Boom) - Boom source port features
- [Doom Wiki - MBF](https://doomwiki.org/wiki/MBF) - Marine's Best Friend
- [Doom Wiki - MBF21](https://doomwiki.org/wiki/MBF21) - MBF21 specification
- [ZDoom Wiki - ZDoom](https://zdoom.org/wiki/ZDoom) - ZDoom features overview
- [ZDoom Wiki - GZDoom](https://zdoom.org/wiki/GZDoom) - GZDoom (OpenGL ZDoom)
- [ZDoom Wiki - DECORATE](https://zdoom.org/wiki/DECORATE) - Actor definition language
- [ZDoom Wiki - ZScript](https://zdoom.org/wiki/ZScript) - ZDoom's scripting language
- [ZDoom Wiki - ACS](https://zdoom.org/wiki/ACS) - Action Code Script
- [Doom Wiki - DeHackEd](https://doomwiki.org/wiki/DeHackEd) - DEHACKED patching
- [Doom Wiki - BEX](https://doomwiki.org/wiki/BEX) - Boom Extended DEHACKED
- [Eternity Engine](https://eternity.youfailit.net/) - Eternity Engine home
