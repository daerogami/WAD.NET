# Phase 9: Game Constants and Definitions Database

## Overview

This phase replaces hardcoded magic numbers and strings scattered throughout WAD.NET with a centralized, spec-compliant definitions database. All game constants should reference official specifications and be accessible through strongly-typed enumerations and lookup tables.

### Current Problems

Magic numbers and strings appear throughout the codebase:
- `WadValidator.cs`: Player start thing types `{1, 2, 3, 4, 11}`
- `DoomSector.cs`: Damage sectors `4, 5, 7, 11, 16`, secret sector `9`
- `DoomSidedef.cs`: Null texture marker `"-"`
- `MapDetector.cs`: Character arithmetic for map numbers
- `StatesSpriteExtractor.cs`: Special sprites `TNT1`, `----`
- `BlockmapLump.cs`: Terminator `0xFFFF`, block size `128`

### Reference Specifications

- **DOOM Specs**: https://www.gamers.org/dhs/helpdocs/dmsp1666.html
- **Boom Specs**: https://doomwiki.org/wiki/Boom
- **MBF21 Specs**: https://doomwiki.org/wiki/MBF21
- **ZDoom Wiki**: https://zdoom.org/wiki/

## Priority: HIGH

These constants are foundational - other parsers and the semantic model from Phase 8 depend on accurate game data.

---

## Task 9.1: Thing Type Database

### DoomEd Numbers

Reference: dmsp1666.html Section 4.2.1

```csharp
namespace WAD.NET.Definitions;

/// <summary>
/// Database of thing types (DoomEd numbers) for all supported games.
/// </summary>
public static class ThingDatabase
{
    private static readonly Dictionary<int, ThingDefinition> _things = new();
    private static readonly Dictionary<string, int> _nameToId = new(StringComparer.OrdinalIgnoreCase);

    static ThingDatabase()
    {
        RegisterDoomThings();
        RegisterDoom2Things();
        RegisterBoomThings();
        RegisterMBF21Things();
    }

    public static ThingDefinition? Get(int doomEdNum)
    {
        return _things.TryGetValue(doomEdNum, out var def) ? def : null;
    }

    public static ThingDefinition? GetByName(string name)
    {
        return _nameToId.TryGetValue(name, out var id) ? Get(id) : null;
    }

    public static IEnumerable<ThingDefinition> GetByCategory(ThingCategory category)
    {
        return _things.Values.Where(t => t.Category == category);
    }

    public static IEnumerable<ThingDefinition> GetMonsters()
    {
        return GetByCategory(ThingCategory.Monster);
    }

    public static IEnumerable<ThingDefinition> GetWeapons()
    {
        return GetByCategory(ThingCategory.Weapon);
    }

    private static void Register(ThingDefinition def)
    {
        _things[def.DoomEdNum] = def;
        _nameToId[def.ClassName] = def.DoomEdNum;
        if (def.LegacyName != null)
            _nameToId[def.LegacyName] = def.DoomEdNum;
    }
}

public class ThingDefinition
{
    public int DoomEdNum { get; init; }
    public string ClassName { get; init; } = "";
    public string? LegacyName { get; init; }  // DECORATE name if different
    public string DisplayName { get; init; } = "";
    public ThingCategory Category { get; init; }
    public ThingFlags Flags { get; init; }
    public GameType Game { get; init; }

    // Default properties (can be overridden by mods)
    public int? Health { get; init; }
    public int? Speed { get; init; }
    public int? Radius { get; init; }
    public int? Height { get; init; }
    public int? Damage { get; init; }
    public int? Mass { get; init; }
    public int? ReactionTime { get; init; }
    public int? PainChance { get; init; }

    // Sprite info
    public string? SpawnSprite { get; init; }
    public char SpawnFrame { get; init; } = 'A';
}

public enum ThingCategory
{
    Player,
    Monster,
    Weapon,
    Ammo,
    Health,
    Armor,
    Powerup,
    Key,
    Obstacle,
    Decoration,
    Gore,
    TechItem,
    Light,
    Teleport,
    Sound,
    Boss,
    Other
}

[Flags]
public enum ThingFlags : uint
{
    None = 0,

    // Spawn flags
    Easy = 1 << 0,
    Medium = 1 << 1,
    Hard = 1 << 2,
    Ambush = 1 << 3,
    Multiplayer = 1 << 4,

    // Thing behavior
    Solid = 1 << 8,
    Shootable = 1 << 9,
    NoSector = 1 << 10,
    NoBlockmap = 1 << 11,
    SpawnCeiling = 1 << 12,
    NoGravity = 1 << 13,
    Pickup = 1 << 14,
    Clip = 1 << 15,

    // Monster flags
    IsMonster = 1 << 16,
    Countkill = 1 << 17,
    CountItem = 1 << 18,
    Float = 1 << 19,

    // Boom/MBF extensions
    Friend = 1 << 24,
    Translucent = 1 << 25,
}

[Flags]
public enum GameType
{
    None = 0,
    Doom = 1 << 0,
    Doom2 = 1 << 1,
    Heretic = 1 << 2,
    Hexen = 1 << 3,
    Strife = 1 << 4,

    // Combinations
    AllDoom = Doom | Doom2,
    All = Doom | Doom2 | Heretic | Hexen | Strife
}
```

### Base Game Things Registration

