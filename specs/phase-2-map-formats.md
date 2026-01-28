# Phase 2: Map Format Support

## Overview

This phase implements parsing for DOOM map data in three formats:
1. **Doom Format** - Original binary format (DOOM, DOOM II, Heretic)
2. **Hexen Format** - Extended binary format with ACS support
3. **UDMF** - Universal Doom Map Format (text-based)

## Priority: HIGH

Map parsing is essential for any WAD tool, editor, or analyzer.

---

## Map Detection

Maps are identified by marker lumps followed by data lumps:

### DOOM/DOOM II Map Names
- DOOM: `E1M1` through `E4M9` (Episode + Map)
- DOOM II: `MAP01` through `MAP32`

### Required Map Lumps (in order)
| Lump | Description | Doom | Hexen | UDMF |
|------|-------------|------|-------|------|
| `THINGS` | Entity placements | Binary | Binary | - |
| `LINEDEFS` | Line definitions | Binary | Binary | - |
| `SIDEDEFS` | Side definitions | Binary | Binary | - |
| `VERTEXES` | Vertex coordinates | Binary | Binary | - |
| `SEGS` | BSP segments | Binary | Binary | - |
| `SSECTORS` | Subsectors | Binary | Binary | - |
| `NODES` | BSP tree | Binary | Binary | - |
| `SECTORS` | Sector definitions | Binary | Binary | - |
| `REJECT` | Reject table | Binary | Binary | - |
| `BLOCKMAP` | Collision grid | Binary | Binary | - |
| `BEHAVIOR` | ACS bytecode | - | Binary | Optional |
| `TEXTMAP` | UDMF text data | - | - | Text |
| `ENDMAP` | UDMF terminator | - | - | Marker |

---

## Task 2.1: Implement Map Marker Detection

### Implementation
```csharp
public enum MapFormat
{
    Unknown,
    Doom,
    Hexen,
    UDMF
}

public static class MapDetector
{
    private static readonly Regex DoomMapPattern = new(@"^E[1-4]M[1-9]$");
    private static readonly Regex Doom2MapPattern = new(@"^MAP\d{2}$");

    public static bool IsMapMarker(string lumpName)
    {
        return DoomMapPattern.IsMatch(lumpName) ||
               Doom2MapPattern.IsMatch(lumpName);
    }

    public static MapFormat DetectFormat(IEnumerable<string> lumpNames)
    {
        var names = lumpNames.ToHashSet();

        if (names.Contains("TEXTMAP"))
            return MapFormat.UDMF;

        if (names.Contains("BEHAVIOR"))
            return MapFormat.Hexen;

        if (names.Contains("THINGS") && names.Contains("LINEDEFS"))
            return MapFormat.Doom;

        return MapFormat.Unknown;
    }
}
```

---

## Task 2.2: Doom Format - THINGS Lump

### Binary Format (10 bytes per thing)
| Offset | Size | Type | Description |
|--------|------|------|-------------|
| 0 | 2 | int16 | X position |
| 2 | 2 | int16 | Y position |
| 4 | 2 | uint16 | Angle (0-359) |
| 6 | 2 | uint16 | Type (DoomEd number) |
| 8 | 2 | uint16 | Spawn flags |

### Spawn Flags
```csharp
[Flags]
public enum ThingFlags : ushort
{
    None = 0,
    SkillEasy = 0x0001,      // Appears on skills 1 & 2
    SkillMedium = 0x0002,    // Appears on skill 3
    SkillHard = 0x0004,      // Appears on skills 4 & 5
    Ambush = 0x0008,         // Deaf/ambush flag
    Multiplayer = 0x0010,    // Only in multiplayer
}
```

