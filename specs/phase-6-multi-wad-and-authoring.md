# Phase 6: Multi-WAD Analysis, Authoring & Utilities

## Overview

This phase adds features for working with multiple WADs as a cohesive unit, writing WAD files, engine-agnostic launch configuration, and minimal map visualization:
- Multi-WAD loading with lump cascade resolution
- WAD/PK3 writing and lump manipulation
- Engine-agnostic launch configuration model
- Minimal map thumbnail rendering

## Priority: MEDIUM

These features enable practical tooling scenarios: mod managers, WAD collection sharing, and programmatic WAD construction.

---

## Multi-WAD Loading & Cascade Analysis

### Background

DOOM's resource system is built on layering: an IWAD provides base content, and one or more PWADs override or extend it. Understanding the effective state after loading multiple archives in order is fundamental to analyzing mod configurations.

This feature loads multiple archives (WAD, PK3, PK7, or folder) in a specified order and resolves the final effective lump set, tracking what was added, replaced, or left unchanged.

### Implementation

```csharp
namespace WAD.NET.Composite
{
    public enum LumpResolutionType
    {
        Base,       // Original from IWAD/first archive
        Override,   // Replaced by a later archive
        Added       // New lump not in any earlier archive
    }

    public class ResolvedLump
    {
        public ILump Lump { get; init; }
        public LumpResolutionType Resolution { get; init; }
        public int SourceIndex { get; init; }         // Index into the load order
        public string SourcePath { get; init; }       // Path of the archive that provided this lump
        public ILump OverriddenLump { get; init; }    // The lump this replaced, if Override
    }

    public class CompositeArchive
    {
        public IReadOnlyList<string> LoadOrder { get; }
        public IReadOnlyDictionary<string, ResolvedLump> EffectiveLumps { get; }
        public IReadOnlyList<ResolvedLump> Overrides { get; }
        public IReadOnlyList<ResolvedLump> Additions { get; }

        /// <summary>
        /// Loads multiple archives in order and resolves the effective lump set.
        /// The first archive is treated as the base (typically an IWAD).
        /// Subsequent archives override or extend it.
        /// </summary>
        public static CompositeArchive Load(params string[] archivePaths);

        /// <summary>
        /// Loads multiple archives using pre-constructed readers.
        /// </summary>
        public static CompositeArchive Load(params IArchiveReader[] readers);

        /// <summary>
        /// Returns all lumps from a specific archive in the load order.
        /// </summary>
        public IEnumerable<ResolvedLump> GetLumpsFromSource(int sourceIndex);

        /// <summary>
        /// Returns the full override chain for a given lump name across all archives.
        /// </summary>
        public IReadOnlyList<ILump> GetOverrideChain(string lumpName);
    }
}
```

### Namespace Resolution Rules

1. Lumps are matched by name (8-char, case-insensitive for WAD format)
2. Later archives in the load order take precedence
3. Marker-bounded sections (F_START/F_END, S_START/S_END, etc.) merge additively — new entries are appended, existing names are overridden
4. Map marker groups (e.g., MAP01 + its sub-lumps) are replaced atomically — if a PWAD contains MAP01, all of its map lumps replace the base map

---

## Task 6.1: CompositeArchive Core

Implement `CompositeArchive` with support for loading multiple `IArchiveReader` instances and resolving the effective lump set. Handle WAD, PK3, PK7, and folder readers via `ArchiveReaderFactory`.

### Acceptance Criteria

1. Loading an IWAD alone produces all lumps with `Resolution = Base`
2. Loading IWAD + PWAD correctly marks overridden lumps
3. Loading IWAD + multiple PWADs resolves in correct priority order (last wins)
4. `GetOverrideChain` returns the full history for a given lump name
5. Mixed archive types (WAD + PK3) resolve correctly
6. Map groups are replaced atomically

---

## Task 6.2: Composite Conflict & Analysis Reporting

Extend `CompositeArchive` with analysis capabilities that integrate with the existing `CompatibilityAnalyzer`.

```csharp
public class CompositeAnalysis
{
    public ModCompatibility Compatibility { get; init; }
    public IReadOnlyList<LumpConflict> Conflicts { get; init; }
}

public class LumpConflict
{
    public string LumpName { get; init; }
    public IReadOnlyList<LumpConflictSource> Sources { get; init; }
}

public class LumpConflictSource
{
    public int SourceIndex { get; init; }
    public string SourcePath { get; init; }
    public ILump Lump { get; init; }
}

// Extension on CompositeArchive
public CompositeAnalysis Analyze();
```

