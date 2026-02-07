# Definition Parsers

WAD.NET includes parsers for three major text-based definition formats used in DOOM modding: DEHACKED, MAPINFO, and SNDINFO.

## DEHACKED

[DEHACKED](https://doomwiki.org/wiki/DeHackEd) (also known as DEH) was the original tool for modifying DOOM's executable behavior. WAD.NET parses DEHACKED patches embedded in WAD files (as the `DEHACKED` lump), supporting the original DEH format, [BEX extensions](https://doomwiki.org/wiki/BEX), and [MBF21 extensions](https://doomwiki.org/wiki/MBF21).

### Parsing

```csharp
using WAD.NET.Parsers.Dehacked;

// Read the DEHACKED lump from an archive
using var archive = ArchiveReaderFactory.Open("mymod.wad");
var entry = archive.GetEntry("DEHACKED");
var content = Encoding.ASCII.GetString(archive.ReadLump(entry));

var parser = new DehackedParser(content);
DehackedPatch patch = parser.Parse();
```

### Accessing Patch Data

```csharp
// Header info
Console.WriteLine($"Doom version: {patch.DoomVersion}");
Console.WriteLine($"Patch format: {patch.PatchFormat}");

// Modified things (actors)
foreach (var thing in patch.Things)
{
    Console.WriteLine($"Thing {thing.Key}: {thing.Value.Count} modified properties");
}

// Modified frames (animation states)
foreach (var frame in patch.Frames)
{
    Console.WriteLine($"Frame {frame.Key}: {frame.Value.Count} modified properties");
}

// Text replacements
foreach (var text in patch.Texts)
{
    Console.WriteLine($"Text: '{text.Original}' -> '{text.Replacement}'");
}

// BEX strings
foreach (var str in patch.Strings)
{
    Console.WriteLine($"[STRINGS] {str.Key} = {str.Value}");
}

// BEX code pointers
foreach (var ptr in patch.CodePointers)
{
    Console.WriteLine($"[CODEPTR] Frame {ptr.Key} = {ptr.Value}");
}
```

### DEHACKED History

| Format | Introduced By | Features |
|--------|---------------|----------|
| DEH | [DeHackEd](https://doomwiki.org/wiki/DeHackEd) | Thing/frame/weapon/text modifications |
| BEX | [Boom](https://doomwiki.org/wiki/Boom) | String replacements, code pointers, par times |
| MBF | [MBF](https://doomwiki.org/wiki/MBF) | Helper dog, CODEPTR, additional code pointers |
| MBF21 | [MBF21](https://doomwiki.org/wiki/MBF21) | New thing flags, weapon flags, code pointers |

## MAPINFO

[MAPINFO](https://zdoom.org/wiki/MAPINFO) defines map metadata for [ZDoom](https://zdoom.org/wiki/ZDoom)-family source ports: map names, music, sky textures, next/secret map transitions, and episode definitions.

### Parsing

```csharp
using WAD.NET.Parsers.MapInfo;

var content = Encoding.UTF8.GetString(archive.ReadLump(archive.GetEntry("MAPINFO")));

var parser = new MapInfoParser(content);
MapInfo info = parser.Parse();
```

### Accessing Map Definitions

```csharp
// Iterate map definitions
foreach (var kvp in info.Maps)
{
    var mapDef = kvp.Value;
    Console.WriteLine($"{mapDef.MapLump}: \"{mapDef.NiceName}\"");
    Console.WriteLine($"  Music: {mapDef.Music}");
    Console.WriteLine($"  Sky: {mapDef.Sky1}");
    Console.WriteLine($"  Next: {mapDef.Next}");
    Console.WriteLine($"  Secret next: {mapDef.SecretNext}");
    Console.WriteLine($"  Par time: {mapDef.Par}");
    Console.WriteLine($"  Cluster: {mapDef.Cluster}");
}

// Episode definitions
foreach (var episode in info.Episodes)
{
    Console.WriteLine($"Episode {episode.Number}: \"{episode.Name}\" (starts at {episode.StartMap})");
}

// Cluster (hub) definitions
foreach (var cluster in info.Clusters)
{
    Console.WriteLine($"Cluster {cluster.Key}: {cluster.Value.EnterText}");
}
```

### MAPINFO Variants

| Lump | Source Port | Reference |
|------|-------------|-----------|
| `MAPINFO` | ZDoom, GZDoom | [ZDoom Wiki - MAPINFO](https://zdoom.org/wiki/MAPINFO) |
| `ZMAPINFO` | ZDoom (extended) | [ZDoom Wiki - ZMAPINFO](https://zdoom.org/wiki/ZMAPINFO) |
| `UMAPINFO` | MBF21 ports | [Doom Wiki - UMAPINFO](https://doomwiki.org/wiki/UMAPINFO) |
| `EMAPINFO` | Eternity Engine | [Eternity Wiki](https://eternity.youfailit.net/) |

> **Note:** WAD.NET's `MapInfoParser` handles the ZDoom `MAPINFO` format. For `UMAPINFO` (a simpler format for Boom-compatible ports), compatibility is detected by `CompatibilityAnalyzer`.

## SNDINFO

[SNDINFO](https://zdoom.org/wiki/SNDINFO) maps logical sound names to lump names, with support for random sounds, ambient sounds, music aliases, and other audio directives.

### Parsing

```csharp
using WAD.NET.Parsers.SndInfo;

var content = Encoding.UTF8.GetString(archive.ReadLump(archive.GetEntry("SNDINFO")));

var parser = new SndInfoParser();
SndInfo info = parser.Parse(content);
```

### Accessing Sound Definitions

```csharp
// Sound name -> lump name mappings
foreach (var kvp in info.Sounds)
{
    Console.WriteLine($"{kvp.Key} => {kvp.Value}");
}
// e.g., "weapons/pistol" => "DSPISTOL"

// Random sound groups ($random)
foreach (var kvp in info.RandomSounds)
{
    Console.WriteLine($"$random {kvp.Key}: [{string.Join(", ", kvp.Value)}]");
}

// Ambient sounds ($ambient)
foreach (var ambient in info.AmbientSounds)
{
    Console.WriteLine($"$ambient {ambient.Id} {ambient.LogicalName}");
}

// Music aliases ($musicalias)
foreach (var alias in info.MusicAliases)
{
    Console.WriteLine($"$musicalias {alias.Key} {alias.Value}");
}
```

### SNDINFO Directives

| Directive | Description | Reference |
|-----------|-------------|-----------|
| `$random` | Define a random sound group | [ZDoom Wiki](https://zdoom.org/wiki/SNDINFO/Random_sounds) |
| `$ambient` | Define an ambient sound | [ZDoom Wiki](https://zdoom.org/wiki/SNDINFO/Ambient_sounds) |
| `$musicalias` | Alias a music name | [ZDoom Wiki](https://zdoom.org/wiki/SNDINFO/Music_aliases) |
| `$pitchshiftrange` | Set pitch variation range | [ZDoom Wiki](https://zdoom.org/wiki/SNDINFO) |
| `$playercompat` | Player sound compatibility | [ZDoom Wiki](https://zdoom.org/wiki/SNDINFO) |

## Format References

- [Doom Wiki - DeHackEd](https://doomwiki.org/wiki/DeHackEd) - DEHACKED format overview
- [Doom Wiki - BEX](https://doomwiki.org/wiki/BEX) - Boom Extended DEHACKED
- [Doom Wiki - MBF21](https://doomwiki.org/wiki/MBF21) - MBF21 DEHACKED extensions
- [ZDoom Wiki - MAPINFO](https://zdoom.org/wiki/MAPINFO) - Full MAPINFO reference
- [ZDoom Wiki - SNDINFO](https://zdoom.org/wiki/SNDINFO) - Full SNDINFO reference
- [ZDoom Wiki - ZMAPINFO](https://zdoom.org/wiki/ZMAPINFO) - Extended MAPINFO format
- [Doom Wiki - UMAPINFO](https://doomwiki.org/wiki/UMAPINFO) - Universal MAPINFO for Boom-family ports