### Data Structure
```csharp
public readonly struct DoomThing
{
    public short X { get; init; }
    public short Y { get; init; }
    public ushort Angle { get; init; }
    public ushort Type { get; init; }
    public ThingFlags Flags { get; init; }

    public static DoomThing Parse(ReadOnlySpan<byte> data)
    {
        return new DoomThing
        {
            X = BinaryPrimitives.ReadInt16LittleEndian(data),
            Y = BinaryPrimitives.ReadInt16LittleEndian(data.Slice(2)),
            Angle = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(4)),
            Type = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(6)),
            Flags = (ThingFlags)BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(8))
        };
    }
}

public sealed class ThingsLump : Lump, IMapLump
{
    public const int EntrySize = 10;
    public DoomThing[] Things { get; }

    public ThingsLump(string name, string source, ReadOnlySpan<byte> data)
        : base(name, source)
    {
        int count = data.Length / EntrySize;
        Things = new DoomThing[count];

        for (int i = 0; i < count; i++)
        {
            Things[i] = DoomThing.Parse(data.Slice(i * EntrySize, EntrySize));
        }
    }
}
```

---

## Task 2.3: Doom Format - VERTEXES Lump

### Binary Format (4 bytes per vertex)
| Offset | Size | Type | Description |
|--------|------|------|-------------|
| 0 | 2 | int16 | X coordinate |
| 2 | 2 | int16 | Y coordinate |

### Data Structure
```csharp
public readonly struct Vertex
{
    public short X { get; init; }
    public short Y { get; init; }

    public static Vertex Parse(ReadOnlySpan<byte> data)
    {
        return new Vertex
        {
            X = BinaryPrimitives.ReadInt16LittleEndian(data),
            Y = BinaryPrimitives.ReadInt16LittleEndian(data.Slice(2))
        };
    }
}

public sealed class VertexesLump : Lump, IMapLump
{
    public const int EntrySize = 4;
    public Vertex[] Vertices { get; }

    public VertexesLump(string name, string source, ReadOnlySpan<byte> data)
        : base(name, source)
    {
        int count = data.Length / EntrySize;
        Vertices = new Vertex[count];

        for (int i = 0; i < count; i++)
        {
            Vertices[i] = Vertex.Parse(data.Slice(i * EntrySize, EntrySize));
        }
    }
}
```

---

## Task 2.4: Doom Format - LINEDEFS Lump

### Binary Format (14 bytes per linedef)
| Offset | Size | Type | Description |
|--------|------|------|-------------|
| 0 | 2 | uint16 | Start vertex index |
| 2 | 2 | uint16 | End vertex index |
| 4 | 2 | uint16 | Flags |
| 6 | 2 | uint16 | Special type |
| 8 | 2 | uint16 | Sector tag |
| 10 | 2 | uint16 | Front sidedef index (0xFFFF = none) |
| 12 | 2 | uint16 | Back sidedef index (0xFFFF = none) |

### Linedef Flags
```csharp
[Flags]
public enum LinedefFlags : ushort
{
    None = 0,
    Impassable = 0x0001,        // Blocks players and monsters
    BlockMonsters = 0x0002,     // Blocks monsters only
    TwoSided = 0x0004,          // Has back side
    UpperUnpegged = 0x0008,     // Upper texture unpegged
    LowerUnpegged = 0x0010,     // Lower texture unpegged
    Secret = 0x0020,            // Shown as 1-sided on automap
    BlockSound = 0x0040,        // Blocks sound propagation
    NotOnMap = 0x0080,          // Not shown on automap
    AlreadyOnMap = 0x0100,      // Starts on automap
}
```

### Data Structure
```csharp
public readonly struct DoomLinedef
{
    public ushort StartVertex { get; init; }
    public ushort EndVertex { get; init; }
    public LinedefFlags Flags { get; init; }
    public ushort Special { get; init; }
    public ushort Tag { get; init; }
    public ushort FrontSidedef { get; init; }
    public ushort BackSidedef { get; init; }

    public bool HasBackSide => BackSidedef != 0xFFFF;

    public static DoomLinedef Parse(ReadOnlySpan<byte> data)
    {
        return new DoomLinedef
        {
            StartVertex = BinaryPrimitives.ReadUInt16LittleEndian(data),
            EndVertex = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(2)),
            Flags = (LinedefFlags)BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(4)),
            Special = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(6)),
            Tag = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(8)),
            FrontSidedef = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(10)),
            BackSidedef = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(12))
        };
    }
}
```

---

## Task 2.5: Doom Format - SIDEDEFS Lump