### Acceptance Criteria

1. Conflicts are reported when 2+ non-base archives provide the same lump
2. Compatibility analysis runs against the effective (resolved) lump set
3. Analysis results include per-archive feature attribution

---

## WAD Writing & Lump Manipulation

### Background

Enable programmatic creation and modification of WAD and PK3 archives at the lump level. This is raw lump manipulation — no structured editing of map geometry, sprites, etc.

### Implementation

```csharp
namespace WAD.NET.Authoring
{
    public class WadBuilder
    {
        /// <summary>
        /// Creates a new empty WAD builder.
        /// </summary>
        public WadBuilder(WadType type = WadType.PWAD);

        /// <summary>
        /// Creates a builder pre-populated from an existing archive.
        /// </summary>
        public static WadBuilder FromArchive(IArchiveReader reader);

        /// <summary>
        /// Adds a lump with raw byte data.
        /// </summary>
        public WadBuilder AddLump(string name, byte[] data);

        /// <summary>
        /// Adds a marker lump (zero-length).
        /// </summary>
        public WadBuilder AddMarker(string name);

        /// <summary>
        /// Replaces an existing lump's data by name.
        /// Throws KeyNotFoundException if the lump does not exist.
        /// </summary>
        public WadBuilder ReplaceLump(string name, byte[] data);

        /// <summary>
        /// Removes a lump by name.
        /// </summary>
        public WadBuilder RemoveLump(string name);

        /// <summary>
        /// Inserts a lump at a specific position in the directory.
        /// </summary>
        public WadBuilder InsertLump(int index, string name, byte[] data);

        /// <summary>
        /// Returns the current lump list for inspection.
        /// </summary>
        public IReadOnlyList<(string Name, int Size)> GetDirectory();

        /// <summary>
        /// Writes the WAD to a stream.
        /// </summary>
        public void WriteTo(Stream output);

        /// <summary>
        /// Writes the WAD to a file path.
        /// </summary>
        public void WriteToFile(string path);
    }

    public class Pk3Builder
    {
        /// <summary>
        /// Creates a new empty PK3 builder.
        /// </summary>
        public Pk3Builder();

        /// <summary>
        /// Creates a builder pre-populated from an existing PK3/PK7.
        /// </summary>
        public static Pk3Builder FromArchive(IArchiveReader reader);

        /// <summary>
        /// Adds a file entry at the given path within the archive.
        /// </summary>
        public Pk3Builder AddEntry(string path, byte[] data);

        /// <summary>
        /// Replaces an existing entry's data.
        /// </summary>
        public Pk3Builder ReplaceEntry(string path, byte[] data);

        /// <summary>
        /// Removes an entry by path.
        /// </summary>
        public Pk3Builder RemoveEntry(string path);

        /// <summary>
        /// Writes the PK3 (ZIP) to a stream.
        /// </summary>
        public void WriteTo(Stream output);

        /// <summary>
        /// Writes the PK3 to a file path.
        /// </summary>
        public void WriteToFile(string path);
    }
}
```

---

## Task 6.3: WadBuilder

Implement `WadBuilder` for creating and modifying WAD files.

### WAD Write Format

The output must produce a valid WAD file per the standard format:

1. 12-byte header: magic (IWAD/PWAD) + lump count + directory offset
2. Lump data written sequentially
3. Directory entries (16 bytes each) written after all lump data
4. Lump names padded to 8 bytes with null characters

### Acceptance Criteria

1. A WAD created from scratch with `AddLump` produces a valid WAD readable by `WadReader`
2. `FromArchive` round-trips: load a WAD, build from it, write it, re-read it — lumps match
3. `ReplaceLump` changes only the target lump's data
4. `RemoveLump` removes the lump from directory and data
5. `InsertLump` places the lump at the correct directory position
6. Marker lumps are written with size 0
7. `GetDirectory` reflects current state after mutations

---

## Task 6.4: Pk3Builder

Implement `Pk3Builder` for creating and modifying PK3 (ZIP) archives. Use the existing SharpCompress dependency for ZIP writing.

### Acceptance Criteria

1. A PK3 created from scratch is a valid ZIP readable by `Pk3Reader`
2. `FromArchive` round-trips correctly
3. Folder structure within the ZIP follows PK3 conventions (maps/, sprites/, etc.)
4. Entry paths are normalized to forward slashes