```csharp
private static void RegisterDoomThings()
{
    // Players
    Register(new ThingDefinition
    {
        DoomEdNum = 1,
        ClassName = "DoomPlayer",
        LegacyName = "Player1Start",
        DisplayName = "Player 1 Start",
        Category = ThingCategory.Player,
        Game = GameType.AllDoom,
        SpawnSprite = "PLAY"
    });
    Register(new ThingDefinition { DoomEdNum = 2, ClassName = "Player2Start", DisplayName = "Player 2 Start", Category = ThingCategory.Player, Game = GameType.AllDoom });
    Register(new ThingDefinition { DoomEdNum = 3, ClassName = "Player3Start", DisplayName = "Player 3 Start", Category = ThingCategory.Player, Game = GameType.AllDoom });
    Register(new ThingDefinition { DoomEdNum = 4, ClassName = "Player4Start", DisplayName = "Player 4 Start", Category = ThingCategory.Player, Game = GameType.AllDoom });
    Register(new ThingDefinition { DoomEdNum = 11, ClassName = "DeathmatchStart", DisplayName = "Deathmatch Start", Category = ThingCategory.Player, Game = GameType.AllDoom });

    // Monsters - DOOM
    Register(new ThingDefinition
    {
        DoomEdNum = 3004,
        ClassName = "ZombieMan",
        LegacyName = "Zombieman",
        DisplayName = "Former Human",
        Category = ThingCategory.Monster,
        Flags = ThingFlags.IsMonster | ThingFlags.Countkill | ThingFlags.Solid | ThingFlags.Shootable,
        Game = GameType.AllDoom,
        Health = 20,
        Speed = 8,
        Radius = 20,
        Height = 56,
        PainChance = 200,
        SpawnSprite = "POSS"
    });
    Register(new ThingDefinition
    {
        DoomEdNum = 9,
        ClassName = "ShotgunGuy",
        DisplayName = "Former Sergeant",
        Category = ThingCategory.Monster,
        Flags = ThingFlags.IsMonster | ThingFlags.Countkill | ThingFlags.Solid | ThingFlags.Shootable,
        Game = GameType.AllDoom,
        Health = 30,
        Speed = 8,
        Radius = 20,
        Height = 56,
        PainChance = 170,
        SpawnSprite = "SPOS"
    });
    Register(new ThingDefinition
    {
        DoomEdNum = 3001,
        ClassName = "DoomImp",
        LegacyName = "Imp",
        DisplayName = "Imp",
        Category = ThingCategory.Monster,
        Flags = ThingFlags.IsMonster | ThingFlags.Countkill | ThingFlags.Solid | ThingFlags.Shootable,
        Game = GameType.AllDoom,
        Health = 60,
        Speed = 8,
        Radius = 20,
        Height = 56,
        PainChance = 200,
        SpawnSprite = "TROO"
    });
    Register(new ThingDefinition
    {
        DoomEdNum = 3002,
        ClassName = "Demon",
        LegacyName = "Pinky",
        DisplayName = "Demon",
        Category = ThingCategory.Monster,
        Flags = ThingFlags.IsMonster | ThingFlags.Countkill | ThingFlags.Solid | ThingFlags.Shootable,
        Game = GameType.AllDoom,
        Health = 150,
        Speed = 10,
        Radius = 30,
        Height = 56,
        PainChance = 180,
        SpawnSprite = "SARG"
    });
    Register(new ThingDefinition
    {
        DoomEdNum = 58,
        ClassName = "Spectre",
        DisplayName = "Spectre",
        Category = ThingCategory.Monster,
        Flags = ThingFlags.IsMonster | ThingFlags.Countkill | ThingFlags.Solid | ThingFlags.Shootable | ThingFlags.Translucent,
        Game = GameType.AllDoom,
        Health = 150,
        Speed = 10,
        Radius = 30,
        Height = 56,
        PainChance = 180,
        SpawnSprite = "SARG"
    });
    Register(new ThingDefinition
    {
        DoomEdNum = 3006,
        ClassName = "LostSoul",
        DisplayName = "Lost Soul",
        Category = ThingCategory.Monster,
        Flags = ThingFlags.IsMonster | ThingFlags.Countkill | ThingFlags.Float | ThingFlags.NoGravity | ThingFlags.Shootable,
        Game = GameType.AllDoom,
        Health = 100,
        Speed = 8,
        Radius = 16,
        Height = 56,
        Damage = 3,
        PainChance = 256,
        SpawnSprite = "SKUL"
    });
    Register(new ThingDefinition
    {
        DoomEdNum = 3005,
        ClassName = "Cacodemon",
        DisplayName = "Cacodemon",
        Category = ThingCategory.Monster,
        Flags = ThingFlags.IsMonster | ThingFlags.Countkill | ThingFlags.Float | ThingFlags.NoGravity | ThingFlags.Solid | ThingFlags.Shootable,
        Game = GameType.AllDoom,
        Health = 400,
        Speed = 8,
        Radius = 31,
        Height = 56,
        PainChance = 128,
        SpawnSprite = "HEAD"
    });
    Register(new ThingDefinition
    {
        DoomEdNum = 3003,
        ClassName = "BaronOfHell",
        LegacyName = "Baron",
        DisplayName = "Baron of Hell",
        Category = ThingCategory.Monster,
        Flags = ThingFlags.IsMonster | ThingFlags.Countkill | ThingFlags.Solid | ThingFlags.Shootable,
        Game = GameType.AllDoom,
        Health = 1000,
        Speed = 8,
        Radius = 24,
        Height = 64,
        PainChance = 50,
        SpawnSprite = "BOSS"
    });
    Register(new ThingDefinition
    {
        DoomEdNum = 16,
        ClassName = "Cyberdemon",
        DisplayName = "Cyberdemon",
        Category = ThingCategory.Boss,
        Flags = ThingFlags.IsMonster | ThingFlags.Countkill | ThingFlags.Solid | ThingFlags.Shootable,
        Game = GameType.AllDoom,
        Health = 4000,
        Speed = 16,
        Radius = 40,
        Height = 110,
        PainChance = 20,
        SpawnSprite = "CYBR"
    });
    Register(new ThingDefinition
    {
        DoomEdNum = 7,
        ClassName = "SpiderMastermind",
        DisplayName = "Spider Mastermind",
        Category = ThingCategory.Boss,
        Flags = ThingFlags.IsMonster | ThingFlags.Countkill | ThingFlags.Solid | ThingFlags.Shootable,
        Game = GameType.AllDoom,
        Health = 3000,
        Speed = 12,
        Radius = 128,
        Height = 100,
        PainChance = 40,
        SpawnSprite = "SPID"
    });

    // Weapons
    Register(new ThingDefinition
    {
        DoomEdNum = 2005,
        ClassName = "Chainsaw",
        DisplayName = "Chainsaw",
        Category = ThingCategory.Weapon,
        Flags = ThingFlags.Pickup,
        Game = GameType.AllDoom,
        SpawnSprite = "CSAW"
    });
    Register(new ThingDefinition
    {
        DoomEdNum = 2001,
        ClassName = "Shotgun",
        DisplayName = "Shotgun",
        Category = ThingCategory.Weapon,
        Flags = ThingFlags.Pickup,
        Game = GameType.AllDoom,
        SpawnSprite = "SHOT"
    });
    Register(new ThingDefinition
    {
        DoomEdNum = 82,
        ClassName = "SuperShotgun",
        DisplayName = "Super Shotgun",
        Category = ThingCategory.Weapon,
        Flags = ThingFlags.Pickup,
        Game = GameType.Doom2,
        SpawnSprite = "SGN2"
    });
    Register(new ThingDefinition
    {
        DoomEdNum = 2002,
        ClassName = "Chaingun",
        DisplayName = "Chaingun",
        Category = ThingCategory.Weapon,
        Flags = ThingFlags.Pickup,
        Game = GameType.AllDoom,
        SpawnSprite = "MGUN"
    });
    Register(new ThingDefinition
    {
        DoomEdNum = 2003,
        ClassName = "RocketLauncher",
        DisplayName = "Rocket Launcher",
        Category = ThingCategory.Weapon,
        Flags = ThingFlags.Pickup,
        Game = GameType.AllDoom,
        SpawnSprite = "LAUN"
    });
    Register(new ThingDefinition
    {
        DoomEdNum = 2004,
        ClassName = "PlasmaRifle",
        DisplayName = "Plasma Rifle",
        Category = ThingCategory.Weapon,
        Flags = ThingFlags.Pickup,
        Game = GameType.AllDoom,
        SpawnSprite = "PLAS"
    });
    Register(new ThingDefinition
    {
        DoomEdNum = 2006,
        ClassName = "BFG9000",
        DisplayName = "BFG 9000",
        Category = ThingCategory.Weapon,
        Flags = ThingFlags.Pickup,
        Game = GameType.AllDoom,
        SpawnSprite = "BFUG"
    });

    // Ammo
    Register(new ThingDefinition { DoomEdNum = 2007, ClassName = "Clip", DisplayName = "Ammo Clip", Category = ThingCategory.Ammo, Flags = ThingFlags.Pickup, Game = GameType.AllDoom, SpawnSprite = "CLIP" });
    Register(new ThingDefinition { DoomEdNum = 2048, ClassName = "Box of Ammo", DisplayName = "Box of Bullets", Category = ThingCategory.Ammo, Flags = ThingFlags.Pickup, Game = GameType.AllDoom, SpawnSprite = "AMMO" });
    Register(new ThingDefinition { DoomEdNum = 2008, ClassName = "Shell", DisplayName = "Shotgun Shells", Category = ThingCategory.Ammo, Flags = ThingFlags.Pickup, Game = GameType.AllDoom, SpawnSprite = "SHEL" });
    Register(new ThingDefinition { DoomEdNum = 2049, ClassName = "ShellBox", DisplayName = "Box of Shells", Category = ThingCategory.Ammo, Flags = ThingFlags.Pickup, Game = GameType.AllDoom, SpawnSprite = "SBOX" });
    Register(new ThingDefinition { DoomEdNum = 2010, ClassName = "RocketAmmo", DisplayName = "Rocket", Category = ThingCategory.Ammo, Flags = ThingFlags.Pickup, Game = GameType.AllDoom, SpawnSprite = "ROCK" });
    Register(new ThingDefinition { DoomEdNum = 2046, ClassName = "RocketBox", DisplayName = "Box of Rockets", Category = ThingCategory.Ammo, Flags = ThingFlags.Pickup, Game = GameType.AllDoom, SpawnSprite = "BROK" });
    Register(new ThingDefinition { DoomEdNum = 2047, ClassName = "Cell", DisplayName = "Energy Cell", Category = ThingCategory.Ammo, Flags = ThingFlags.Pickup, Game = GameType.AllDoom, SpawnSprite = "CELL" });
    Register(new ThingDefinition { DoomEdNum = 17, ClassName = "CellPack", DisplayName = "Energy Cell Pack", Category = ThingCategory.Ammo, Flags = ThingFlags.Pickup, Game = GameType.AllDoom, SpawnSprite = "CELP" });

    // Health & Armor
    Register(new ThingDefinition { DoomEdNum = 2011, ClassName = "Stimpack", DisplayName = "Stimpack", Category = ThingCategory.Health, Flags = ThingFlags.Pickup, Game = GameType.AllDoom, SpawnSprite = "STIM" });
    Register(new ThingDefinition { DoomEdNum = 2012, ClassName = "Medikit", DisplayName = "Medikit", Category = ThingCategory.Health, Flags = ThingFlags.Pickup, Game = GameType.AllDoom, SpawnSprite = "MEDI" });
    Register(new ThingDefinition { DoomEdNum = 2014, ClassName = "HealthBonus", DisplayName = "Health Bonus", Category = ThingCategory.Health, Flags = ThingFlags.Pickup | ThingFlags.CountItem, Game = GameType.AllDoom, SpawnSprite = "BON1" });
    Register(new ThingDefinition { DoomEdNum = 2015, ClassName = "ArmorBonus", DisplayName = "Armor Bonus", Category = ThingCategory.Armor, Flags = ThingFlags.Pickup | ThingFlags.CountItem, Game = GameType.AllDoom, SpawnSprite = "BON2" });
    Register(new ThingDefinition { DoomEdNum = 2018, ClassName = "GreenArmor", DisplayName = "Green Armor", Category = ThingCategory.Armor, Flags = ThingFlags.Pickup, Game = GameType.AllDoom, SpawnSprite = "ARM1" });
    Register(new ThingDefinition { DoomEdNum = 2019, ClassName = "BlueArmor", DisplayName = "Blue Armor", Category = ThingCategory.Armor, Flags = ThingFlags.Pickup, Game = GameType.AllDoom, SpawnSprite = "ARM2" });

    // Powerups
    Register(new ThingDefinition { DoomEdNum = 2022, ClassName = "InvulnerabilitySphere", DisplayName = "Invulnerability", Category = ThingCategory.Powerup, Flags = ThingFlags.Pickup | ThingFlags.CountItem, Game = GameType.AllDoom, SpawnSprite = "PINV" });
    Register(new ThingDefinition { DoomEdNum = 2023, ClassName = "Berserk", DisplayName = "Berserk", Category = ThingCategory.Powerup, Flags = ThingFlags.Pickup | ThingFlags.CountItem, Game = GameType.AllDoom, SpawnSprite = "PSTR" });
    Register(new ThingDefinition { DoomEdNum = 2024, ClassName = "BlurSphere", DisplayName = "Partial Invisibility", Category = ThingCategory.Powerup, Flags = ThingFlags.Pickup | ThingFlags.CountItem, Game = GameType.AllDoom, SpawnSprite = "PINS" });
    Register(new ThingDefinition { DoomEdNum = 2025, ClassName = "RadSuit", DisplayName = "Radiation Suit", Category = ThingCategory.Powerup, Flags = ThingFlags.Pickup, Game = GameType.AllDoom, SpawnSprite = "SUIT" });
    Register(new ThingDefinition { DoomEdNum = 2026, ClassName = "Allmap", DisplayName = "Computer Map", Category = ThingCategory.Powerup, Flags = ThingFlags.Pickup | ThingFlags.CountItem, Game = GameType.AllDoom, SpawnSprite = "PMAP" });
    Register(new ThingDefinition { DoomEdNum = 2045, ClassName = "Infrared", DisplayName = "Light Amplification Visor", Category = ThingCategory.Powerup, Flags = ThingFlags.Pickup | ThingFlags.CountItem, Game = GameType.AllDoom, SpawnSprite = "PVIS" });
    Register(new ThingDefinition { DoomEdNum = 83, ClassName = "Megasphere", DisplayName = "Megasphere", Category = ThingCategory.Powerup, Flags = ThingFlags.Pickup | ThingFlags.CountItem, Game = GameType.Doom2, SpawnSprite = "MEGA" });
    Register(new ThingDefinition { DoomEdNum = 2013, ClassName = "Soulsphere", DisplayName = "Soulsphere", Category = ThingCategory.Powerup, Flags = ThingFlags.Pickup | ThingFlags.CountItem, Game = GameType.AllDoom, SpawnSprite = "SOUL" });

    // Keys
    Register(new ThingDefinition { DoomEdNum = 5, ClassName = "BlueCard", DisplayName = "Blue Keycard", Category = ThingCategory.Key, Flags = ThingFlags.Pickup, Game = GameType.AllDoom, SpawnSprite = "BKEY" });
    Register(new ThingDefinition { DoomEdNum = 6, ClassName = "YellowCard", DisplayName = "Yellow Keycard", Category = ThingCategory.Key, Flags = ThingFlags.Pickup, Game = GameType.AllDoom, SpawnSprite = "YKEY" });
    Register(new ThingDefinition { DoomEdNum = 13, ClassName = "RedCard", DisplayName = "Red Keycard", Category = ThingCategory.Key, Flags = ThingFlags.Pickup, Game = GameType.AllDoom, SpawnSprite = "RKEY" });
    Register(new ThingDefinition { DoomEdNum = 40, ClassName = "BlueSkull", DisplayName = "Blue Skull Key", Category = ThingCategory.Key, Flags = ThingFlags.Pickup, Game = GameType.AllDoom, SpawnSprite = "BSKU" });
    Register(new ThingDefinition { DoomEdNum = 39, ClassName = "YellowSkull", DisplayName = "Yellow Skull Key", Category = ThingCategory.Key, Flags = ThingFlags.Pickup, Game = GameType.AllDoom, SpawnSprite = "YSKU" });
    Register(new ThingDefinition { DoomEdNum = 38, ClassName = "RedSkull", DisplayName = "Red Skull Key", Category = ThingCategory.Key, Flags = ThingFlags.Pickup, Game = GameType.AllDoom, SpawnSprite = "RSKU" });
}

private static void RegisterDoom2Things()
{
    // DOOM II exclusive monsters
    Register(new ThingDefinition
    {
        DoomEdNum = 65,
        ClassName = "ChaingunGuy",
        LegacyName = "HeavyWeaponDude",
        DisplayName = "Heavy Weapon Dude",
        Category = ThingCategory.Monster,
        Flags = ThingFlags.IsMonster | ThingFlags.Countkill | ThingFlags.Solid | ThingFlags.Shootable,
        Game = GameType.Doom2,
        Health = 70,
        Speed = 8,
        Radius = 20,
        Height = 56,
        PainChance = 170,
        SpawnSprite = "CPOS"
    });
    Register(new ThingDefinition
    {
        DoomEdNum = 66,
        ClassName = "Revenant",
        DisplayName = "Revenant",
        Category = ThingCategory.Monster,
        Flags = ThingFlags.IsMonster | ThingFlags.Countkill | ThingFlags.Solid | ThingFlags.Shootable,
        Game = GameType.Doom2,
        Health = 300,
        Speed = 10,
        Radius = 20,
        Height = 64,
        PainChance = 100,
        SpawnSprite = "SKEL"
    });
    Register(new ThingDefinition
    {
        DoomEdNum = 67,
        ClassName = "Fatso",
        LegacyName = "Mancubus",
        DisplayName = "Mancubus",
        Category = ThingCategory.Monster,
        Flags = ThingFlags.IsMonster | ThingFlags.Countkill | ThingFlags.Solid | ThingFlags.Shootable,
        Game = GameType.Doom2,
        Health = 600,
        Speed = 8,
        Radius = 48,
        Height = 64,
        PainChance = 80,
        SpawnSprite = "FATT"
    });
    Register(new ThingDefinition
    {
        DoomEdNum = 68,
        ClassName = "Arachnotron",
        DisplayName = "Arachnotron",
        Category = ThingCategory.Monster,
        Flags = ThingFlags.IsMonster | ThingFlags.Countkill | ThingFlags.Solid | ThingFlags.Shootable,
        Game = GameType.Doom2,
        Health = 500,
        Speed = 12,
        Radius = 64,
        Height = 64,
        PainChance = 128,
        SpawnSprite = "BSPI"
    });
    Register(new ThingDefinition
    {
        DoomEdNum = 69,
        ClassName = "HellKnight",
        DisplayName = "Hell Knight",
        Category = ThingCategory.Monster,
        Flags = ThingFlags.IsMonster | ThingFlags.Countkill | ThingFlags.Solid | ThingFlags.Shootable,
        Game = GameType.Doom2,
        Health = 500,
        Speed = 8,
        Radius = 24,
        Height = 64,
        PainChance = 50,
        SpawnSprite = "BOS2"
    });
    Register(new ThingDefinition
    {
        DoomEdNum = 71,
        ClassName = "PainElemental",
        DisplayName = "Pain Elemental",
        Category = ThingCategory.Monster,
        Flags = ThingFlags.IsMonster | ThingFlags.Countkill | ThingFlags.Float | ThingFlags.NoGravity | ThingFlags.Solid | ThingFlags.Shootable,
        Game = GameType.Doom2,
        Health = 400,
        Speed = 8,
        Radius = 31,
        Height = 56,
        PainChance = 128,
        SpawnSprite = "PAIN"
    });
    Register(new ThingDefinition
    {
        DoomEdNum = 64,
        ClassName = "Archvile",
        LegacyName = "ArchVile",
        DisplayName = "Arch-vile",
        Category = ThingCategory.Monster,
        Flags = ThingFlags.IsMonster | ThingFlags.Countkill | ThingFlags.Solid | ThingFlags.Shootable,
        Game = GameType.Doom2,
        Health = 700,
        Speed = 15,
        Radius = 20,
        Height = 56,
        PainChance = 10,
        SpawnSprite = "VILE"
    });
    Register(new ThingDefinition
    {
        DoomEdNum = 72,
        ClassName = "CommanderKeen",
        DisplayName = "Commander Keen",
        Category = ThingCategory.Monster,
        Flags = ThingFlags.Solid | ThingFlags.Shootable | ThingFlags.SpawnCeiling | ThingFlags.NoGravity,
        Game = GameType.Doom2,
        Health = 100,
        SpawnSprite = "KEEN"
    });
    Register(new ThingDefinition
    {
        DoomEdNum = 84,
        ClassName = "WolfensteinSS",
        DisplayName = "Wolfenstein SS",
        Category = ThingCategory.Monster,
        Flags = ThingFlags.IsMonster | ThingFlags.Countkill | ThingFlags.Solid | ThingFlags.Shootable,
        Game = GameType.Doom2,
        Health = 50,
        Speed = 8,
        Radius = 20,
        Height = 56,
        PainChance = 170,
        SpawnSprite = "SSWV"
    });
    Register(new ThingDefinition
    {
        DoomEdNum = 88,
        ClassName = "BossBrain",
        DisplayName = "Boss Brain",
        Category = ThingCategory.Boss,
        Flags = ThingFlags.Solid | ThingFlags.Shootable,
        Game = GameType.Doom2,
        Health = 250,
        SpawnSprite = "BBRN"
    });
    Register(new ThingDefinition
    {
        DoomEdNum = 89,
        ClassName = "BossTarget",
        DisplayName = "Boss Shooter Target",
        Category = ThingCategory.Boss,
        Game = GameType.Doom2
    });
    Register(new ThingDefinition
    {
        DoomEdNum = 87,
        ClassName = "BossEye",
        DisplayName = "Boss Shooter",
        Category = ThingCategory.Boss,
        Game = GameType.Doom2,
        SpawnSprite = "SSWV"
    });
}
```

