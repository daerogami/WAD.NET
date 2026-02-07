# Validation

WAD.NET provides structural validation for WAD files, checking map integrity, marker pairs, cross-references between map elements, and resource dependencies. This helps catch common mapping errors and verify WAD correctness.

## Basic Validation

```csharp
using WAD.NET.Archives;
using WAD.NET.Validation;

using var wadReader = new WadArchiveReader("mymod.wad");
var wad = wadReader.ReadWad();

var validator = new WadValidator();
var result = validator.Validate(wad);

if (result.IsValid)
{
    Console.WriteLine("No errors found.");
}
else
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"ERROR [{error.Code}] {error.MapName ?? ""}: {error.Message}");
    }
}

foreach (var warning in result.Warnings)
{
    Console.WriteLine($"WARN  [{warning.Code}] {warning.MapName ?? ""}: {warning.Message}");
}
```

### Quick Check

For a simple pass/fail:

```csharp
bool isValid = validator.IsValid(wad);
```

## What Gets Validated

### Map Structure

For each map marker (E1M1, MAP01, etc.), the validator checks:

- **Required lumps present** - THINGS, LINEDEFS, SIDEDEFS, VERTEXES, and SECTORS must all exist
- **UDMF structure** - TEXTMAP and ENDMAP must be present for UDMF maps
- **Non-empty data** - Map lumps should contain data

### Cross-Reference Integrity

For classic DOOM and [Hexen](https://doomwiki.org/wiki/Hexen_level_format) format maps:

- **Linedef vertex references** - Start and end vertex indices must be within the vertex array
- **Linedef sidedef references** - Front and back sidedef indices must be valid
- **Front sidedef required** - Every linedef must have a front sidedef
- **Two-sided consistency** - Linedefs with the two-sided flag must have a back sidedef
- **Sidedef sector references** - Each sidedef's sector index must be valid

For [UDMF](https://doomwiki.org/wiki/UDMF) maps, the same cross-reference checks are performed on the parsed UDMF data.

### Marker Pairs

The validator checks that marker sections are properly structured:

- **Matching pairs** - Every `F_START` must have a corresponding `F_END`, etc.
- **No nesting** - Marker sections should not be nested within the same type
- **Non-empty sections** - Warns if a marker section contains no lumps

Supported marker pairs:

| Start | End | Content |
|-------|-----|---------|
| `F_START` / `FF_START` | `F_END` / `FF_END` | [Flats](https://doomwiki.org/wiki/Flat) (floor/ceiling textures) |
| `S_START` / `SS_START` | `S_END` / `SS_END` | [Sprites](https://doomwiki.org/wiki/Sprite) |
| `P_START` / `PP_START` | `P_END` / `PP_END` | [Patches](https://doomwiki.org/wiki/Patch) (texture patches) |
| `TX_START` | `TX_END` | Textures |
| `HI_START` | `HI_END` | Hi-res replacements |
| `V_START` / `VV_START` | `V_END` / `VV_END` | Voices |
| `A_START` / `AA_START` | `A_END` / `AA_END` | ACS libraries |

### Resource Dependencies

- **PLAYPAL required** - Warns if the WAD has graphics lumps (flats, sprites, pictures) but no [PLAYPAL](https://doomwiki.org/wiki/PLAYPAL) lump
- **COLORMAP required** - Warns if graphics are present without a [COLORMAP](https://doomwiki.org/wiki/COLORMAP) lump

### Things Validation

- **Empty maps** - Warns if a map has no things at all
- **Player start** - Warns if no [player 1 start](https://doomwiki.org/wiki/Player_start) (thing type 1) is present

## Validation Codes

Each error and warning has a code for programmatic handling:

| Code | Severity | Description |
|------|----------|-------------|
| `MAP_MISSING_LUMPS` | Error | Map is missing required lumps |
| `MAP_INCOMPLETE` | Error | Map data is incomplete |
| `MAP_EMPTY_DATA` | Error | Map lump has zero size |
| `LINEDEF_INVALID_START_VERTEX` | Error | Linedef references out-of-range start vertex |
| `LINEDEF_INVALID_END_VERTEX` | Error | Linedef references out-of-range end vertex |
| `LINEDEF_INVALID_FRONT_SIDEDEF` | Error | Linedef references out-of-range front sidedef |
| `LINEDEF_INVALID_BACK_SIDEDEF` | Error | Linedef references out-of-range back sidedef |
| `LINEDEF_MISSING_FRONT_SIDEDEF` | Error | Linedef has no front sidedef |
| `LINEDEF_TWOSIDED_NO_BACK` | Warning | Two-sided linedef has no back sidedef |
| `SIDEDEF_INVALID_SECTOR` | Error | Sidedef references out-of-range sector |
| `MARKER_UNPAIRED` | Error | Start/end marker without matching pair |
| `MARKER_NESTED` | Warning | Nested markers of the same type |
| `MARKER_EMPTY` | Warning | Marker section contains no lumps |
| `RESOURCE_MISSING_PLAYPAL` | Warning | Graphics present without PLAYPAL |
| `RESOURCE_MISSING_COLORMAP` | Warning | Graphics present without COLORMAP |
| `MAP_NO_THINGS` | Warning | Map has no things |
| `MAP_NO_PLAYER_START` | Warning | Map has no player 1 start |

## Validating a WAD Configuration

Validate an IWAD and its PWADs together:

```csharp
var result = validator.ValidateConfig(wadConfig);
```

This validates each WAD individually and merges the results.

## Format References

- [Doom Wiki - Linedef](https://doomwiki.org/wiki/Linedef) - Linedef structure and flags
- [Doom Wiki - Sidedef](https://doomwiki.org/wiki/Sidedef) - Sidedef structure
- [Doom Wiki - Sector](https://doomwiki.org/wiki/Sector) - Sector structure
- [Doom Wiki - Thing](https://doomwiki.org/wiki/Thing) - Thing types and flags
- [Doom Wiki - PLAYPAL](https://doomwiki.org/wiki/PLAYPAL) - The 256-color palette
- [Doom Wiki - COLORMAP](https://doomwiki.org/wiki/COLORMAP) - Light level color mapping
