# WAD.NET

A .NET Standard 2.1 library for reading and analyzing DOOM WAD files, PK3 archives, and related formats.

## Installation

```bash
dotnet add package WAD.NET
```

## Supported Formats

- **WAD files** - IWAD and PWAD archives (DOOM, Heretic, Hexen, Strife)
- **PK3/PK7** - ZIP-based archives used by modern source ports
- **UDMF** - Universal Doom Map Format

## Usage

```csharp
using WAD.NET;

// Load a WAD file
var wad = new Wad("DOOM2.WAD");

// Access lumps
foreach (var lump in wad.Lumps)
{
    Console.WriteLine($"{lump.Name}: {lump.Size} bytes");
}
```

## License

MIT