### Binary Format (30 bytes per sidedef)
| Offset | Size | Type | Description |
|--------|------|------|-------------|
| 0 | 2 | int16 | X texture offset |
| 2 | 2 | int16 | Y texture offset |
| 4 | 8 | char[8] | Upper texture name |
| 12 | 8 | char[8] | Lower texture name |
| 20 | 8 | char[8] | Middle texture name |
| 28 | 2 | uint16 | Sector index |

### Data Structure
```csharp
public readonly struct DoomSidedef
{
    public short XOffset { get; init; }
    public short YOffset { get; init; }
    public string UpperTexture { get; init; }
    public string LowerTexture { get; init; }
    public string MiddleTexture { get; init; }
    public ushort Sector { get; init; }

    public static DoomSidedef Parse(ReadOnlySpan<byte> data)
    {
        return new DoomSidedef
        {
            XOffset = BinaryPrimitives.ReadInt16LittleEndian(data),
            YOffset = BinaryPrimitives.ReadInt16LittleEndian(data.Slice(2)),
            UpperTexture = ReadTextureName(data.Slice(4, 8)),
            LowerTexture = ReadTextureName(data.Slice(12, 8)),
            MiddleTexture = ReadTextureName(data.Slice(20, 8)),
            Sector = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(28))
        };
    }

    private static string ReadTextureName(ReadOnlySpan<byte> data)
    {
        int length = data.IndexOf((byte)0);
        if (length < 0) length = 8;
        var name = Encoding.ASCII.GetString(data.Slice(0, length));
        return name == "-" ? string.Empty : name;
    }
}
```

---

## Task 2.6: Doom Format - SECTORS Lump

### Binary Format (26 bytes per sector)
| Offset | Size | Type | Description |
|--------|------|------|-------------|
| 0 | 2 | int16 | Floor height |
| 2 | 2 | int16 | Ceiling height |
| 4 | 8 | char[8] | Floor texture |
| 12 | 8 | char[8] | Ceiling texture |
| 20 | 2 | uint16 | Light level (0-255) |
| 22 | 2 | uint16 | Special type |
| 24 | 2 | uint16 | Tag |

### Data Structure
```csharp
public readonly struct DoomSector
{
    public short FloorHeight { get; init; }
    public short CeilingHeight { get; init; }
    public string FloorTexture { get; init; }
    public string CeilingTexture { get; init; }
    public ushort LightLevel { get; init; }
    public ushort Special { get; init; }
    public ushort Tag { get; init; }

    public static DoomSector Parse(ReadOnlySpan<byte> data)
    {
        return new DoomSector
        {
            FloorHeight = BinaryPrimitives.ReadInt16LittleEndian(data),
            CeilingHeight = BinaryPrimitives.ReadInt16LittleEndian(data.Slice(2)),
            FloorTexture = ReadTextureName(data.Slice(4, 8)),
            CeilingTexture = ReadTextureName(data.Slice(12, 8)),
            LightLevel = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(20)),
            Special = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(22)),
            Tag = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(24))
        };
    }
}
```

---

## Task 2.7: Hexen Format Extensions

### Hexen THINGS (20 bytes per thing)
| Offset | Size | Type | Description |
|--------|------|------|-------------|
| 0 | 2 | uint16 | Thing ID (TID) |
| 2 | 2 | int16 | X position |
| 4 | 2 | int16 | Y position |
| 6 | 2 | int16 | Z position |
| 8 | 2 | uint16 | Angle |
| 10 | 2 | uint16 | Type |
| 12 | 2 | uint16 | Flags |
| 14 | 1 | uint8 | Special |
| 15 | 1 | uint8 | Arg 0 |
| 16 | 1 | uint8 | Arg 1 |
| 17 | 1 | uint8 | Arg 2 |
| 18 | 1 | uint8 | Arg 3 |
| 19 | 1 | uint8 | Arg 4 |