---

## Task 9.2: Sector Special Database

Reference: dmsp1666.html Section 4.9.1

```csharp
namespace WAD.NET.Definitions;

public static class SectorDatabase
{
    private static readonly Dictionary<int, SectorSpecialDefinition> _specials = new();

    static SectorDatabase()
    {
        RegisterDoomSectorSpecials();
        RegisterBoomSectorSpecials();
    }

    public static SectorSpecialDefinition? Get(int special)
    {
        return _specials.TryGetValue(special, out var def) ? def : null;
    }

    public static bool IsSecret(int special) => special == 9;

    public static bool IsDamaging(int special)
    {
        var def = Get(special);
        return def?.DamageAmount > 0;
    }

    public static bool IsLightEffect(int special)
    {
        var def = Get(special);
        return def?.LightEffect != LightEffect.None;
    }
}

public class SectorSpecialDefinition
{
    public int Special { get; init; }
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public SectorSpecialType Type { get; init; }
    public int DamageAmount { get; init; }
    public int DamageInterval { get; init; }  // Tics between damage
    public bool LeakySuit { get; init; }      // Rad suit doesn't fully protect
    public LightEffect LightEffect { get; init; }
    public GameType Game { get; init; }
}

public enum SectorSpecialType
{
    None,
    Light,
    Damage,
    Secret,
    Door,
    End,
    Special
}

public enum LightEffect
{
    None,
    BlinkRandom,
    BlinkHalf,
    BlinkFull,
    Oscillate,
    FlickerFire
}

private static void RegisterDoomSectorSpecials()
{
    // Light effects
    Register(new SectorSpecialDefinition
    {
        Special = 1,
        Name = "Light Blink (random)",
        Type = SectorSpecialType.Light,
        LightEffect = LightEffect.BlinkRandom,
        Game = GameType.AllDoom
    });
    Register(new SectorSpecialDefinition
    {
        Special = 2,
        Name = "Light Blink (0.5 sec)",
        Type = SectorSpecialType.Light,
        LightEffect = LightEffect.BlinkHalf,
        Game = GameType.AllDoom
    });
    Register(new SectorSpecialDefinition
    {
        Special = 3,
        Name = "Light Blink (1 sec)",
        Type = SectorSpecialType.Light,
        LightEffect = LightEffect.BlinkFull,
        Game = GameType.AllDoom
    });

    // Damage sectors
    Register(new SectorSpecialDefinition
    {
        Special = 4,
        Name = "Damage -10/20% health + light blink",
        Description = "Nukage damage with blinking light",
        Type = SectorSpecialType.Damage,
        DamageAmount = 20,
        DamageInterval = 32,
        LightEffect = LightEffect.BlinkHalf,
        Game = GameType.AllDoom
    });
    Register(new SectorSpecialDefinition
    {
        Special = 5,
        Name = "Damage -5/10% health",
        Description = "Light nukage/slime damage",
        Type = SectorSpecialType.Damage,
        DamageAmount = 10,
        DamageInterval = 32,
        Game = GameType.AllDoom
    });
    Register(new SectorSpecialDefinition
    {
        Special = 7,
        Name = "Damage -2/5% health",
        Description = "Light damage (blood, etc.)",
        Type = SectorSpecialType.Damage,
        DamageAmount = 5,
        DamageInterval = 32,
        Game = GameType.AllDoom
    });
    Register(new SectorSpecialDefinition
    {
        Special = 11,
        Name = "Damage -10/20% health (no exit)",
        Description = "Like type 4 but doesn't end level at 10%",
        Type = SectorSpecialType.Damage,
        DamageAmount = 20,
        DamageInterval = 32,
        Game = GameType.AllDoom
    });
    Register(new SectorSpecialDefinition
    {
        Special = 16,
        Name = "Damage -10/20% health",
        Description = "Super nukage damage",
        Type = SectorSpecialType.Damage,
        DamageAmount = 20,
        DamageInterval = 32,
        Game = GameType.AllDoom
    });

    // Oscillating light
    Register(new SectorSpecialDefinition
    {
        Special = 8,
        Name = "Light Oscillates",
        Type = SectorSpecialType.Light,
        LightEffect = LightEffect.Oscillate,
        Game = GameType.AllDoom
    });

    // Secret
    Register(new SectorSpecialDefinition
    {
        Special = 9,
        Name = "Secret",
        Description = "Counted as secret when entered",
        Type = SectorSpecialType.Secret,
        Game = GameType.AllDoom
    });

    // Door close
    Register(new SectorSpecialDefinition
    {
        Special = 10,
        Name = "Door Close (30 sec)",
        Description = "30 seconds after level start, ceiling closes",
        Type = SectorSpecialType.Door,
        Game = GameType.AllDoom
    });

    // End level
    Register(new SectorSpecialDefinition
    {
        Special = 11,
        Name = "End Level (20% damage)",
        Description = "Damages player 20% and ends level on death",
        Type = SectorSpecialType.End,
        DamageAmount = 20,
        DamageInterval = 32,
        Game = GameType.AllDoom
    });

    // Sync blink
    Register(new SectorSpecialDefinition
    {
        Special = 12,
        Name = "Light Blink (sync, 0.5 sec)",
        Type = SectorSpecialType.Light,
        LightEffect = LightEffect.BlinkHalf,
        Game = GameType.AllDoom
    });
    Register(new SectorSpecialDefinition
    {
        Special = 13,
        Name = "Light Blink (sync, 1 sec)",
        Type = SectorSpecialType.Light,
        LightEffect = LightEffect.BlinkFull,
        Game = GameType.AllDoom
    });

    // Door open
    Register(new SectorSpecialDefinition
    {
        Special = 14,
        Name = "Door Open (300 sec)",
        Description = "300 seconds after level start, ceiling opens",
        Type = SectorSpecialType.Door,
        Game = GameType.AllDoom
    });

    // Fire flicker
    Register(new SectorSpecialDefinition
    {
        Special = 17,
        Name = "Light Flickers (fire)",
        Type = SectorSpecialType.Light,
        LightEffect = LightEffect.FlickerFire,
        Game = GameType.AllDoom
    });
}
```

