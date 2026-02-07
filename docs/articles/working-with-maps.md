# Working with Maps

WAD.NET parses maps from all three DOOM map formats: classic DOOM binary, Hexen extended binary, and UDMF text format. This guide covers reading, inspecting, and working with map data.

## Map Formats Overview

| Format | Games | Key Feature | Reference |
|--------|-------|-------------|-----------|
| DOOM | DOOM, DOOM II, Heretic | Original binary format | [Doom Wiki](https://doomwiki.org/wiki/Doom_level_format) |
| Hexen | Hexen, Strife | ACS scripts, thing IDs, 6 linedef args | [Doom Wiki](https://doomwiki.org/wiki/Hexen_level_format) |
| UDMF | Modern source ports | Text-based, extensible key-value pairs | [UDMF Spec](https://doomwiki.org/wiki/UDMF) |

WAD.NET auto-detects the format by inspecting the map lumps that follow a map marker.

## Reading Maps

### Setup

Map reading requires a parsed `Wad` object (not just an `IArchiveReader`):

```csharp
using WAD.NET.Archives;
using WAD.NET.Maps;

using var wadReader = new WadArchiveReader("DOOM2.WAD");
var wad = wadReader.ReadWad();
var mapReader = new MapReader();
```

### List Available Maps

```csharp
var mapNames = mapReader.GetMapNames(wad);
// ["MAP01", "MAP02", ... "MAP32"]
```

### Read All Maps

```csharp
var maps = mapReader.ReadMaps(wad);
foreach (var map in maps)
{
    Console.WriteLine($"{map.Name}: {map.Format}, {map.ThingCount} things, {map.LinedefCount} linedefs");
}
```

### Read a Specific Map

```csharp
var map = mapReader.ReadMap(wad, "MAP01");
```

## Map Data by Format

All map types implement `IMap`, which provides common counts. Cast to the specific type for full data access.

### DOOM Format

The [original binary format](https://doomwiki.org/wiki/Doom_level_format) stores map data in separate lumps following a map marker:

```
MAP01       (marker, 0 bytes)
THINGS      (10 bytes per thing)
LINEDEFS    (14 bytes per linedef)
SIDEDEFS    (30 bytes per sidedef)
VERTEXES    (4 bytes per vertex)
SEGS
SSECTORS
NODES
SECTORS     (26 bytes per sector)
REJECT
BLOCKMAP
```

Access DOOM map data:

```csharp
if (map is DoomMap doomMap)
{
    // Things - monsters, items, player starts
    foreach (var thing in doomMap.Things)
    {
        Console.WriteLine($"Thing type {thing.Type} at ({thing.X}, {thing.Y}), angle {thing.Angle}");
    }

    // Vertices - map geometry points
    foreach (var vertex in doomMap.Vertices)
    {
        Console.WriteLine($"Vertex at ({vertex.X}, {vertex.Y})");
    }

    // Linedefs - walls connecting vertices
    foreach (var linedef in doomMap.Linedefs)
    {
        Console.WriteLine($"Line: v{linedef.StartVertex} -> v{linedef.EndVertex}, " +
                          $"type={linedef.Special}, flags={linedef.Flags}");
    }

    // Sidedefs - texture assignments for linedefs
    foreach (var sidedef in doomMap.Sidedefs)
    {
        Console.WriteLine($"Sidedef: sector={sidedef.Sector}, " +
                          $"upper={sidedef.UpperTexture}, lower={sidedef.LowerTexture}, mid={sidedef.MiddleTexture}");
    }

    // Sectors - floor/ceiling areas
    foreach (var sector in doomMap.Sectors)
    {
        Console.WriteLine($"Sector: floor={sector.FloorHeight}, ceil={sector.CeilingHeight}, " +
                          $"light={sector.LightLevel}, type={sector.Special}");
    }

    // BSP data
    Console.WriteLine($"Segs: {doomMap.Segs.Length}");
    Console.WriteLine($"Subsectors: {doomMap.Subsectors.Length}");
    Console.WriteLine($"Nodes: {doomMap.Nodes.Length}");
}
```

For a complete description of what each field means, see the [Doom Wiki - Level data](https://doomwiki.org/wiki/Doom_level_format).

### Hexen Format

The [Hexen format](https://doomwiki.org/wiki/Hexen_level_format) extends DOOM's binary format with:

- Things have TIDs (thing IDs), action specials, and arguments
- Linedefs have action specials with up to 5 byte arguments (instead of a sector tag)
- A BEHAVIOR lump containing compiled [ACS](https://doomwiki.org/wiki/ACS) scripts

```csharp
if (map is HexenMap hexenMap)
{
    // Hexen things include TID and action specials
    foreach (var thing in hexenMap.Things)
    {
        Console.WriteLine($"Thing type {thing.Type}, TID={thing.Tid}, " +
                          $"special={thing.Special}, args=[{string.Join(",", thing.Args)}]");
    }

    // Hexen linedefs have 5 argument bytes
    foreach (var linedef in hexenMap.Linedefs)
    {
        Console.WriteLine($"Line: special={linedef.Special}, " +
                          $"args=[{string.Join(",", linedef.Args)}]");
    }

    // ACS bytecode
    Console.WriteLine($"BEHAVIOR data: {hexenMap.BehaviorData.Length} bytes");
}
```

### UDMF Format

[UDMF](https://doomwiki.org/wiki/UDMF) (Universal Doom Map Format) stores map data as human-readable text with extensible key-value properties. It replaces all the binary lumps with a single `TEXTMAP` lump:

```
MAP01       (marker)
TEXTMAP     (text-based map data)
...         (optional script lumps)
ENDMAP      (end marker)
```

```csharp
if (map is UdmfMapWrapper udmfMap && udmfMap.Map != null)
{
    var udmf = udmfMap.Map;
    Console.WriteLine($"Namespace: {udmf.Namespace}");
    Console.WriteLine($"Things: {udmf.ThingCount}");
    Console.WriteLine($"Vertices: {udmf.VertexCount}");
    Console.WriteLine($"Linedefs: {udmf.LinedefCount}");
    Console.WriteLine($"Sidedefs: {udmf.SidedefCount}");
    Console.WriteLine($"Sectors: {udmf.SectorCount}");
}
```

The UDMF namespace indicates which extensions are in use:

| Namespace | Source Port | Reference |
|-----------|-------------|-----------|
| `doom` | Standard DOOM fields only | [UDMF Spec](https://github.com/coelckers/gzdoom/blob/master/specs/udmf.txt) |
| `heretic` | Standard Heretic fields | [UDMF Spec](https://github.com/coelckers/gzdoom/blob/master/specs/udmf.txt) |
| `hexen` | Standard Hexen fields | [UDMF Spec](https://github.com/coelckers/gzdoom/blob/master/specs/udmf.txt) |
| `strife` | Standard Strife fields | [UDMF Spec](https://github.com/coelckers/gzdoom/blob/master/specs/udmf.txt) |
| `zdoom` | ZDoom extensions | [ZDoom UDMF](https://github.com/coelckers/gzdoom/blob/master/specs/udmf_zdoom.txt) |
| `eternity` | Eternity Engine extensions | [Eternity Wiki](https://eternity.youfailit.net/) |

## Format Detection

`MapDetector` identifies map markers and detects formats:

```csharp
// Check if a name is a map marker (E1M1, MAP01, etc.)
bool isMap = MapDetector.IsMapMarker("MAP01"); // true
bool isMap2 = MapDetector.IsMapMarker("E1M1"); // true
bool notMap = MapDetector.IsMapMarker("THINGS"); // false

// Check if a name is a map sub-lump
bool isSub = MapDetector.IsMapLump("THINGS"); // true

// Detect format from available lump names
var format = MapDetector.DetectFormat(new[] { "THINGS", "LINEDEFS", "TEXTMAP" });
// Returns MapFormat.UDMF if TEXTMAP is present
```

Detection rules:
- If `TEXTMAP` is present: **UDMF**
- If `BEHAVIOR` is present: **Hexen**
- Otherwise: **DOOM**

## Map Naming Conventions

| Pattern | Game | Examples |
|---------|------|----------|
| `ExMy` | DOOM, Heretic | E1M1, E4M9 |
| `MAPxx` | DOOM II, Hexen, Strife | MAP01, MAP32 |

## Format References

- [Doom Wiki - Level data](https://doomwiki.org/wiki/Doom_level_format) - DOOM binary map format
- [Doom Wiki - Hexen level format](https://doomwiki.org/wiki/Hexen_level_format) - Hexen extensions
- [Doom Wiki - UDMF](https://doomwiki.org/wiki/UDMF) - Universal Doom Map Format overview
- [UDMF 1.1 Specification](https://github.com/coelckers/gzdoom/blob/master/specs/udmf11.txt) - Full UDMF spec
- [ZDoom UDMF Extensions](https://github.com/coelckers/gzdoom/blob/master/specs/udmf_zdoom.txt) - ZDoom-specific UDMF fields
- [Doom Wiki - Linedef](https://doomwiki.org/wiki/Linedef) - What linedefs are
- [Doom Wiki - Thing](https://doomwiki.org/wiki/Thing) - What things are
- [Doom Wiki - Sector](https://doomwiki.org/wiki/Sector) - What sectors are
- [Doom Wiki - ACS](https://doomwiki.org/wiki/ACS) - Action Code Script (Hexen/ZDoom)