### Hexen LINEDEFS (16 bytes per linedef)
| Offset | Size | Type | Description |
|--------|------|------|-------------|
| 0 | 2 | uint16 | Start vertex |
| 2 | 2 | uint16 | End vertex |
| 4 | 2 | uint16 | Flags |
| 6 | 1 | uint8 | Special |
| 7 | 1 | uint8 | Arg 0 |
| 8 | 1 | uint8 | Arg 1 |
| 9 | 1 | uint8 | Arg 2 |
| 10 | 1 | uint8 | Arg 3 |
| 11 | 1 | uint8 | Arg 4 |
| 12 | 2 | uint16 | Front sidedef |
| 14 | 2 | uint16 | Back sidedef |

### Data Structures
```csharp
public readonly struct HexenThing
{
    public ushort TID { get; init; }
    public short X { get; init; }
    public short Y { get; init; }
    public short Z { get; init; }
    public ushort Angle { get; init; }
    public ushort Type { get; init; }
    public ushort Flags { get; init; }
    public byte Special { get; init; }
    public byte[] Args { get; init; }  // 5 args
}

public readonly struct HexenLinedef
{
    public ushort StartVertex { get; init; }
    public ushort EndVertex { get; init; }
    public ushort Flags { get; init; }
    public byte Special { get; init; }
    public byte[] Args { get; init; }  // 5 args
    public ushort FrontSidedef { get; init; }
    public ushort BackSidedef { get; init; }
}
```

---

## Task 2.8: UDMF Parser

### UDMF Grammar
```
translation_unit := global_expr_list
global_expr_list := global_expr global_expr_list | ε
global_expr := block | assignment_expr
block := identifier '{' expr_list '}'
expr_list := assignment_expr expr_list | ε
assignment_expr := identifier '=' value ';'
value := integer | float | quoted_string | keyword
```

### Implementation
```csharp
public enum UdmfTokenType
{
    Identifier,
    Integer,
    Float,
    String,
    Equals,
    Semicolon,
    OpenBrace,
    CloseBrace,
    EndOfFile
}

public readonly struct UdmfToken
{
    public UdmfTokenType Type { get; init; }
    public string Value { get; init; }
    public int Line { get; init; }
    public int Column { get; init; }
}

public class UdmfLexer
{
    private readonly string _text;
    private int _position;
    private int _line = 1;
    private int _column = 1;

    public UdmfLexer(string text) => _text = text;

    public IEnumerable<UdmfToken> Tokenize()
    {
        while (_position < _text.Length)
        {
            SkipWhitespaceAndComments();
            if (_position >= _text.Length)
                break;

            yield return ReadToken();
        }

        yield return new UdmfToken { Type = UdmfTokenType.EndOfFile };
    }

    private void SkipWhitespaceAndComments()
    {
        while (_position < _text.Length)
        {
            if (char.IsWhiteSpace(_text[_position]))
            {
                if (_text[_position] == '\n')
                {
                    _line++;
                    _column = 1;
                }
                else
                {
                    _column++;
                }
                _position++;
            }
            else if (_position + 1 < _text.Length &&
                     _text[_position] == '/' && _text[_position + 1] == '/')
            {
                // Single-line comment
                while (_position < _text.Length && _text[_position] != '\n')
                    _position++;
            }
            else if (_position + 1 < _text.Length &&
                     _text[_position] == '/' && _text[_position + 1] == '*')
            {
                // Multi-line comment
                _position += 2;
                while (_position + 1 < _text.Length &&
                       !(_text[_position] == '*' && _text[_position + 1] == '/'))
                {
                    if (_text[_position] == '\n')
                    {
                        _line++;
                        _column = 1;
                    }
                    _position++;
                }
                _position += 2;
            }
            else
            {
                break;
            }
        }
    }

    private UdmfToken ReadToken()
    {
        char c = _text[_position];

        if (c == '{') return SingleCharToken(UdmfTokenType.OpenBrace);
        if (c == '}') return SingleCharToken(UdmfTokenType.CloseBrace);
        if (c == '=') return SingleCharToken(UdmfTokenType.Equals);
        if (c == ';') return SingleCharToken(UdmfTokenType.Semicolon);
        if (c == '"') return ReadString();
        if (char.IsDigit(c) || c == '-' || c == '+') return ReadNumber();
        if (char.IsLetter(c) || c == '_') return ReadIdentifier();

        throw new FormatException($"Unexpected character '{c}' at line {_line}");
    }

    // Additional helper methods...
}

public class UdmfParser
{
    private readonly Queue<UdmfToken> _tokens;

    public UdmfMap Parse(string text)
    {
        var lexer = new UdmfLexer(text);
        _tokens = new Queue<UdmfToken>(lexer.Tokenize());

        var map = new UdmfMap();

        while (Peek().Type != UdmfTokenType.EndOfFile)
        {
            var identifier = Expect(UdmfTokenType.Identifier);

            if (Peek().Type == UdmfTokenType.OpenBrace)
            {
                ParseBlock(map, identifier.Value);
            }
            else
            {
                ParseAssignment(map, identifier.Value);
            }
        }

        return map;
    }

    private void ParseBlock(UdmfMap map, string blockType)
    {
        Expect(UdmfTokenType.OpenBrace);

        var properties = new Dictionary<string, object>();

        while (Peek().Type != UdmfTokenType.CloseBrace)
        {
            var key = Expect(UdmfTokenType.Identifier);
            Expect(UdmfTokenType.Equals);
            var value = ReadValue();
            Expect(UdmfTokenType.Semicolon);
            properties[key.Value.ToLowerInvariant()] = value;
        }

        Expect(UdmfTokenType.CloseBrace);

        switch (blockType.ToLowerInvariant())
        {
            case "vertex":
                map.Vertices.Add(CreateVertex(properties));
                break;
            case "linedef":
                map.Linedefs.Add(CreateLinedef(properties));
                break;
            case "sidedef":
                map.Sidedefs.Add(CreateSidedef(properties));
                break;
            case "sector":
                map.Sectors.Add(CreateSector(properties));
                break;
            case "thing":
                map.Things.Add(CreateThing(properties));
                break;
        }
    }
}
```