---

## Task 9.3: Linedef Action Database

```csharp
namespace WAD.NET.Definitions;

public static class LinedefDatabase
{
    private static readonly Dictionary<int, LinedefActionDefinition> _actions = new();

    public static LinedefActionDefinition? Get(int action)
    {
        return _actions.TryGetValue(action, out var def) ? def : null;
    }

    public static bool IsExit(int action)
    {
        var def = Get(action);
        return def?.ActionType == LinedefActionType.Exit;
    }

    public static bool IsTeleporter(int action)
    {
        var def = Get(action);
        return def?.ActionType == LinedefActionType.Teleport;
    }
}

public class LinedefActionDefinition
{
    public int Action { get; init; }
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public LinedefActionType ActionType { get; init; }
    public TriggerType Trigger { get; init; }
    public bool Repeatable { get; init; }
    public bool MonsterActivated { get; init; }
    public GameType Game { get; init; }
}

public enum LinedefActionType
{
    None,
    Door,
    Floor,
    Ceiling,
    Platform,
    Lift,
    Stairs,
    Crusher,
    Light,
    Teleport,
    Exit,
    Scroll,
    Special
}

public enum TriggerType
{
    Walk,       // W - Walk across
    Switch,     // S - Use switch
    Gun,        // G - Shoot
    Push,       // P - Push wall
    Automatic,  // Auto-triggered
}
```

