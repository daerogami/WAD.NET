# Phase 3: PK3/ZIP Archive Support

## Overview

This phase adds support for PK3 (ZIP-based) archives, which are the modern standard for DOOM modding. PK3 files use standard ZIP compression with a specific directory structure.

## Priority: MEDIUM

PK3 support is essential for modern mods but not required for classic WAD parsing.

---

## Archive Formats

| Extension | Format | Compression | Usage |
|-----------|--------|-------------|-------|
| `.pk3` | ZIP | Deflate | Standard ZDoom/GZDoom mods |
| `.pkz` | ZIP | Deflate | ZDoom alternate extension |
| `.pke` | ZIP | Deflate | Eternity Engine mods |
| `.ipk3` | ZIP | Deflate | Standalone game mods |
| `.pk7` | 7z | LZMA | High-compression mods |
| `.wad` | WAD | None | Classic format |
| `folder/` | Directory | N/A | Development/debug mode |

---

## Task 3.1: Archive Reader Abstraction

### Interface Design
```csharp
/// <summary>
/// Base interface for all archive readers (WAD, PK3, folder)
/// </summary>
public interface IArchiveReader : IDisposable
{
    /// <summary>Archive file path or folder path</summary>
    string Path { get; }

    /// <summary>Archive type (WAD, PK3, PK7, Folder)</summary>
    ArchiveType Type { get; }

    /// <summary>Get all lump entries in the archive</summary>
    IEnumerable<LumpEntry> GetEntries();

    /// <summary>Get a specific lump by name</summary>
    LumpEntry? GetEntry(string name);

    /// <summary>Read lump data</summary>
    byte[] ReadLump(LumpEntry entry);

    /// <summary>Read lump data as stream</summary>
    Stream OpenLump(LumpEntry entry);

    /// <summary>Check if archive contains a lump</summary>
    bool Contains(string name);
}

public enum ArchiveType
{
    WAD,
    PK3,
    PK7,
    Folder
}

/// <summary>
/// Represents a lump/file entry in an archive
/// </summary>
public class LumpEntry
{
    /// <summary>Lump name (8-char max for WAD, full path for PK3)</summary>
    public string Name { get; init; }

    /// <summary>Full path within archive (for PK3/folder)</summary>
    public string FullPath { get; init; }

    /// <summary>Compressed size in bytes</summary>
    public long CompressedSize { get; init; }

    /// <summary>Uncompressed size in bytes</summary>
    public long Size { get; init; }

    /// <summary>Lump category based on location/name</summary>
    public LumpCategory Category { get; init; }

    /// <summary>Whether this is a marker lump (zero size)</summary>
    public bool IsMarker => Size == 0;
}

public enum LumpCategory
{
    Unknown,
    Map,
    Sprite,
    Flat,
    Texture,
    Patch,
    Sound,
    Music,
    Graphic,
    ACS,
    Script,
    Definition
}
```

---

## Task 3.2: PK3 Reader Implementation

### PK3 Directory Structure
```
mod.pk3/
├── acs/                # Compiled ACS scripts
├── actors/             # DECORATE actor definitions (legacy)
├── brightmaps/         # Brightmap textures
├── colormaps/          # Custom colormaps
├── decorate/           # DECORATE definitions
│   └── *.txt
├── filter/             # Game-specific resources
│   ├── doom.doom2/
│   └── doom.id/
├── flats/              # Floor/ceiling textures
├── fonts/              # Font definitions
├── graphics/           # Menu/UI graphics
├── hires/              # High-resolution textures
├── maps/               # Map files (WAD or UDMF)
│   ├── MAP01.wad
│   └── MAP02/          # UDMF folder structure
│       ├── TEXTMAP
│       └── SCRIPTS
├── models/             # 3D models
├── music/              # Music tracks
├── patches/            # Texture patches
├── sounds/             # Sound effects
├── sprites/            # Sprite graphics
├── textures/           # Wall textures
├── voxels/             # Voxel models
├── zscript/            # ZScript source files
│   └── *.zs
├── ANIMDEFS            # Animation definitions
├── DECORATE            # Main DECORATE file
├── GLDEFS              # OpenGL definitions
├── MAPINFO             # Map metadata
├── MENUDEF             # Menu definitions
├── SNDINFO             # Sound definitions
├── TERRAIN             # Floor effects
├── TEXTURES            # Texture definitions
└── ZSCRIPT             # Main ZScript file
```