---

## Engine-Agnostic Launch Configuration

### Background

WAD collections are commonly shared as a set of files with a specific load order and optional gameplay settings. This feature provides a common model for representing that configuration, decoupled from any specific engine's config file format.

The existing `ConfigUtilities.cs` (Zandronum-specific) should be moved to `SourcePorts/Zandronum/` and refactored to implement the serializer interface.

### Implementation

```csharp
namespace WAD.NET.LaunchConfig
{
    public class WadLaunchConfiguration
    {
        /// <summary>
        /// Path or identifier for the base IWAD.
        /// </summary>
        public string Iwad { get; set; }

        /// <summary>
        /// Ordered list of PWADs/mods to load.
        /// </summary>
        public List<string> Files { get; set; } = new();

        /// <summary>
        /// Optional starting map (e.g., "MAP01", "E1M1").
        /// </summary>
        public string Map { get; set; }

        /// <summary>
        /// Optional skill level (1-5, where 1 = easiest).
        /// </summary>
        public int? Skill { get; set; }

        /// <summary>
        /// Optional game mode for multiplayer.
        /// </summary>
        public GameMode? Mode { get; set; }

        /// <summary>
        /// Optional number of players (for multiplayer configs).
        /// </summary>
        public int? Players { get; set; }

        /// <summary>
        /// Optional network port.
        /// </summary>
        public int? Port { get; set; }

        /// <summary>
        /// Engine-specific extra arguments that don't fit the common model.
        /// </summary>
        public Dictionary<string, string> ExtraParameters { get; set; } = new();
    }

    public enum GameMode
    {
        SinglePlayer,
        Cooperative,
        Deathmatch,
        TeamDeathmatch,
        CaptureTheFlag,
        LastManStanding,
        Survival
    }

    public interface ILaunchConfigSerializer
    {
        /// <summary>
        /// Serializes a launch configuration to the engine's native format.
        /// </summary>
        string Serialize(WadLaunchConfiguration config);

        /// <summary>
        /// Generates command-line arguments for launching the engine.
        /// </summary>
        string[] ToCommandLineArgs(WadLaunchConfiguration config);

        /// <summary>
        /// Attempts to parse an engine-specific config into the common model.
        /// </summary>
        WadLaunchConfiguration Deserialize(string content);
    }
}
```

---

## Task 6.5: Launch Configuration Core

Implement `WadLaunchConfiguration`, `GameMode`, and `ILaunchConfigSerializer`.

### Acceptance Criteria

1. `WadLaunchConfiguration` can represent IWAD + multiple files + all optional settings
2. `ExtraParameters` provides an escape hatch for engine-specific settings
3. All properties are optional except `Iwad`

---

## Task 6.6: Zandronum Launch Config Serializer

Move `ConfigUtilities.cs` to `SourcePorts/Zandronum/ZandronumConfigSerializer.cs` and refactor to implement `ILaunchConfigSerializer`.

```csharp
namespace WAD.NET.SourcePorts.Zandronum
{
    public class ZandronumConfigSerializer : ILaunchConfigSerializer
    {
        public string Serialize(WadLaunchConfiguration config);
        public string[] ToCommandLineArgs(WadLaunchConfiguration config);
        public WadLaunchConfiguration Deserialize(string content);
    }
}
```

### Acceptance Criteria

1. Existing Zandronum join-string parsing works through the new interface
2. `ToCommandLineArgs` produces valid Zandronum command-line arguments
3. `Serialize` generates Zandronum .cfg format
4. Round-trip: Serialize then Deserialize preserves all common fields
5. Zandronum-specific dmflags map to `ExtraParameters`

---

## Map Thumbnail Rendering

### Background

A minimal top-down wireframe renderer for map data. This is intentionally simple — applications that need richer rendering can use the parsed vertex/linedef data directly.

All rendering code must be isolated in a `Rendering/` folder so it can be cleanly removed if needed.

### Implementation

