# Phase 6b: Composite Content Access & Cross-Resource Validation

## Overview

This phase addresses three gaps in WAD.NET needed for the waddb-agent integration:
- CompositeArchive content reading (lump bytes from the resolved set)
- Sprite reference extraction from DECORATE and ZScript state blocks
- Cross-resource dependency validation across the effective lump set

## Priority: HIGH

These features are prerequisites for the waddb-agent's resource health checking and content inspection capabilities.

---

## Task 6b.1: CompositeArchive Content Reading

### Problem

`CompositeArchive` currently resolves which lumps are effective but cannot read their content. The internal `CompositeArchiveReader` returns empty byte arrays from `ReadLump`/`OpenLump` because the original `IArchiveReader` instances are disposed after load.

### Solution

Retain the `IArchiveReader` instances for the lifetime of the `CompositeArchive`. Make `CompositeArchive` implement `IDisposable` to clean up the readers. Store source reader references on `ResolvedLump` so content can be read on demand.

### Implementation

```csharp
// CompositeArchive changes:
public class CompositeArchive : IDisposable
{
    private readonly List<IArchiveReader> _readers;
    private bool _disposed;

    /// <summary>
    /// Reads the content of a resolved lump from its source archive.
    /// </summary>
    public byte[] ReadLump(string lumpName);

    /// <summary>
    /// Opens a stream to the content of a resolved lump.
    /// </summary>
    public Stream OpenLump(string lumpName);

    public void Dispose();
}

// ResolvedLump changes:
public class ResolvedLump
{
    // Existing properties...

    /// <summary>
    /// Reference to the source archive reader for content access.
    /// </summary>
    internal IArchiveReader SourceReader { get; init; }
}
```

The `Load(params string[])` overload must **not** dispose the readers after construction. Instead, the readers are retained and disposed when `CompositeArchive.Dispose()` is called.

The `Load(params IArchiveReader[])` overload retains references but does **not** own them — `Dispose()` should not dispose externally-provided readers. Add a private `_ownsReaders` flag to distinguish.

The `CompositeArchiveReader` wrapper used by `Analyze()` should delegate `ReadLump`/`OpenLump` to the actual source readers via the `ResolvedLump.SourceReader` reference.

### Acceptance Criteria

1. `CompositeArchive.ReadLump("THINGS")` returns the byte content from the effective source archive
2. Content matches what the source archive reader returns directly
3. `Load(string[])` disposes readers when `CompositeArchive` is disposed
4. `Load(IArchiveReader[])` does not dispose externally-provided readers
5. `CompositeArchiveReader.ReadLump` returns real data (for `CompatibilityAnalyzer` content-dependent checks)
6. Calling `ReadLump` after `Dispose()` throws `ObjectDisposedException`

---

## Task 6b.2: Sprite Reference Extraction

### Problem

`DecorateScanner` extracts state labels (Spawn, Death, etc.) but not the sprite names used within those states. `ZScriptScanner` doesn't parse states at all. To validate that referenced sprites exist, we need to extract the 4-character sprite prefixes from state definitions.

### DECORATE State Format
```
States
{
  Spawn:
    POSS AB 10 A_Look
    Loop
  See:
    POSS AABBCCDD 4 A_Chase
    Loop
  Death:
    POSS H 5
    POSS I 5 A_Scream
    POSS J 5 A_Fall
    POSS K -1
    Stop
}
```

Each state line starts with a 4-character sprite name followed by frame letters. The sprite prefix (POSS, SHT2, etc.) is what we need to extract.

### ZScript State Format
Identical syntax within States blocks, but embedded in ZScript class definitions.

### Implementation

Add sprite reference extraction to both scanners and create a shared utility:

```csharp
namespace WAD.NET.Detection
{
    public static class StatesSpriteExtractor
    {
        /// <summary>
        /// Extracts unique 4-character sprite prefixes from a States block body.
        /// Excludes TNT1 (invisible sprite) and "----" (null sprite).
        /// </summary>
        public static IReadOnlySet<string> ExtractSpritePrefixes(string statesBody);
    }
}

// Add to DecorateActorDefinition:
public IReadOnlySet<string> ReferencedSprites { get; }

// Add to ZScriptClassDefinition:
public IReadOnlySet<string> ReferencedSprites { get; }
```

The extractor parses lines within a States block, matching the pattern: a 4-character sprite name at the start of a line (after whitespace), followed by one or more frame letters. TNT1 (the invisible sprite) and "----" should be excluded as they don't correspond to real sprite lumps.

### Acceptance Criteria

1. `StatesSpriteExtractor.ExtractSpritePrefixes` correctly extracts sprite names from a States block
2. `DecorateScanner.Scan()` populates `ReferencedSprites` on each actor
3. `ZScriptScanner.Scan()` populates `ReferencedSprites` on each class
4. TNT1 and "----" are excluded
5. Sprite names are uppercased and deduplicated
6. Frame letters are not included — only the 4-char prefix

---

## Task 6b.3: Cross-Resource Dependency Validation

### Problem

`WadValidator` validates structural integrity within a single WAD. There is no validation that checks whether resources referenced by scripts actually exist in the effective lump set. This is critical for the waddb-agent's "broken resource detection."

### Implementation

Create a new validator that operates on `CompositeArchive` (or any `IArchiveReader`):