### Implementation
```csharp
public sealed class Pk3Reader : IArchiveReader
{
    private readonly ZipArchive _archive;
    private readonly Dictionary<string, LumpEntry> _entries;

    public string Path { get; }
    public ArchiveType Type => ArchiveType.PK3;

    public Pk3Reader(string filePath)
    {
        Path = filePath;
        _archive = ZipFile.OpenRead(filePath);
        _entries = BuildEntryIndex();
    }

    public Pk3Reader(Stream stream, bool leaveOpen = false)
    {
        Path = string.Empty;
        _archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen);
        _entries = BuildEntryIndex();
    }

    private Dictionary<string, LumpEntry> BuildEntryIndex()
    {
        var entries = new Dictionary<string, LumpEntry>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var zipEntry in _archive.Entries)
        {
            if (zipEntry.FullName.EndsWith('/'))
                continue;  // Skip directories

            var entry = CreateLumpEntry(zipEntry);
            var key = GetLumpName(zipEntry.FullName);

            // Handle duplicates by using full path
            if (entries.ContainsKey(key))
                key = zipEntry.FullName;

            entries[key] = entry;
        }

        return entries;
    }

    private LumpEntry CreateLumpEntry(ZipArchiveEntry zipEntry)
    {
        return new LumpEntry
        {
            Name = GetLumpName(zipEntry.FullName),
            FullPath = zipEntry.FullName,
            CompressedSize = zipEntry.CompressedLength,
            Size = zipEntry.Length,
            Category = CategorizeEntry(zipEntry.FullName)
        };
    }

    private static string GetLumpName(string fullPath)
    {
        var fileName = System.IO.Path.GetFileName(fullPath);
        var nameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(fileName);

        // Truncate to 8 chars for WAD compatibility
        if (nameWithoutExt.Length > 8)
            return nameWithoutExt[..8].ToUpperInvariant();

        return nameWithoutExt.ToUpperInvariant();
    }

    private static LumpCategory CategorizeEntry(string path)
    {
        var parts = path.Split('/', '\\');
        if (parts.Length < 1) return LumpCategory.Unknown;

        var folder = parts[0].ToLowerInvariant();

        return folder switch
        {
            "maps" => LumpCategory.Map,
            "sprites" => LumpCategory.Sprite,
            "flats" => LumpCategory.Flat,
            "textures" => LumpCategory.Texture,
            "patches" => LumpCategory.Patch,
            "sounds" => LumpCategory.Sound,
            "music" => LumpCategory.Music,
            "graphics" => LumpCategory.Graphic,
            "acs" => LumpCategory.ACS,
            "zscript" or "decorate" => LumpCategory.Script,
            _ => CategorizeByFileName(path)
        };
    }

    private static LumpCategory CategorizeByFileName(string path)
    {
        var fileName = System.IO.Path.GetFileName(path).ToUpperInvariant();

        if (fileName is "MAPINFO" or "ZMAPINFO" or "UMAPINFO")
            return LumpCategory.Definition;
        if (fileName is "DECORATE" or "ZSCRIPT")
            return LumpCategory.Script;
        if (fileName is "SNDINFO" or "SNDSEQ")
            return LumpCategory.Definition;
        if (fileName is "TEXTURES" or "ANIMDEFS")
            return LumpCategory.Definition;

        return LumpCategory.Unknown;
    }

    public IEnumerable<LumpEntry> GetEntries() => _entries.Values;

    public LumpEntry? GetEntry(string name)
    {
        return _entries.TryGetValue(name, out var entry) ? entry : null;
    }

    public byte[] ReadLump(LumpEntry entry)
    {
        var zipEntry = _archive.GetEntry(entry.FullPath)
            ?? throw new InvalidOperationException(
                $"Entry not found: {entry.FullPath}");

        using var stream = zipEntry.Open();
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    public Stream OpenLump(LumpEntry entry)
    {
        var zipEntry = _archive.GetEntry(entry.FullPath)
            ?? throw new InvalidOperationException(
                $"Entry not found: {entry.FullPath}");

        return zipEntry.Open();
    }

    public bool Contains(string name) => _entries.ContainsKey(name);

    public void Dispose() => _archive.Dispose();
}
```