---

## Task 9.4: Sprite Name Constants

```csharp
namespace WAD.NET.Definitions;

/// <summary>
/// Well-known sprite names and their meanings.
/// </summary>
public static class SpriteConstants
{
    /// <summary>Invisible sprite - thing has no visual representation.</summary>
    public const string Invisible = "TNT1";

    /// <summary>Null/missing sprite marker.</summary>
    public const string Null = "----";

    /// <summary>Standard sprite name length.</summary>
    public const int SpriteNameLength = 4;

    /// <summary>All-angle rotation indicator (frame letter followed by 0).</summary>
    public const char AllAngles = '0';

    /// <summary>
    /// Checks if a sprite name represents an invisible/null sprite.
    /// </summary>
    public static bool IsInvisible(string spriteName)
    {
        return string.Equals(spriteName, Invisible, StringComparison.OrdinalIgnoreCase)
            || string.Equals(spriteName, Null, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Validates a sprite name format.
    /// </summary>
    public static bool IsValidSpriteName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        if (name.Length != SpriteNameLength)
            return false;

        // Must be alphanumeric or underscore
        foreach (char c in name)
        {
            if (!char.IsLetterOrDigit(c) && c != '_')
                return false;
        }

        return true;
    }
}

/// <summary>
/// Texture name constants.
/// </summary>
public static class TextureConstants
{
    /// <summary>Null texture marker - no texture on this surface.</summary>
    public const string NullTexture = "-";

    /// <summary>
    /// Checks if a texture name represents no texture.
    /// </summary>
    public static bool IsNull(string textureName)
    {
        return string.IsNullOrEmpty(textureName)
            || textureName == NullTexture;
    }
}

/// <summary>
/// Binary format magic values.
/// </summary>
public static class BinaryConstants
{
    // WAD header
    public const string IwadMarker = "IWAD";
    public const string PwadMarker = "PWAD";

    // BLOCKMAP
    public const int BlockmapBlockSize = 128;
    public const ushort BlockmapTerminator = 0xFFFF;
    public const ushort BlockmapEmptyBlock = 0x0000;

    // NODES
    public const ushort SubsectorFlag = 0x8000;

    // ACS
    public const string AcsMarker = "ACS";
    public const string AcsEnhancedMarker = "ACSE";
    public const string AcsLittleEndianMarker = "ACSe";

    // ENDOOM
    public const int EndoomSize = 4000;  // 80x25 characters * 2 bytes
}
```

