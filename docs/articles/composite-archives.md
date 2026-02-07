# Composite Archives

DOOM's mod system works by layering archives: a base IWAD provides the core game data, and one or more PWADs or PK3s override or extend it. WAD.NET's `CompositeArchive` implements this resource layering model with full override tracking and conflict detection.

## How DOOM Resource Layering Works

When DOOM loads multiple archives, later archives override earlier ones by lump name. For example, if both `DOOM2.WAD` and `mymod.wad` contain a lump named `MAP01`, the version from `mymod.wad` wins because it was loaded later.

This applies to all resource types - textures, sounds, sprites, maps, and definitions. The same layering model is used by all source ports including [GZDoom](https://zdoom.org/wiki/), [DSDA-Doom](https://github.com/kraflab/dsda-doom), and [Crispy Doom](https://github.com/fabiangreffrath/crispy-doom).

## Loading Multiple Archives

### From File Paths

```csharp
using WAD.NET.Composite;

// Load order matters - first is the base (IWAD), rest override in order
using var composite = CompositeArchive.Load(
    "DOOM2.WAD",        // Base IWAD
    "mymod.wad",        // First PWAD override
    "textures.pk3"      // Second override (highest priority)
);
```

`CompositeArchive.Load(string[])` auto-detects each archive's format and owns the created readers (they are disposed when the composite is disposed).

### From Pre-Constructed Readers

If you already have `IArchiveReader` instances:

```csharp
using var iwad = new WadArchiveReader("DOOM2.WAD");
using var pwad = new WadArchiveReader("mymod.wad");
using var pk3 = new Pk3Reader("textures.pk3");

// Composite does NOT own these readers - you manage their lifetime
using var composite = CompositeArchive.Load(iwad, pwad, pk3);
```

## Accessing the Effective Lump Set

After loading, `EffectiveLumps` contains the resolved set where each lump name maps to its winning version:

```csharp
foreach (var kvp in composite.EffectiveLumps)
{
    var resolved = kvp.Value;
    Console.WriteLine($"{resolved.Lump.Name}: from {Path.GetFileName(resolved.SourcePath)} ({resolved.Resolution})");
}
```

### Reading Lump Data

```csharp
// Read by name
byte[] data = composite.ReadLump("PLAYPAL");

// Or open a stream
using var stream = composite.OpenLump("D_RUNNIN");
```

### Resolution Types

Each `ResolvedLump` has a `Resolution` property indicating how it was resolved:

| Resolution | Meaning |
|------------|---------|
| `Base` | Comes from the first archive (IWAD) |
| `Override` | Replaces a lump from an earlier archive |
| `Added` | New lump not present in any earlier archive |

## Tracking Overrides and Additions

```csharp
// All lumps that override something from an earlier archive
Console.WriteLine($"Overrides: {composite.Overrides.Count}");
foreach (var over in composite.Overrides)
{
    Console.WriteLine($"  {over.Lump.Name}: {Path.GetFileName(over.SourcePath)} " +
                      $"overrides {Path.GetFileName(over.OverriddenLump?.Name ?? "?")}");
}

// All lumps added by non-base archives
Console.WriteLine($"\nAdditions: {composite.Additions.Count}");
foreach (var add in composite.Additions)
{
    Console.WriteLine($"  {add.Lump.Name}: added by {Path.GetFileName(add.SourcePath)}");
}
```

## Override Chains

See every version of a lump across all archives:

```csharp
var chain = composite.GetOverrideChain("MAP01");
Console.WriteLine($"MAP01 appears in {chain.Count} archive(s):");
foreach (var version in chain)
{
    Console.WriteLine($"  {version.Name}: {version.Size} bytes");
}
```

## Filtering by Source

Get all lumps originating from a specific archive in the load order:

```csharp
// Get lumps from the second archive (index 1)
var moddedLumps = composite.GetLumpsFromSource(1);
foreach (var lump in moddedLumps)
{
    Console.WriteLine($"  {lump.Lump.Name} ({lump.Resolution})");
}
```

## Conflict Detection and Analysis

The `Analyze` method detects conflicts (same lump from multiple non-base archives) and runs compatibility analysis:

```csharp
var analysis = composite.Analyze();

// Conflicts: lump names provided by 2+ non-base archives
foreach (var conflict in analysis.Conflicts)
{
    Console.WriteLine($"Conflict: {conflict.LumpName}");
    foreach (var source in conflict.Sources)
    {
        Console.WriteLine($"  from {Path.GetFileName(source.SourcePath)} ({source.Lump.Size} bytes)");
    }
}

// Compatibility requirements for the combined load
Console.WriteLine($"\nMinimum port: {analysis.Compatibility.MinimumPort}");
Console.WriteLine(analysis.Compatibility.Description);
```

## Map Group Handling

Maps are treated as atomic groups. When a PWAD provides `MAP01`, all of its sub-lumps (THINGS, LINEDEFS, SIDEDEFS, etc.) are loaded together, replacing the entire map from the IWAD. This matches how DOOM source ports handle map replacement.

## Marker Section Handling

Marker-bounded sections (`F_START`/`F_END`, `S_START`/`S_END`, etc.) are resolved correctly: lumps within marker sections from later archives override same-named lumps from earlier archives, and new lumps are added to the section.

## Load Order

The `LoadOrder` property provides the ordered list of archive paths:

```csharp
Console.WriteLine("Load order:");
for (int i = 0; i < composite.LoadOrder.Count; i++)
{
    Console.WriteLine($"  [{i}] {composite.LoadOrder[i]}");
}
```

## Format References

- [Doom Wiki - PWAD](https://doomwiki.org/wiki/PWAD) - How patch WADs work
- [Doom Wiki - WAD](https://doomwiki.org/wiki/WAD) - WAD file format and lump organization
- [ZDoom Wiki - Command line parameters](https://zdoom.org/wiki/Command_line_parameters) - How source ports load multiple archives