---

## Task 3.3: PK7 (7z) Reader Implementation

### Dependencies
Add a reference to a 7z library (e.g., `SharpCompress` or `SevenZipExtractor`):

```xml
<PackageReference Include="SharpCompress" Version="0.34.0" />
```

### Implementation
```csharp
using SharpCompress.Archives;
using SharpCompress.Archives.SevenZip;

public sealed class Pk7Reader : IArchiveReader
{
    private readonly SevenZipArchive _archive;
    private readonly Dictionary<string, LumpEntry> _entries;

    public string Path { get; }
    public ArchiveType Type => ArchiveType.PK7;

    public Pk7Reader(string filePath)
    {
        Path = filePath;
        _archive = SevenZipArchive.Open(filePath);
        _entries = BuildEntryIndex();
    }

    private Dictionary<string, LumpEntry> BuildEntryIndex()
    {
        var entries = new Dictionary<string, LumpEntry>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var archiveEntry in _archive.Entries)
        {
            if (archiveEntry.IsDirectory)
                continue;

            var entry = new LumpEntry
            {
                Name = GetLumpName(archiveEntry.Key),
                FullPath = archiveEntry.Key,
                CompressedSize = archiveEntry.CompressedSize,
                Size = archiveEntry.Size,
                Category = CategorizeEntry(archiveEntry.Key)
            };

            entries[entry.Name] = entry;
        }

        return entries;
    }

    public byte[] ReadLump(LumpEntry entry)
    {
        var archiveEntry = _archive.Entries
            .FirstOrDefault(e => e.Key == entry.FullPath)
            ?? throw new InvalidOperationException(
                $"Entry not found: {entry.FullPath}");

        using var stream = archiveEntry.OpenEntryStream();
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    // Similar methods to Pk3Reader...

    public void Dispose() => _archive.Dispose();
}
```

---

## Task 3.4: Folder Reader Implementation

### Purpose
Allow reading unpacked mod folders for development/debugging.

### Implementation
```csharp
public sealed class FolderReader : IArchiveReader
{
    private readonly Dictionary<string, LumpEntry> _entries;

    public string Path { get; }
    public ArchiveType Type => ArchiveType.Folder;

    public FolderReader(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            throw new DirectoryNotFoundException(folderPath);

        Path = folderPath;
        _entries = BuildEntryIndex();
    }

    private Dictionary<string, LumpEntry> BuildEntryIndex()
    {
        var entries = new Dictionary<string, LumpEntry>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var file in Directory.EnumerateFiles(Path, "*", SearchOption.AllDirectories))
        {
            var relativePath = System.IO.Path.GetRelativePath(Path, file)
                .Replace('\\', '/');

            var entry = new LumpEntry
            {
                Name = GetLumpName(relativePath),
                FullPath = relativePath,
                CompressedSize = new FileInfo(file).Length,
                Size = new FileInfo(file).Length,
                Category = CategorizeEntry(relativePath)
            };

            entries[entry.Name] = entry;
        }

        return entries;
    }

    public byte[] ReadLump(LumpEntry entry)
    {
        var fullPath = System.IO.Path.Combine(Path, entry.FullPath);
        return File.ReadAllBytes(fullPath);
    }

    public Stream OpenLump(LumpEntry entry)
    {
        var fullPath = System.IO.Path.Combine(Path, entry.FullPath);
        return File.OpenRead(fullPath);
    }

    // Other interface methods...

    public void Dispose() { }  // Nothing to dispose
}
```

---

## Task 3.5: Archive Factory