### UDMF Map Data Structure
```csharp
public class UdmfMap
{
    public string Namespace { get; set; } = "doom";
    public List<UdmfVertex> Vertices { get; } = new();
    public List<UdmfLinedef> Linedefs { get; } = new();
    public List<UdmfSidedef> Sidedefs { get; } = new();
    public List<UdmfSector> Sectors { get; } = new();
    public List<UdmfThing> Things { get; } = new();
}

public class UdmfVertex
{
    public double X { get; set; }
    public double Y { get; set; }
    public double? ZFloor { get; set; }    // ZDoom extension
    public double? ZCeiling { get; set; }  // ZDoom extension
}

public class UdmfLinedef
{
    public int V1 { get; set; }
    public int V2 { get; set; }
    public bool Blocking { get; set; }
    public bool BlockMonsters { get; set; }
    public bool TwoSided { get; set; }
    public bool DontPegTop { get; set; }
    public bool DontPegBottom { get; set; }
    public bool Secret { get; set; }
    public bool BlockSound { get; set; }
    public bool DontDraw { get; set; }
    public bool Mapped { get; set; }
    public int Special { get; set; }
    public int Arg0 { get; set; }
    public int Arg1 { get; set; }
    public int Arg2 { get; set; }
    public int Arg3 { get; set; }
    public int Arg4 { get; set; }
    public int SideFront { get; set; } = -1;
    public int SideBack { get; set; } = -1;
    public string Comment { get; set; }
}

public class UdmfSidedef
{
    public int Sector { get; set; }
    public double OffsetX { get; set; }
    public double OffsetY { get; set; }
    public string TextureTop { get; set; } = "-";
    public string TextureBottom { get; set; } = "-";
    public string TextureMiddle { get; set; } = "-";
}

public class UdmfSector
{
    public int HeightFloor { get; set; }
    public int HeightCeiling { get; set; }
    public string TextureFloor { get; set; }
    public string TextureCeiling { get; set; }
    public int LightLevel { get; set; } = 160;
    public int Special { get; set; }
    public int Id { get; set; }
}

public class UdmfThing
{
    public int Id { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Height { get; set; }
    public int Angle { get; set; }
    public int Type { get; set; }
    public bool Skill1 { get; set; } = true;
    public bool Skill2 { get; set; } = true;
    public bool Skill3 { get; set; } = true;
    public bool Skill4 { get; set; } = true;
    public bool Skill5 { get; set; } = true;
    public bool Ambush { get; set; }
    public bool Single { get; set; } = true;
    public bool Dm { get; set; } = true;
    public bool Coop { get; set; } = true;
    public int Special { get; set; }
    public int Arg0 { get; set; }
    public int Arg1 { get; set; }
    public int Arg2 { get; set; }
    public int Arg3 { get; set; }
    public int Arg4 { get; set; }
}
```