---

## Task 9.5: Map Name Patterns

```csharp
namespace WAD.NET.Definitions;

public static class MapNamePatterns
{
    /// <summary>DOOM format: E#M# (Episode 1-9, Map 1-9)</summary>
    public static readonly Regex DoomPattern = new(@"^E([1-9])M([1-9])$", RegexOptions.Compiled);

    /// <summary>DOOM II format: MAP## (01-99)</summary>
    public static readonly Regex Doom2Pattern = new(@"^MAP(\d{2})$", RegexOptions.Compiled);

    /// <summary>Hexen format: MAP## (01-99)</summary>
    public static readonly Regex HexenPattern = new(@"^MAP(\d{2})$", RegexOptions.Compiled);

    /// <summary>
    /// Parses a map name into its components.
    /// </summary>
    public static MapName? Parse(string mapName)
    {
        if (string.IsNullOrEmpty(mapName))
            return null;

        var doomMatch = DoomPattern.Match(mapName);
        if (doomMatch.Success)
        {
            return new MapName
            {
                Format = MapFormat.Doom,
                RawName = mapName,
                Episode = int.Parse(doomMatch.Groups[1].Value),
                Map = int.Parse(doomMatch.Groups[2].Value)
            };
        }

        var doom2Match = Doom2Pattern.Match(mapName);
        if (doom2Match.Success)
        {
            return new MapName
            {
                Format = MapFormat.Doom2,
                RawName = mapName,
                Map = int.Parse(doom2Match.Groups[1].Value)
            };
        }

        // Non-standard name
        return new MapName
        {
            Format = MapFormat.Custom,
            RawName = mapName
        };
    }
}

public class MapName
{
    public MapFormat Format { get; init; }
    public string RawName { get; init; } = "";
    public int? Episode { get; init; }
    public int? Map { get; init; }

    /// <summary>
    /// Gets a sortable index for ordering maps.
    /// </summary>
    public int SortOrder => Format switch
    {
        MapFormat.Doom => (Episode ?? 0) * 10 + (Map ?? 0),
        MapFormat.Doom2 => Map ?? 0,
        _ => 0
    };
}

public enum MapFormat
{
    Doom,    // E#M#
    Doom2,   // MAP##
    Hexen,   // MAP## with BEHAVIOR
    Custom   // Any other naming
}
```