### Implementation
```csharp
public static class ArchiveReaderFactory
{
    public static IArchiveReader Open(string path)
    {
        if (Directory.Exists(path))
            return new FolderReader(path);

        if (!File.Exists(path))
            throw new FileNotFoundException("Archive not found", path);

        // Detect by magic bytes, not extension
        using var fs = File.OpenRead(path);
        var magic = new byte[4];
        fs.Read(magic, 0, 4);

        // WAD: "IWAD" or "PWAD"
        if (magic[0] == 'I' || magic[0] == 'P')
        {
            if (magic[1] == 'W' && magic[2] == 'A' && magic[3] == 'D')
            {
                fs.Dispose();
                return new WadReader(path);
            }
        }

        // ZIP/PK3: "PK\x03\x04"
        if (magic[0] == 'P' && magic[1] == 'K' &&
            magic[2] == 0x03 && magic[3] == 0x04)
        {
            fs.Dispose();
            return new Pk3Reader(path);
        }

        // 7z/PK7: "7z\xBC\xAF"
        if (magic[0] == '7' && magic[1] == 'z' &&
            magic[2] == 0xBC && magic[3] == 0xAF)
        {
            fs.Dispose();
            return new Pk7Reader(path);
        }

        throw new FormatException($"Unknown archive format: {path}");
    }
}
```

---

## Task 3.6: Nested WAD Handling

PK3 files can contain WAD files (commonly in the `maps/` folder):

```csharp
public class Pk3Reader : IArchiveReader
{
    /// <summary>
    /// Get all embedded WAD files in the archive
    /// </summary>
    public IEnumerable<string> GetEmbeddedWads()
    {
        return _entries.Values
            .Where(e => e.FullPath.EndsWith(".wad", StringComparison.OrdinalIgnoreCase))
            .Select(e => e.FullPath);
    }

    /// <summary>
    /// Open an embedded WAD as a reader
    /// </summary>
    public WadReader OpenEmbeddedWad(string wadPath)
    {
        var entry = _entries.Values
            .FirstOrDefault(e => e.FullPath.Equals(wadPath, StringComparison.OrdinalIgnoreCase))
            ?? throw new FileNotFoundException($"Embedded WAD not found: {wadPath}");

        var zipEntry = _archive.GetEntry(entry.FullPath);
        using var stream = zipEntry.Open();

        // Read into memory (WADs are typically small)
        var ms = new MemoryStream();
        stream.CopyTo(ms);
        ms.Position = 0;

        return new WadReader(ms, leaveOpen: false, wadName: entry.Name);
    }
}
```

---

## Task 3.7: Refactor WadReader to Implement IArchiveReader

### Changes Required
```csharp
public class WadReader : IArchiveReader
{
    private readonly List<LumpEntry> _entries;
    private readonly BinaryReader _reader;

    public string Path { get; }
    public ArchiveType Type => ArchiveType.WAD;
    public WadType WadType { get; private set; }

    public WadReader(string filePath)
    {
        Path = filePath;
        _reader = new BinaryReader(File.OpenRead(filePath));
        _entries = ReadDirectory();
    }

    public WadReader(Stream stream, bool leaveOpen = false, string wadName = null)
    {
        Path = wadName ?? string.Empty;
        _reader = new BinaryReader(stream, Encoding.ASCII, leaveOpen);
        _entries = ReadDirectory();
    }

    private List<LumpEntry> ReadDirectory()
    {
        WadType = GetWadFileType();
        var lumpCount = _reader.ReadInt32();
        var directoryOffset = _reader.ReadInt32();

        _reader.BaseStream.Seek(directoryOffset, SeekOrigin.Begin);

        var entries = new List<LumpEntry>(lumpCount);
        for (int i = 0; i < lumpCount; i++)
        {
            var offset = _reader.ReadInt32();
            var size = _reader.ReadInt32();
            var nameBytes = _reader.ReadBytes(8);
            var name = ReadLumpName(nameBytes);

            entries.Add(new LumpEntry
            {
                Name = name,
                FullPath = name,
                CompressedSize = size,
                Size = size,
                Category = CategorizeLump(name, entries)
            });
        }

        return entries;
    }

    public IEnumerable<LumpEntry> GetEntries() => _entries;

    public LumpEntry? GetEntry(string name)
    {
        return _entries.FirstOrDefault(e =>
            e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public byte[] ReadLump(LumpEntry entry)
    {
        var index = _entries.IndexOf(entry);
        if (index < 0)
            throw new InvalidOperationException($"Entry not found: {entry.Name}");

        // Calculate offset from directory
        var directoryOffset = 12;  // After header
        var entryOffset = directoryOffset + (index * 16);

        _reader.BaseStream.Seek(entryOffset, SeekOrigin.Begin);
        var lumpOffset = _reader.ReadInt32();
        var lumpSize = _reader.ReadInt32();

        _reader.BaseStream.Seek(lumpOffset, SeekOrigin.Begin);
        return _reader.ReadBytes(lumpSize);
    }

    public Stream OpenLump(LumpEntry entry)
    {
        var data = ReadLump(entry);
        return new MemoryStream(data, writable: false);
    }

    public bool Contains(string name) => GetEntry(name) != null;

    // Legacy method for backwards compatibility
    public Wad ReadWad()
    {
        var wad = new Wad
        {
            Name = System.IO.Path.GetFileName(Path),
            WadType = WadType
        };

        foreach (var entry in _entries)
        {
            if (entry.IsMarker)
                continue;

            var data = ReadLump(entry);
            var lump = CreateLump(entry, data);
            wad.Lumps.Enqueue(lump);
        }

        return wad;
    }

    public void Dispose()
    {
        _reader?.Dispose();
    }
}
```