---

## Task 2.9: BSP Data Lumps

### SEGS Lump (12 bytes per seg)
| Offset | Size | Description |
|--------|------|-------------|
| 0 | 2 | Start vertex |
| 2 | 2 | End vertex |
| 4 | 2 | Angle (BAM) |
| 6 | 2 | Linedef index |
| 8 | 2 | Direction (0=front, 1=back) |
| 10 | 2 | Offset along linedef |

### SSECTORS Lump (4 bytes per subsector)
| Offset | Size | Description |
|--------|------|-------------|
| 0 | 2 | Seg count |
| 2 | 2 | First seg index |

### NODES Lump (28 bytes per node)
| Offset | Size | Description |
|--------|------|-------------|
| 0 | 2 | Partition X |
| 2 | 2 | Partition Y |
| 4 | 2 | Partition dX |
| 6 | 2 | Partition dY |
| 8 | 8 | Right bounding box |
| 16 | 8 | Left bounding box |
| 24 | 2 | Right child |
| 26 | 2 | Left child |

Note: Child values with bit 15 set indicate subsector index.

---

## Task 2.10: Map Container Class

```csharp
public interface IMap
{
    string Name { get; }
    MapFormat Format { get; }
}

public class DoomMap : IMap
{
    public string Name { get; init; }
    public MapFormat Format => MapFormat.Doom;

    public DoomThing[] Things { get; init; }
    public DoomLinedef[] Linedefs { get; init; }
    public DoomSidedef[] Sidedefs { get; init; }
    public Vertex[] Vertices { get; init; }
    public DoomSector[] Sectors { get; init; }
    public Seg[] Segs { get; init; }
    public Subsector[] Subsectors { get; init; }
    public Node[] Nodes { get; init; }
    public byte[] RejectTable { get; init; }
    public short[] Blockmap { get; init; }
}

public class HexenMap : IMap
{
    public string Name { get; init; }
    public MapFormat Format => MapFormat.Hexen;
    public byte[] BehaviorLump { get; init; }  // ACS bytecode
    // Similar structure with Hexen-specific types
}

public class UdmfMapWrapper : IMap
{
    public string Name { get; init; }
    public MapFormat Format => MapFormat.UDMF;
    public UdmfMap Map { get; init; }
}
```

---

## Acceptance Criteria

1. Doom format maps parse all 10 standard lumps correctly
2. Hexen format maps parse extended thing/linedef format
3. UDMF parser handles all standard namespaces (doom, heretic, hexen, strife)
4. Map format auto-detection works correctly
5. Invalid map data throws descriptive exceptions
6. All struct fields are publicly accessible
7. Unit tests cover edge cases (empty maps, maximum values)

---

## Test Cases

```csharp
[Fact]
public void ShouldParseThingsLump()
{
    byte[] data = { /* E1M1 first few things */ };
    var lump = new ThingsLump("THINGS", "DOOM.WAD", data);

    Assert.Equal(138, lump.Things.Length);  // E1M1 thing count
    Assert.Equal(1056, lump.Things[0].X);   // Player 1 start X
}

[Fact]
public void ShouldDetectUdmfFormat()
{
    var lumps = new[] { "MAP01", "TEXTMAP", "ENDMAP" };
    var format = MapDetector.DetectFormat(lumps);
    Assert.Equal(MapFormat.UDMF, format);
}

[Fact]
public void ShouldParseUdmfText()
{
    string udmf = @"
        namespace = ""doom"";
        vertex { x = 0; y = 0; }
        vertex { x = 64; y = 0; }
    ";

    var parser = new UdmfParser();
    var map = parser.Parse(udmf);

    Assert.Equal(2, map.Vertices.Count);
}
```
