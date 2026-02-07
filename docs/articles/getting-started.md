# Getting Started

This guide walks you through installing WAD.NET and performing common tasks with DOOM archive files.

## Installation

Install the [NuGet package](https://www.nuget.org/packages/WAD.NET):

```bash
dotnet add package WAD.NET
```

Or via the Package Manager Console:

```powershell
Install-Package WAD.NET
```

## Opening an Archive

The simplest way to open any supported archive is with `ArchiveReaderFactory.Open`, which auto-detects the format from magic bytes (not file extension):

```csharp
using WAD.NET.Archives;

// Opens WAD, PK3, PK7, or folder - detected automatically
using var archive = ArchiveReaderFactory.Open("DOOM2.WAD");

Console.WriteLine($"Archive type: {archive.Type}");
Console.WriteLine($"Entry count: {archive.GetEntries().Count()}");
```

You can also open a specific format directly:

```csharp
// WAD file
using var wad = new WadArchiveReader("DOOM2.WAD");
Console.WriteLine($"WAD type: {wad.WadType}"); // IWAD or PWAD

// PK3 (ZIP) archive
using var pk3 = new Pk3Reader("mymod.pk3");

// Loose folder
using var folder = new FolderReader("./mymod/");
```

## Listing Lumps

Every archive exposes its contents as `LumpEntry` objects:

```csharp
using var archive = ArchiveReaderFactory.Open("DOOM2.WAD");

foreach (var entry in archive.GetEntries())
{
    Console.WriteLine($"{entry.Name,-12} {entry.Category,-10} {entry.Size,8} bytes");
}
```

Each entry has a `Category` indicating its type (Map, Flat, Sprite, Sound, Music, etc.). You can filter by category:

```csharp
// WAD reader has a convenience method for category filtering
using var wad = new WadArchiveReader("DOOM2.WAD");

var musicLumps = wad.GetEntriesByCategory(LumpCategory.Music);
foreach (var lump in musicLumps)
{
    Console.WriteLine($"Music: {lump.Name}");
}
```

## Reading Lump Data

Read raw bytes from any entry:

```csharp
using var archive = ArchiveReaderFactory.Open("DOOM2.WAD");

var entry = archive.GetEntry("PLAYPAL");
if (entry != null)
{
    byte[] data = archive.ReadLump(entry);
    Console.WriteLine($"PLAYPAL is {data.Length} bytes");
}
```

Or open a stream for larger lumps:

```csharp
var entry = archive.GetEntry("D_RUNNIN");
using var stream = archive.OpenLump(entry);
// Process stream...
```

## Reading Maps

WAD.NET fully parses map data from all three formats (DOOM, Hexen, and [UDMF](https://doomwiki.org/wiki/UDMF)):

```csharp
using WAD.NET.Archives;
using WAD.NET.Concrete;
using WAD.NET.Maps;

// First, get a parsed Wad object
using var wadReader = new WadArchiveReader("DOOM2.WAD");
var wad = wadReader.ReadWad();

// Read maps
var mapReader = new MapReader();

// Get all map names
var mapNames = mapReader.GetMapNames(wad);
Console.WriteLine($"Maps: {string.Join(", ", mapNames)}");

// Read a specific map
var map = mapReader.ReadMap(wad, "MAP01");
if (map != null)
{
    Console.WriteLine($"{map.Name} ({map.Format}):");
    Console.WriteLine($"  Things:   {map.ThingCount}");
    Console.WriteLine($"  Vertices: {map.VertexCount}");
    Console.WriteLine($"  Linedefs: {map.LinedefCount}");
    Console.WriteLine($"  Sidedefs: {map.SidedefCount}");
    Console.WriteLine($"  Sectors:  {map.SectorCount}");
}
```

See [Working with Maps](working-with-maps.md) for format-specific details.

## Loading Multiple Archives

DOOM mods work by layering archives - a base IWAD plus one or more PWADs that override resources. WAD.NET models this with `CompositeArchive`:

```csharp
using WAD.NET.Composite;

using var composite = CompositeArchive.Load(
    "DOOM2.WAD",       // Base IWAD
    "mymod.wad",       // PWAD overrides
    "textures.pk3"     // More overrides
);

// Access the effective (resolved) lump set
foreach (var kvp in composite.EffectiveLumps)
{
    var lump = kvp.Value;
    Console.WriteLine($"{lump.Lump.Name}: from {Path.GetFileName(lump.SourcePath)} ({lump.Resolution})");
}

// See what was overridden
Console.WriteLine($"\nOverrides: {composite.Overrides.Count}");
Console.WriteLine($"Additions: {composite.Additions.Count}");
```

See [Composite Archives](composite-archives.md) for conflict detection and analysis.

## Building a WAD

Create WAD files programmatically with the fluent builder API:

```csharp
using WAD.NET.Authoring;
using WAD.NET.Enums;

var builder = new WadBuilder(WadType.PWAD)
    .AddMarker("FF_START")
    .AddLump("FLAT01", flatData)
    .AddLump("FLAT02", flatData2)
    .AddMarker("FF_END")
    .AddLump("DEHACKED", dehackedBytes);

builder.WriteToFile("mymod.wad");
```

See [Building WADs & PK3s](building-wads.md) for the full builder API.

## Validating a WAD

Check a WAD for structural issues:

```csharp
using WAD.NET.Validation;

using var wadReader = new WadArchiveReader("mymod.wad");
var wad = wadReader.ReadWad();

var validator = new WadValidator();
var result = validator.Validate(wad);

if (result.IsValid)
{
    Console.WriteLine("WAD is valid!");
}
else
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"ERROR [{error.Code}]: {error.Message}");
    }
    foreach (var warning in result.Warnings)
    {
        Console.WriteLine($"WARN  [{warning.Code}]: {warning.Message}");
    }
}
```

## Detecting Source Port Requirements

Automatically determine what source port a mod requires:

```csharp
using WAD.NET.Detection;

using var archive = ArchiveReaderFactory.Open("complexmod.pk3");
var analyzer = new CompatibilityAnalyzer();
var compat = analyzer.Analyze(archive);

Console.WriteLine($"Minimum port: {compat.MinimumPort}");
Console.WriteLine(compat.Description);
Console.WriteLine($"Features: {string.Join(", ", compat.RequiredFeatures)}");
```

## Next Steps

- [Architecture](architecture.md) - Understand the library's design
- [Reading Archives](reading-archives.md) - Deep dive into archive readers
- [Working with Maps](working-with-maps.md) - Map format details and data access
- [ZScript & DECORATE Parser](zscript-parser.md) - Full AST parser for ZDoom scripting languages
- [Game Databases](game-databases.md) - Look up thing types, linedef actions, and sector specials