---

## Task 3.8: Remove CompressedWadReader (COMPLETED)

### Problem
The original `CompressedWadReader` duplicated `WadReader` code and didn't handle compression correctly.

### Solution
`CompressedWadReader` was removed from the codebase. Use `ArchiveReaderFactory` instead:

```csharp
// Use the unified factory for all archive types
var reader = ArchiveReaderFactory.Open("mod.pk3");  // Returns Pk3Reader
var reader = ArchiveReaderFactory.Open("DOOM.WAD"); // Returns WadArchiveReader
```

---

## Acceptance Criteria

1. PK3 files load correctly via `Pk3Reader` or `ArchiveReaderFactory.Open()`
2. All standard PK3 folder categories are recognized
3. Embedded WADs can be opened and read
4. Folder mode works for development
5. Archive type is auto-detected by magic bytes, not extension
6. `WadReader` implements `IArchiveReader` interface
7. Legacy `ReadWad()` method still works
8. PK7 files load correctly (if SharpCompress added)

---

## Test Cases

```csharp
[Fact]
public void ShouldOpenPk3ByMagicBytes()
{
    // Create a file with .wad extension but PK3 content
    using var reader = ArchiveReaderFactory.Open("test.wad");  // Actually a ZIP
    Assert.Equal(ArchiveType.PK3, reader.Type);
}

[Fact]
public void ShouldCategorizeSpritesFolder()
{
    using var reader = new Pk3Reader("mod.pk3");
    var entry = reader.GetEntry("PLAYA1");  // sprites/PLAYA1.png

    Assert.Equal(LumpCategory.Sprite, entry.Category);
}

[Fact]
public void ShouldOpenEmbeddedWad()
{
    using var pk3 = new Pk3Reader("mod.pk3");
    using var wad = pk3.OpenEmbeddedWad("maps/MAP01.wad");

    var things = wad.GetEntry("THINGS");
    Assert.NotNull(things);
}

[Fact]
public void ShouldReadFolder()
{
    using var reader = new FolderReader("./testmod/");

    var entries = reader.GetEntries().ToList();
    Assert.NotEmpty(entries);
}
```

---

## Migration Guide

### Before (v1.x)
```csharp
using var reader = new WadReader("DOOM.WAD");
var wad = reader.ReadWad();

foreach (var lump in wad.Lumps)
{
    Console.WriteLine(lump.Name);
}
```

### After (v2.x)
```csharp
// New unified API
using var reader = ArchiveReaderFactory.Open("mod.pk3");

foreach (var entry in reader.GetEntries())
{
    Console.WriteLine($"{entry.Name} ({entry.Category})");

    var data = reader.ReadLump(entry);
    // Process data...
}

// Legacy API still works
using var wadReader = new WadReader("DOOM.WAD");
var wad = wadReader.ReadWad();  // Same as before
```
