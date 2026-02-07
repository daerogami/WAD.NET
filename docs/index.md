# WAD.NET

A .NET Standard 2.1 library for reading, analyzing, and authoring DOOM WAD files, PK3 archives, and related formats.

WAD.NET provides a comprehensive, cross-platform toolkit for working with content from [DOOM](https://doomwiki.org/wiki/Doom), [Heretic](https://doomwiki.org/wiki/Heretic), [Hexen](https://doomwiki.org/wiki/Hexen), [Strife](https://doomwiki.org/wiki/Strife), and modern source ports like [GZDoom](https://zdoom.org/), [DSDA-Doom](https://github.com/kraflab/dsda-doom), and [Eternity Engine](https://eternity.youfailit.net/).

## Features

- **Multi-format archive reading** - WAD (IWAD/PWAD), PK3 (ZIP), PK7 (7-Zip), and loose folder archives with automatic format detection
- **Full map parsing** - Classic DOOM binary, Hexen extended, and [UDMF](https://doomwiki.org/wiki/UDMF) text formats
- **Composite archives** - Load multiple archives in order with DOOM's resource layering model, conflict detection, and override tracking
- **WAD & PK3 authoring** - Fluent builder API for creating and modifying archives programmatically
- **Structural validation** - Map integrity checks, marker pair validation, cross-reference validation, and resource dependency analysis
- **ZScript & DECORATE parser** - Full Roslyn-style AST parser with semantic analysis, linting, formatting, and C# transpilation
- **Definition parsers** - [DEHACKED](https://doomwiki.org/wiki/DeHackEd), [MAPINFO](https://zdoom.org/wiki/MAPINFO), and [SNDINFO](https://zdoom.org/wiki/SNDINFO) text format parsers
- **Game constants database** - Complete DoomEd number, linedef action, and sector special databases across all supported games
- **Compatibility detection** - Automatic source port requirement analysis (Vanilla through GZDoom)
- **Map rendering** - Generate map thumbnail images from linedef geometry
- **Image export** - Convert DOOM graphics to standard image formats

## Quick Start

```bash
dotnet add package WAD.NET
```

```csharp
using WAD.NET.Archives;

// Auto-detect and open any archive format
using var archive = ArchiveReaderFactory.Open("DOOM2.WAD");

foreach (var entry in archive.GetEntries())
{
    Console.WriteLine($"{entry.Name}: {entry.Size} bytes ({entry.Category})");
}
```

See the [Getting Started](articles/getting-started.md) guide for a full walkthrough.

## Supported Formats

| Format | Read | Write | Description |
|--------|------|-------|-------------|
| WAD (IWAD/PWAD) | Yes | Yes | Classic [DOOM WAD format](https://doomwiki.org/wiki/WAD) |
| PK3 | Yes | Yes | [ZIP-based archives](https://zdoom.org/wiki/Using_ZIPs_as_WAD_replacement) used by ZDoom-family ports |
| PK7 | Yes | No | 7-Zip archives used by some source ports |
| Folder | Yes | No | Loose file directories matching PK3 structure |
| UDMF | Yes | No | [Universal Doom Map Format](https://doomwiki.org/wiki/UDMF) text-based maps |

## Platform Support

WAD.NET targets **.NET Standard 2.1**, which means it runs on:

- .NET 5, 6, 7, 8+
- .NET Core 3.0+
- Mono 6.4+
- Unity (2021.2+)

## License

WAD.NET is released under the [MIT License](https://github.com/daerogami/WAD.NET/blob/dev/LICENSE).

## Links

- [GitHub Repository](https://github.com/daerogami/WAD.NET)
- [NuGet Package](https://www.nuget.org/packages/WAD.NET)
- [DOOM Wiki](https://doomwiki.org/)
- [ZDoom Wiki](https://zdoom.org/wiki/)