---

## Acceptance Criteria

1. All magic numbers in WadValidator replaced with `ThingDatabase` lookups
2. All sector special checks use `SectorDatabase` methods
3. Sprite name checks use `SpriteConstants` methods
4. Texture null checks use `TextureConstants.IsNull()`
5. Map name parsing uses `MapNamePatterns.Parse()`
6. Binary format constants centralized in `BinaryConstants`
7. Thing detection uses `ThingDatabase.Get(doomEdNum)` instead of hardcoded sets

---

## Test Cases

```csharp
[Fact]
public void ShouldLookupZombiemanByDoomEdNum()
{
    var thing = ThingDatabase.Get(3004);

    Assert.NotNull(thing);
    Assert.Equal("ZombieMan", thing.ClassName);
    Assert.Equal(ThingCategory.Monster, thing.Category);
    Assert.True(thing.Flags.HasFlag(ThingFlags.IsMonster));
    Assert.Equal(20, thing.Health);
}

[Fact]
public void ShouldLookupThingByName()
{
    var thing = ThingDatabase.GetByName("Cyberdemon");

    Assert.NotNull(thing);
    Assert.Equal(16, thing.DoomEdNum);
    Assert.Equal(ThingCategory.Boss, thing.Category);
}

[Fact]
public void ShouldIdentifyPlayerStarts()
{
    var playerStarts = new[] { 1, 2, 3, 4, 11 };

    foreach (var doomEdNum in playerStarts)
    {
        var thing = ThingDatabase.Get(doomEdNum);
        Assert.Equal(ThingCategory.Player, thing?.Category);
    }
}

[Fact]
public void ShouldIdentifySecretSector()
{
    Assert.True(SectorDatabase.IsSecret(9));
    Assert.False(SectorDatabase.IsSecret(0));
    Assert.False(SectorDatabase.IsSecret(4));
}

[Fact]
public void ShouldIdentifyDamageSectors()
{
    var damageSectors = new[] { 4, 5, 7, 11, 16 };

    foreach (var special in damageSectors)
    {
        Assert.True(SectorDatabase.IsDamaging(special));
    }

    Assert.False(SectorDatabase.IsDamaging(9));  // Secret, not damage
}

[Fact]
public void ShouldParseDoomMapName()
{
    var map = MapNamePatterns.Parse("E1M1");

    Assert.Equal(MapFormat.Doom, map?.Format);
    Assert.Equal(1, map?.Episode);
    Assert.Equal(1, map?.Map);
}

[Fact]
public void ShouldParseDoom2MapName()
{
    var map = MapNamePatterns.Parse("MAP23");

    Assert.Equal(MapFormat.Doom2, map?.Format);
    Assert.Null(map?.Episode);
    Assert.Equal(23, map?.Map);
}

[Fact]
public void ShouldIdentifyInvisibleSprites()
{
    Assert.True(SpriteConstants.IsInvisible("TNT1"));
    Assert.True(SpriteConstants.IsInvisible("----"));
    Assert.False(SpriteConstants.IsInvisible("POSS"));
}

[Fact]
public void ShouldIdentifyNullTextures()
{
    Assert.True(TextureConstants.IsNull("-"));
    Assert.True(TextureConstants.IsNull(""));
    Assert.True(TextureConstants.IsNull(null));
    Assert.False(TextureConstants.IsNull("STARTAN2"));
}
```

---

## Consumer Requirements: WadAgent Integration

WadAgent (WadDB.WadAgent) currently contains ~230 lines of hardcoded game data that must be replaced by this database. The following data and APIs are **required** for WadAgent integration.

### Data WadAgent Currently Hardcodes

These must all be available from `ThingDatabase`:

