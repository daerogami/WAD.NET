# Phase 5: Modern Source Port Extensions

## Overview

This phase adds support for modern DOOM source port features:
- Boom extended linedefs and generalized actions
- MBF (Marine's Best Friend) extensions
- MBF21 modern standard
- DEHACKED patch support
- MAPINFO definitions
- DECORATE actor detection
- ZScript detection

## Priority: LOW

These features are advanced and primarily useful for mod analysis tools.

---

## Boom Format Extensions

### Extended Linedef Flags (Boom)
```csharp
[Flags]
public enum BoomLinedefFlags : ushort
{
    // Standard DOOM flags (0x0001 - 0x0100)
    Impassable = 0x0001,
    BlockMonsters = 0x0002,
    TwoSided = 0x0004,
    UpperUnpegged = 0x0008,
    LowerUnpegged = 0x0010,
    Secret = 0x0020,
    BlockSound = 0x0040,
    NotOnMap = 0x0080,
    AlreadyOnMap = 0x0100,

    // Boom extensions (0x0200+)
    PassThru = 0x0200,        // ML_PASSUSE - Pass activation through
    Reserved1 = 0x0400,
    Reserved2 = 0x0800,

    // MBF extensions
    TranslucentMidtex = 0x1000,  // MBF: Translucent middle texture
}
```

### Generalized Linedef Types (Boom)

Boom uses special linedef types 0x2F80-0x7FFF for generalized actions.

```csharp
public static class BoomGeneralizedLinedef
{
    // Type ranges
    public const ushort FloorBase = 0x6000;      // 24576
    public const ushort CeilingBase = 0x4000;    // 16384
    public const ushort DoorBase = 0x3C00;       // 15360
    public const ushort LockedDoorBase = 0x3800; // 14336
    public const ushort LiftBase = 0x3400;       // 13312
    public const ushort StairsBase = 0x3000;     // 12288
    public const ushort CrusherBase = 0x2F80;    // 12160

    public static bool IsGeneralized(ushort type)
    {
        return type >= CrusherBase;
    }

    public static GeneralizedAction Parse(ushort type)
    {
        if (type >= FloorBase)
            return ParseFloor(type);
        if (type >= CeilingBase)
            return ParseCeiling(type);
        if (type >= DoorBase)
            return ParseDoor(type);
        if (type >= LockedDoorBase)
            return ParseLockedDoor(type);
        if (type >= LiftBase)
            return ParseLift(type);
        if (type >= StairsBase)
            return ParseStairs(type);
        if (type >= CrusherBase)
            return ParseCrusher(type);

        return null;
    }

    private static GeneralizedAction ParseFloor(ushort type)
    {
        int offset = type - FloorBase;

        return new GeneralizedFloorAction
        {
            Trigger = (TriggerType)(offset & 0x07),
            Speed = (SpeedType)((offset >> 3) & 0x03),
            Model = (ModelType)((offset >> 5) & 0x01),
            Direction = (DirectionType)((offset >> 6) & 0x01),
            Target = (FloorTargetType)((offset >> 7) & 0x07),
            Change = (ChangeType)((offset >> 10) & 0x03),
            Crush = (offset & 0x1000) != 0
        };
    }

    // Similar methods for other types...
}

public class GeneralizedAction
{
    public TriggerType Trigger { get; init; }
    public SpeedType Speed { get; init; }
}

public class GeneralizedFloorAction : GeneralizedAction
{
    public ModelType Model { get; init; }
    public DirectionType Direction { get; init; }
    public FloorTargetType Target { get; init; }
    public ChangeType Change { get; init; }
    public bool Crush { get; init; }
}

public enum TriggerType
{
    WalkOnce,
    WalkRepeatable,
    SwitchOnce,
    SwitchRepeatable,
    GunOnce,
    GunRepeatable,
    PushOnce,
    PushRepeatable
}

public enum SpeedType
{
    Slow,
    Normal,
    Fast,
    Turbo
}
```

---

## Task 5.1: Boom Feature Detection

### Implementation
```csharp
public static class BoomDetector
{
    public static BoomFeatures DetectFeatures(IMap map)
    {
        var features = new BoomFeatures();

        if (map is DoomMap doomMap)
        {
            foreach (var linedef in doomMap.Linedefs)
            {
                // Check for generalized linedefs
                if (BoomGeneralizedLinedef.IsGeneralized(linedef.Special))
                {
                    features.HasGeneralizedLinedefs = true;
                }

                // Check for extended flags
                if (((ushort)linedef.Flags & 0x0200) != 0)
                {
                    features.HasPassThruFlag = true;
                }
            }

            // Check for deep water (sector tag 242)
            foreach (var sector in doomMap.Sectors)
            {
                if (sector.Special == 242)
                {
                    features.HasDeepWater = true;
                }
            }
        }

        return features;
    }
}

public class BoomFeatures
{
    public bool HasGeneralizedLinedefs { get; set; }
    public bool HasPassThruFlag { get; set; }
    public bool HasDeepWater { get; set; }
    public bool HasColormapTransfer { get; set; }
    public bool HasScrollers { get; set; }
    public bool HasTranslucency { get; set; }

    public bool UsesBoomFeatures =>
        HasGeneralizedLinedefs || HasPassThruFlag ||
        HasDeepWater || HasColormapTransfer ||
        HasScrollers || HasTranslucency;
}
```

---

## Task 5.2: MBF21 Extensions

### MBF21 Thing Flags
```csharp
[Flags]
public enum MBF21ThingFlags : uint
{
    // Standard thing flags in upper bits
    LogKill = 0x00010000,         // Increment level kill counter
    FullVolSounds = 0x00020000,   // Full-volume sounds
    IsMonster = 0x00040000,       // Counted as monster for A_Look
    CountKill = 0x00080000,       // Counts toward kill percentage

    // MBF21 additions
    Map07Boss1 = 0x00100000,      // Triggers 666 on death
    Map07Boss2 = 0x00200000,      // Triggers 667 on death
    E1M8Boss = 0x00400000,        // Baron death special
    E2M8Boss = 0x00800000,        // Cyberdemon death special
    E3M8Boss = 0x01000000,        // Spider death special
    E4M6Boss = 0x02000000,        // Cyberdemon death special
    E4M8Boss = 0x04000000,        // Spider death special
    RipSound = 0x08000000,        // Makes ripper sound
    NoBossSpawn = 0x10000000,     // Icon of Sin can't spawn this
}
```

### MBF21 Linedef Flags
```csharp
[Flags]
public enum MBF21LinedefFlags : uint
{
    // Bits 10-15 reserved for activation
    BlockPlayers = 0x00010000,     // Block players only
    BlockFloaters = 0x00020000,    // Block flying monsters
    BlockLandMonsters = 0x00040000 // Block walking monsters
}
```

---

## Task 5.3: DEHACKED Parser

### DEH Format
DEHACKED patches modify game behavior by altering hardcoded values.

```
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

Thing 1 (Zombie)
Hit points = 100
Reaction time = 8
Speed = 12

Frame 10
Sprite number = 3
Sprite subnumber = 0

Text 5 7
E1M1E2M1
```

### Implementation
```csharp
public class DehackedPatch
{
    public int DoomVersion { get; set; }
    public int PatchFormat { get; set; }

    public List<DehThing> Things { get; } = new();
    public List<DehFrame> Frames { get; } = new();
    public List<DehWeapon> Weapons { get; } = new();
    public List<DehAmmo> Ammo { get; } = new();
    public List<DehSound> Sounds { get; } = new();
    public List<DehSprite> Sprites { get; } = new();
    public List<DehText> Texts { get; } = new();
    public List<DehString> Strings { get; } = new();
    public DehMisc Misc { get; set; }
}

public class DehThing
{
    public int Index { get; set; }
    public string Name { get; set; }
    public int? HitPoints { get; set; }
    public int? Speed { get; set; }
    public int? ReactionTime { get; set; }
    public int? PainChance { get; set; }
    public int? Damage { get; set; }
    public int? Mass { get; set; }
    public uint? Flags { get; set; }
    public uint? Flags2 { get; set; }  // MBF
    public uint? MBF21Flags { get; set; }  // MBF21
}

public class DehackedParser
{
    private readonly string[] _lines;
    private int _lineIndex;

    public DehackedParser(string content)
    {
        _lines = content.Split('\n');
    }

    public DehackedPatch Parse()
    {
        var patch = new DehackedPatch();

        while (_lineIndex < _lines.Length)
        {
            var line = _lines[_lineIndex].Trim();

            if (line.StartsWith("Doom version"))
                patch.DoomVersion = ParseIntValue(line);
            else if (line.StartsWith("Patch format"))
                patch.PatchFormat = ParseIntValue(line);
            else if (line.StartsWith("Thing "))
                patch.Things.Add(ParseThing());
            else if (line.StartsWith("Frame "))
                patch.Frames.Add(ParseFrame());
            else if (line.StartsWith("Weapon "))
                patch.Weapons.Add(ParseWeapon());
            else if (line.StartsWith("Text "))
                patch.Texts.Add(ParseText(line));
            else if (line.StartsWith("[CODEPTR]"))
                ParseCodePointers(patch);
            else if (line.StartsWith("[STRINGS]"))
                ParseStrings(patch);

            _lineIndex++;
        }

        return patch;
    }

    private DehThing ParseThing()
    {
        var header = _lines[_lineIndex];
        var match = Regex.Match(header, @"Thing (\d+)(?: \(([^)]+)\))?");

        var thing = new DehThing
        {
            Index = int.Parse(match.Groups[1].Value),
            Name = match.Groups[2].Success ? match.Groups[2].Value : null
        };

        _lineIndex++;

        while (_lineIndex < _lines.Length)
        {
            var line = _lines[_lineIndex].Trim();
            if (string.IsNullOrEmpty(line) || !line.Contains('='))
                break;

            var parts = line.Split('=');
            var key = parts[0].Trim();
            var value = int.Parse(parts[1].Trim());

            switch (key)
            {
                case "Hit points": thing.HitPoints = value; break;
                case "Speed": thing.Speed = value; break;
                case "Reaction time": thing.ReactionTime = value; break;
                case "Pain chance": thing.PainChance = value; break;
                case "Damage": thing.Damage = value; break;
                case "Mass": thing.Mass = value; break;
                case "Bits": thing.Flags = (uint)value; break;
                case "MBF21 Bits": thing.MBF21Flags = (uint)value; break;
            }

            _lineIndex++;
        }

        return thing;
    }

    // Additional parsing methods...
}
```

---

## Task 5.4: MAPINFO Parser

### MAPINFO Format
```
map E1M1 "Hangar"
{
    levelnum = 1
    titlepatch = "WILV00"
    next = "E1M2"
    secretnext = "E1M9"
    sky1 = "SKY1"
    music = "D_E1M1"
    cluster = 1
    par = 30
}

cluster 1
{
    flat = "FLOOR4_8"
    music = "D_READ_M"
    exittext = "Once you beat the big badasses..."
}
```

### Implementation
```csharp
public class MapInfo
{
    public Dictionary<string, MapDefinition> Maps { get; } = new();
    public Dictionary<int, ClusterDefinition> Clusters { get; } = new();
    public Dictionary<int, EpisodeDefinition> Episodes { get; } = new();
    public SkillDefinition[] Skills { get; set; }
}

public class MapDefinition
{
    public string MapLump { get; set; }
    public string NiceName { get; set; }
    public int LevelNum { get; set; }
    public string TitlePatch { get; set; }
    public string Next { get; set; }
    public string SecretNext { get; set; }
    public string Sky1 { get; set; }
    public string Sky2 { get; set; }
    public string Music { get; set; }
    public int Cluster { get; set; }
    public int Par { get; set; }
    public bool NoIntermission { get; set; }
    public bool AllowMonsterTelefrags { get; set; }
}

public class MapInfoParser
{
    private readonly Queue<string> _tokens;

    public MapInfoParser(string content)
    {
        _tokens = Tokenize(content);
    }

    public MapInfo Parse()
    {
        var info = new MapInfo();

        while (_tokens.Count > 0)
        {
            var token = _tokens.Dequeue();

            switch (token.ToLowerInvariant())
            {
                case "map":
                    var mapDef = ParseMapDefinition();
                    info.Maps[mapDef.MapLump] = mapDef;
                    break;
                case "cluster":
                    var clusterDef = ParseClusterDefinition();
                    info.Clusters[clusterDef.Id] = clusterDef;
                    break;
                case "episode":
                    var episodeDef = ParseEpisodeDefinition();
                    info.Episodes[episodeDef.Number] = episodeDef;
                    break;
                case "clearepisodes":
                    info.Episodes.Clear();
                    break;
            }
        }

        return info;
    }

    private MapDefinition ParseMapDefinition()
    {
        var def = new MapDefinition
        {
            MapLump = _tokens.Dequeue()
        };

        // Optional nice name
        if (_tokens.Peek().StartsWith("\""))
        {
            def.NiceName = ParseString();
        }

        Expect("{");

        while (_tokens.Peek() != "}")
        {
            var key = _tokens.Dequeue().ToLowerInvariant();

            switch (key)
            {
                case "levelnum":
                    Expect("=");
                    def.LevelNum = int.Parse(_tokens.Dequeue());
                    break;
                case "next":
                    Expect("=");
                    def.Next = ParseString();
                    break;
                case "secretnext":
                    Expect("=");
                    def.SecretNext = ParseString();
                    break;
                case "sky1":
                    Expect("=");
                    def.Sky1 = ParseString();
                    break;
                case "music":
                    Expect("=");
                    def.Music = ParseString();
                    break;
                case "cluster":
                    Expect("=");
                    def.Cluster = int.Parse(_tokens.Dequeue());
                    break;
                case "par":
                    Expect("=");
                    def.Par = int.Parse(_tokens.Dequeue());
                    break;
                case "nointermission":
                    def.NoIntermission = true;
                    break;
            }
        }

        Expect("}");
        return def;
    }

    // Additional parsing methods...
}
```

---

## Task 5.5: DECORATE Detection

DECORATE is complex; focus on detection and basic metadata extraction.

### Implementation
```csharp
public class DecorateInfo
{
    public List<ActorDefinition> Actors { get; } = new();
}

public class ActorDefinition
{
    public string Name { get; set; }
    public string Parent { get; set; }
    public int? EditorNumber { get; set; }
    public int? SpawnHealth { get; set; }
    public string[] States { get; set; }
}

public class DecorateScanner
{
    /// <summary>
    /// Quick scan to detect DECORATE usage without full parsing
    /// </summary>
    public static DecorateInfo Scan(string content)
    {
        var info = new DecorateInfo();

        // Find actor definitions
        var actorPattern = new Regex(
            @"actor\s+(\w+)(?:\s*:\s*(\w+))?(?:\s+(\d+))?",
            RegexOptions.IgnoreCase | RegexOptions.Multiline);

        foreach (Match match in actorPattern.Matches(content))
        {
            info.Actors.Add(new ActorDefinition
            {
                Name = match.Groups[1].Value,
                Parent = match.Groups[2].Success ? match.Groups[2].Value : null,
                EditorNumber = match.Groups[3].Success
                    ? int.Parse(match.Groups[3].Value)
                    : null
            });
        }

        return info;
    }
}
```

---

## Task 5.6: ZScript Detection

ZScript is even more complex; focus on detection only.

### Implementation
```csharp
public class ZScriptInfo
{
    public List<string> ClassNames { get; } = new();
    public List<string> Includes { get; } = new();
    public string Version { get; set; }
}

public class ZScriptScanner
{
    public static ZScriptInfo Scan(string content)
    {
        var info = new ZScriptInfo();

        // Detect version
        var versionMatch = Regex.Match(content, @"version\s+""([^""]+)""");
        if (versionMatch.Success)
            info.Version = versionMatch.Groups[1].Value;

        // Find class definitions
        var classPattern = new Regex(
            @"class\s+(\w+)(?:\s*:\s*(\w+))?",
            RegexOptions.IgnoreCase);

        foreach (Match match in classPattern.Matches(content))
        {
            info.ClassNames.Add(match.Groups[1].Value);
        }

        // Find includes
        var includePattern = new Regex(@"#include\s+""([^""]+)""");
        foreach (Match match in includePattern.Matches(content))
        {
            info.Includes.Add(match.Groups[1].Value);
        }

        return info;
    }
}
```

---

## Task 5.7: Mod Compatibility Analyzer

### Implementation
```csharp
public enum SourcePort
{
    Vanilla,       // Original DOOM
    Boom,          // Boom-compatible
    MBF,           // MBF features
    MBF21,         // MBF21 standard
    ZDoom,         // ZDoom family
    GZDoom,        // GZDoom (OpenGL)
    Eternity,      // Eternity Engine
    Unknown
}

public class ModCompatibility
{
    public SourcePort MinimumPort { get; set; }
    public List<string> RequiredFeatures { get; } = new();
    public List<string> Warnings { get; } = new();
}

public class CompatibilityAnalyzer
{
    public ModCompatibility Analyze(IArchiveReader archive)
    {
        var result = new ModCompatibility
        {
            MinimumPort = SourcePort.Vanilla
        };

        // Check for ZScript
        if (archive.Contains("ZSCRIPT"))
        {
            result.MinimumPort = SourcePort.GZDoom;
            result.RequiredFeatures.Add("ZScript");
        }

        // Check for DECORATE
        if (archive.Contains("DECORATE"))
        {
            UpgradeMinimum(result, SourcePort.ZDoom);
            result.RequiredFeatures.Add("DECORATE");
        }

        // Check for MAPINFO
        if (archive.Contains("MAPINFO") || archive.Contains("ZMAPINFO"))
        {
            UpgradeMinimum(result, SourcePort.ZDoom);
            result.RequiredFeatures.Add("MAPINFO");
        }

        // Check for UMAPINFO (Boom/MBF21)
        if (archive.Contains("UMAPINFO"))
        {
            UpgradeMinimum(result, SourcePort.MBF21);
            result.RequiredFeatures.Add("UMAPINFO");
        }

        // Check for DEHACKED
        if (archive.Contains("DEHACKED"))
        {
            result.RequiredFeatures.Add("DEHACKED");
            // Analyze DEH content for MBF21 features
            var dehEntry = archive.GetEntry("DEHACKED");
            var dehContent = Encoding.ASCII.GetString(archive.ReadLump(dehEntry));

            if (dehContent.Contains("MBF21"))
            {
                UpgradeMinimum(result, SourcePort.MBF21);
            }
            else if (dehContent.Contains("[CODEPTR]"))
            {
                UpgradeMinimum(result, SourcePort.MBF);
            }
            else
            {
                UpgradeMinimum(result, SourcePort.Boom);
            }
        }

        // Check maps for Boom features
        foreach (var entry in archive.GetEntries()
            .Where(e => e.Category == LumpCategory.Map))
        {
            var mapData = archive.ReadLump(entry);
            // Analyze for generalized linedefs, etc.
        }

        return result;
    }

    private void UpgradeMinimum(ModCompatibility result, SourcePort port)
    {
        if (port > result.MinimumPort)
            result.MinimumPort = port;
    }
}
```

---

## Task 5.8: SNDINFO Parser

### SNDINFO Format
```
// Sound definitions
$pitchshiftrange 4

pistol          DSPISTOL
shotgn          DSSHOTGN
weapons/sshotf  DSDBOPN

$random grunt/death { grunt/death1 grunt/death2 grunt/death3 }
$alias menu/choose  weapons/pistol
$limit weapons/pistol 4
```

### Implementation
```csharp
public class SndInfo
{
    public Dictionary<string, string> Sounds { get; } = new();
    public Dictionary<string, string[]> RandomSounds { get; } = new();
    public Dictionary<string, string> Aliases { get; } = new();
    public Dictionary<string, int> Limits { get; } = new();
    public int PitchShiftRange { get; set; } = 0;
}

public class SndInfoParser
{
    public SndInfo Parse(string content)
    {
        var info = new SndInfo();
        var lines = content.Split('\n');

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();

            // Skip comments
            if (line.StartsWith("//") || string.IsNullOrEmpty(line))
                continue;

            // Remove inline comments
            var commentIndex = line.IndexOf("//");
            if (commentIndex >= 0)
                line = line[..commentIndex].Trim();

            if (line.StartsWith("$"))
            {
                ParseDirective(line, info);
            }
            else
            {
                // Sound definition: logical_name lump_name
                var parts = line.Split(new[] { ' ', '\t' },
                    StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length >= 2)
                {
                    info.Sounds[parts[0]] = parts[1];
                }
            }
        }

        return info;
    }

    private void ParseDirective(string line, SndInfo info)
    {
        if (line.StartsWith("$random"))
        {
            // $random name { sound1 sound2 ... }
            var match = Regex.Match(line, @"\$random\s+(\S+)\s*\{([^}]+)\}");
            if (match.Success)
            {
                var name = match.Groups[1].Value;
                var sounds = match.Groups[2].Value
                    .Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                info.RandomSounds[name] = sounds;
            }
        }
        else if (line.StartsWith("$alias"))
        {
            var parts = line.Split(new[] { ' ', '\t' },
                StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3)
            {
                info.Aliases[parts[1]] = parts[2];
            }
        }
        else if (line.StartsWith("$limit"))
        {
            var parts = line.Split(new[] { ' ', '\t' },
                StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3)
            {
                info.Limits[parts[1]] = int.Parse(parts[2]);
            }
        }
        else if (line.StartsWith("$pitchshiftrange"))
        {
            var parts = line.Split(new[] { ' ', '\t' },
                StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                info.PitchShiftRange = int.Parse(parts[1]);
            }
        }
    }
}
```

---

## Acceptance Criteria

1. Boom generalized linedef types are correctly decoded
2. MBF21 extended flags are recognized
3. DEHACKED patches parse correctly
4. MAPINFO definitions are extracted
5. DECORATE/ZScript presence is detected
6. Mod compatibility is accurately determined
7. SNDINFO sound definitions are parsed

---

## Test Cases

```csharp
[Fact]
public void ShouldDecodeGeneralizedFloor()
{
    ushort type = 0x6048;  // Generalized floor
    var action = BoomGeneralizedLinedef.Parse(type);

    Assert.IsType<GeneralizedFloorAction>(action);
    var floor = (GeneralizedFloorAction)action;
    Assert.Equal(TriggerType.WalkOnce, floor.Trigger);
}

[Fact]
public void ShouldDetectBoomFeatures()
{
    var map = CreateMapWithGeneralizedLinedefs();
    var features = BoomDetector.DetectFeatures(map);

    Assert.True(features.HasGeneralizedLinedefs);
    Assert.True(features.UsesBoomFeatures);
}

[Fact]
public void ShouldParseDehacked()
{
    var deh = @"
Patch File for DeHackEd v3.0
Doom version = 19

Thing 1 (Zombie)
Hit points = 100
";

    var parser = new DehackedParser(deh);
    var patch = parser.Parse();

    Assert.Single(patch.Things);
    Assert.Equal(100, patch.Things[0].HitPoints);
}

[Fact]
public void ShouldDetermineMinimumPort()
{
    using var archive = new Pk3Reader("zscript_mod.pk3");
    var analyzer = new CompatibilityAnalyzer();
    var compat = analyzer.Analyze(archive);

    Assert.Equal(SourcePort.GZDoom, compat.MinimumPort);
    Assert.Contains("ZScript", compat.RequiredFeatures);
}
```