```csharp
namespace WAD.NET.Rendering
{
    public class MapThumbnailOptions
    {
        public int Width { get; set; } = 512;
        public int Height { get; set; } = 512;
        public Color LineColor { get; set; } = Color.White;
        public Color BackgroundColor { get; set; } = Color.Black;
        public Color SecretLineColor { get; set; } = Color.Magenta;
        public Color TwoSidedLineColor { get; set; } = Color.Gray;
        public int Padding { get; set; } = 16;
    }

    public class MapThumbnailRenderer
    {
        /// <summary>
        /// Renders a top-down wireframe of the map to a PNG byte array.
        /// </summary>
        public byte[] RenderToPng(IMap map, MapThumbnailOptions options = null);

        /// <summary>
        /// Renders a top-down wireframe to an ImageSharp Image for further manipulation.
        /// </summary>
        public Image<Rgba32> RenderToImage(IMap map, MapThumbnailOptions options = null);
    }
}
```

### Rendering Rules

1. Auto-scale map geometry to fit within `Width x Height` minus `Padding`
2. Maintain aspect ratio (no stretching)
3. One-sided linedefs drawn in `LineColor` (solid walls)
4. Two-sided linedefs drawn in `TwoSidedLineColor`
5. Secret linedefs (flag 0x0020) drawn in `SecretLineColor`
6. Coordinate transform: DOOM Y-axis is inverted relative to image coordinates

---

## Task 6.7: MapThumbnailRenderer

Implement the renderer in `WAD.NET/Rendering/MapThumbnailRenderer.cs` using the existing ImageSharp dependency.

### Acceptance Criteria

1. Renders a valid PNG from parsed map data
2. Map geometry is auto-scaled and centered
3. Aspect ratio is preserved
4. One-sided, two-sided, and secret lines are visually distinct
5. Empty maps (no linedefs) produce a blank image without errors
6. All rendering code is contained within the `Rendering/` folder

---

## Test Cases

```csharp
// Multi-WAD
[Fact]
public void ShouldResolveOverrides()
{
    var composite = CompositeArchive.Load("doom2.wad", "my_pwad.wad");

    var map01 = composite.EffectiveLumps["MAP01"];
    Assert.Equal(LumpResolutionType.Override, map01.Resolution);
    Assert.Equal(1, map01.SourceIndex);
}

[Fact]
public void ShouldTrackOverrideChain()
{
    var composite = CompositeArchive.Load("doom2.wad", "pwad1.wad", "pwad2.wad");
    var chain = composite.GetOverrideChain("MAP01");

    Assert.Equal(3, chain.Count); // base + 2 overrides
}

// WAD Writing
[Fact]
public void ShouldRoundTripWad()
{
    var builder = new WadBuilder(WadType.PWAD);
    builder.AddLump("TEST", new byte[] { 0x01, 0x02, 0x03 });
    builder.AddMarker("F_START");
    builder.AddLump("FLAT1", flatData);
    builder.AddMarker("F_END");

    using var stream = new MemoryStream();
    builder.WriteTo(stream);
    stream.Position = 0;

    var reader = new WadReader(stream);
    var wad = reader.Load();
    Assert.Equal(4, wad.Lumps.Count);
}

[Fact]
public void ShouldRemoveLump()
{
    var builder = WadBuilder.FromArchive(reader);
    builder.RemoveLump("DEMO1");

    var dir = builder.GetDirectory();
    Assert.DoesNotContain(dir, e => e.Name == "DEMO1");
}

// Launch Config
[Fact]
public void ShouldSerializeZandronumArgs()
{
    var config = new WadLaunchConfiguration
    {
        Iwad = "doom2.wad",
        Files = { "brutal.pk3", "maps.wad" },
        Map = "MAP01",
        Skill = 4
    };

    var serializer = new ZandronumConfigSerializer();
    var args = serializer.ToCommandLineArgs(config);

    Assert.Contains("-iwad", args);
    Assert.Contains("doom2.wad", args);
    Assert.Contains("-file", args);
    Assert.Contains("-warp", args);
    Assert.Contains("-skill", args);
}

// Map Thumbnail
[Fact]
public void ShouldRenderMapThumbnail()
{
    var map = LoadTestMap("MAP01");
    var renderer = new MapThumbnailRenderer();
    var png = renderer.RenderToPng(map);

    Assert.NotEmpty(png);
    // Verify valid PNG header
    Assert.Equal(0x89, png[0]);
    Assert.Equal((byte)'P', png[1]);
    Assert.Equal((byte)'N', png[2]);
    Assert.Equal((byte)'G', png[3]);
}

[Fact]
public void ShouldHandleEmptyMap()
{
    var map = new DoomMap(); // no linedefs
    var renderer = new MapThumbnailRenderer();
    var png = renderer.RenderToPng(map);

    Assert.NotEmpty(png); // blank image, no exception
}
```