```csharp
// WadAgent/Services/ActorService.cs lines 22-37
// Must be queryable via ThingDatabase.GetByCategory(ThingCategory.Weapon)
private static readonly HashSet<string> KnownBaseWeapons = new()
{
    "Fist", "Chainsaw", "Pistol", "Shotgun", "SuperShotgun", "Chaingun",
    "RocketLauncher", "PlasmaRifle", "BFG9000",
    "Staff", "GoldenWand", "Crossbow", "Blaster", "SkullRod", "PhoenixRod", "Mace", "Gauntlets",
    // ... Hexen weapons
};

// WadAgent/Services/ActorService.cs lines 39-61
// Must be available via ThingDefinition.SpawnSprite
private static readonly Dictionary<string, string> KnownWeaponSprites = new()
{
    { "Fist", "PUNG" }, { "Chainsaw", "SAWG" }, { "Pistol", "PISG" },
    { "Shotgun", "SHTG" }, { "SuperShotgun", "SHT2" }, { "Chaingun", "CHGG" },
    // ...
};

// WadAgent/Services/ActorService.cs lines 86-108
// Must be queryable via ThingDatabase.GetByCategory(ThingCategory.Monster)
private static readonly HashSet<string> KnownBaseMonsters = new()
{
    "ZombieMan", "ShotgunGuy", "ChaingunGuy", "DoomImp", "Demon", "Spectre",
    "LostSoul", "Cacodemon", "HellKnight", "BaronOfHell", "Arachnotron",
    // ...
};

// WadAgent/Services/ActorService.cs lines 116-156
// Must be available via ThingDefinition.SpawnSprite
private static readonly Dictionary<string, string> KnownMonsterSprites = new()
{
    { "ZombieMan", "POSS" }, { "ShotgunGuy", "SPOS" }, { "ChaingunGuy", "CPOS" },
    { "DoomImp", "TROO" }, { "Demon", "SARG" }, { "Cacodemon", "HEAD" },
    // ...
};
```

### Required ThingDefinition Properties

```csharp
public class ThingDefinition
{
    // Already in spec:
    public int DoomEdNum { get; init; }
    public string ClassName { get; init; }
    public string? LegacyName { get; init; }
    public string DisplayName { get; init; }
    public ThingCategory Category { get; init; }
    public ThingFlags Flags { get; init; }
    public GameType Game { get; init; }
    public int? Health { get; init; }
    public int? Speed { get; init; }
    public string? SpawnSprite { get; init; }

    // REQUIRED ADDITION for weapons:
    public int? Slot { get; init; }  // Weapon slot number (1-9)
}
```

### Required ThingDatabase APIs

```csharp
public static class ThingDatabase
{
    // Already in spec:
    public static ThingDefinition? Get(int doomEdNum);
    public static ThingDefinition? GetByName(string name);
    public static IEnumerable<ThingDefinition> GetByCategory(ThingCategory category);
    public static IEnumerable<ThingDefinition> GetMonsters();
    public static IEnumerable<ThingDefinition> GetWeapons();

    // REQUIRED ADDITION: Filter by game type
    public static IEnumerable<ThingDefinition> GetByCategory(ThingCategory category, GameType game);

    // REQUIRED ADDITION: Check if a class name is a known base game actor
    public static bool IsKnownActor(string className);
}
```

### WadAgent Usage Pattern

```csharp
// Replace hardcoded KnownBaseMonsters with:
var isKnownMonster = ThingDatabase.GetByName(parentClass) is { Category: ThingCategory.Monster };

// Replace hardcoded KnownMonsterSprites with:
var baseThing = ThingDatabase.GetByName(className);
var spritePrefix = baseThing?.SpawnSprite;

// Replace hardcoded BaseGameWeapons loop with:
var iwadType = DetectIWADType(filePaths);  // returns GameType.Doom or GameType.Doom2
var baseWeapons = ThingDatabase.GetByCategory(ThingCategory.Weapon, iwadType);

foreach (var weapon in baseWeapons)
{
    if (replacedByMod.Contains(weapon.ClassName))
        continue;

    weapons.Add(new WeaponInfo
    {
        Name = weapon.DisplayName,
        ClassName = weapon.ClassName,
        Slot = weapon.Slot,
        SpritePrefixes = new[] { weapon.SpawnSprite },
        // ...
    });
}
```

### Verification Checklist

Before releasing, verify WadAgent can delete:
- [ ] `KnownBaseWeapons` HashSet (~15 entries) - replaced by `ThingDatabase.GetWeapons()`
- [ ] `KnownWeaponSprites` Dictionary (~15 entries) - replaced by `ThingDefinition.SpawnSprite`
- [ ] `BaseGameWeapons` tuple array (~9 entries) - replaced by `ThingDatabase.GetWeapons()`
- [ ] `KnownBaseMonsters` HashSet (~40 entries) - replaced by `ThingDatabase.GetMonsters()`
- [ ] `KnownMonsterSprites` Dictionary (~30 entries) - replaced by `ThingDefinition.SpawnSprite`

### Complete Coverage Requirements

The `ThingDatabase` must include **all** of the following for WadAgent compatibility:

**Doom Monsters** (with SpawnSprite):
- ZombieMan (POSS), ShotgunGuy (SPOS), ChaingunGuy (CPOS)
- DoomImp (TROO), Demon (SARG), Spectre (SARG)
- LostSoul (SKUL), Cacodemon (HEAD), HellKnight (BOS2)
- BaronOfHell (BOSS), Arachnotron (BSPI), PainElemental (PAIN)
- Revenant (SKEL), Mancubus/Fatso (FATT), Archvile (VILE)
- SpiderMastermind (SPID), Cyberdemon (CYBR)
- WolfensteinSS (SSWV), CommanderKeen (KEEN)

**Doom Weapons** (with SpawnSprite and Slot):
- Fist (PUNG, slot 1), Chainsaw (SAWG, slot 1)
- Pistol (PISG, slot 2)
- Shotgun (SHTG, slot 3), SuperShotgun (SHT2, slot 3)
- Chaingun (CHGG, slot 4)
- RocketLauncher (MISG, slot 5)
- PlasmaRifle (PLSG, slot 6)
- BFG9000 (BFGG, slot 7)

**Heretic Monsters and Weapons** (if supporting Heretic)

**Hexen Monsters and Weapons** (if supporting Hexen)