```csharp
namespace WAD.NET.Validation
{
    public class ResourceDependencyValidator
    {
        /// <summary>
        /// Validates that all cross-resource references resolve within the archive.
        /// </summary>
        public ResourceDependencyResult Validate(IArchiveReader archive);

        /// <summary>
        /// Validates against a composite archive's effective lump set.
        /// </summary>
        public ResourceDependencyResult Validate(CompositeArchive composite);
    }

    public class ResourceDependencyResult
    {
        public IReadOnlyList<ResourceDependencyIssue> Issues { get; init; }
        public bool IsValid => !Issues.Any(i => i.Severity == IssueSeverity.Error);
    }

    public class ResourceDependencyIssue
    {
        public IssueSeverity Severity { get; init; }
        public string Category { get; init; }     // "Sprite", "Sound", "Patch"
        public string Source { get; init; }        // Which lump/script references it
        public string ReferencedName { get; init; } // The missing resource name
        public string Message { get; init; }
    }

    public enum IssueSeverity
    {
        Warning,
        Error
    }
}
```

### Checks to Implement

1. **DECORATE sprite references** — For each DECORATE actor, verify that all `ReferencedSprites` have corresponding sprite lumps (matching the 4-char prefix) in the archive. Severity: Warning (sprites may come from the engine's internal resources).

2. **ZScript sprite references** — Same check for ZScript classes. Severity: Warning.

3. **SNDINFO sound references** — For each sound definition in SNDINFO (`logical_name → lump_name`), verify the lump_name exists in the archive. Severity: Warning (sounds may be optional or engine-provided).

4. **PNAMES patch references** — Parse PNAMES lump (list of patch names used by TEXTURE1/TEXTURE2) and verify each patch exists as a lump. Severity: Error (missing patches cause visual glitches).

5. **TEXTURE1/TEXTURE2 patch indices** — Verify patch indices in texture definitions don't exceed PNAMES count. Severity: Error.

### PNAMES Format (for reference)

```
Offset 0: int32 count
Offset 4: char[8] * count (null-padded patch names)
```

### TEXTURE1/TEXTURE2 Format (for reference)

```
Offset 0: int32 textureCount
Offset 4: int32[textureCount] offsets (relative to start of lump)

Each texture entry:
  char[8] name
  int32   masked (unused)
  int16   width
  int16   height
  int32   columnDirectory (unused)
  int16   patchCount

  For each patch:
    int16 originX
    int16 originY
    int16 patchIndex    ← index into PNAMES
    int16 stepDir       (unused)
    int16 colorMap      (unused)
```

### Acceptance Criteria

1. Missing DECORATE sprites produce warnings with actor name context
2. Missing SNDINFO sound lumps produce warnings with sound name context
3. Missing PNAMES patches produce errors
4. Out-of-range TEXTURE1/TEXTURE2 patch indices produce errors
5. Works with both single `IArchiveReader` and `CompositeArchive`
6. Engine-internal resources (standard DOOM sprites like POSS, SARG, etc.) are not flagged — only check lumps that should be provided by the mod

---

## Test Cases

```csharp
// Task 6b.1 - Content reading
[Fact]
public void CompositeArchive_ReadLump_ReturnsContent()
{
    var builder1 = new WadBuilder(WadType.IWAD);
    builder1.AddLump("TEST", new byte[] { 0x01, 0x02 });
    // ... write to stream, create composite
    var data = composite.ReadLump("TEST");
    Assert.Equal(new byte[] { 0x01, 0x02 }, data);
}

[Fact]
public void CompositeArchive_ReadLump_ReturnsOverriddenContent()
{
    // IWAD has TEST = {0x01}, PWAD has TEST = {0x02}
    var data = composite.ReadLump("TEST");
    Assert.Equal(new byte[] { 0x02 }, data);
}

[Fact]
public void CompositeArchive_Dispose_ThrowsOnRead()
{
    composite.Dispose();
    Assert.Throws<ObjectDisposedException>(() => composite.ReadLump("TEST"));
}

// Task 6b.2 - Sprite extraction
[Fact]
public void ShouldExtractSpritePrefixes()
{
    var states = @"
        Spawn:
            POSS AB 10 A_Look
            Loop
        Death:
            POSS H 5
            BOS2 A 5
            TNT1 A 0
            Stop";
    var sprites = StatesSpriteExtractor.ExtractSpritePrefixes(states);

    Assert.Contains("POSS", sprites);
    Assert.Contains("BOS2", sprites);
    Assert.DoesNotContain("TNT1", sprites);
}

[Fact]
public void DecorateScanner_ShouldPopulateReferencedSprites()
{
    var decorate = @"
        Actor MyMonster : DoomImp
        {
            States
            {
                Spawn:
                    TROO AB 10 A_Look
                    Loop
            }
        }";
    var info = DecorateScanner.Scan(decorate);
    Assert.Contains("TROO", info.Actors[0].ReferencedSprites);
}

// Task 6b.3 - Resource validation
[Fact]
public void ShouldDetectMissingSndInfoSounds()
{
    // Archive with SNDINFO referencing "DSTEST" but no DSTEST lump
    var result = validator.Validate(archive);
    Assert.Contains(result.Issues, i =>
        i.Category == "Sound" && i.ReferencedName == "DSTEST");
}

[Fact]
public void ShouldDetectMissingPnamesPatches()
{
    // Archive with PNAMES listing "WALL01" but no WALL01 lump
    var result = validator.Validate(archive);
    Assert.Contains(result.Issues, i =>
        i.Category == "Patch" && i.Severity == IssueSeverity.Error);
}
```
