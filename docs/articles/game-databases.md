# Game Databases

WAD.NET includes comprehensive databases of game constants for DOOM, DOOM II, and source port extensions. These databases let you look up thing types (DoomEd numbers), linedef actions, and sector specials by ID, name, or category.

## Thing Database

The `ThingDatabase` contains [DoomEd numbers](https://doomwiki.org/wiki/DoomEd_number) - the numeric identifiers used in map editors to place monsters, items, weapons, and decorations.

### Looking Up a Thing

```csharp
using WAD.NET.Definitions.GameData;

// Look up by DoomEd number
var imp = ThingDatabase.Get(3001);
Console.WriteLine($"{imp.Name}: {imp.ClassName} (DoomEdNum {imp.DoomEdNum})");
Console.WriteLine($"  Category: {imp.Category}");
Console.WriteLine($"  Game: {imp.Game}");
Console.WriteLine($"  Description: {imp.Description}");

// Look up by class name
var baron = ThingDatabase.GetByName("BaronOfHell");
```

### Querying by Category

```csharp
// All monsters
foreach (var monster in ThingDatabase.GetMonsters())
{
    Console.WriteLine($"[{monster.DoomEdNum}] {monster.Name}");
}

// All weapons
foreach (var weapon in ThingDatabase.GetWeapons())
{
    Console.WriteLine($"[{weapon.DoomEdNum}] {weapon.Name}");
}

// By category
var pickups = ThingDatabase.GetByCategory(ThingCategory.Ammo);
var decorations = ThingDatabase.GetByCategory(ThingCategory.Decoration);
var powerups = ThingDatabase.GetByCategory(ThingCategory.Powerup);
```

### Filtering by Game

```csharp
// DOOM II-only monsters
var doom2Monsters = ThingDatabase.GetByCategory(ThingCategory.Monster, GameType.Doom2);
foreach (var m in doom2Monsters)
{
    Console.WriteLine($"[{m.DoomEdNum}] {m.Name}");
}
```

### Thing Categories

| Category | Examples |
|----------|----------|
| `Player` | Player starts, deathmatch starts |
| `Monster` | [Imp](https://doomwiki.org/wiki/Imp), [Cacodemon](https://doomwiki.org/wiki/Cacodemon), [Cyberdemon](https://doomwiki.org/wiki/Cyberdemon) |
| `Weapon` | [Shotgun](https://doomwiki.org/wiki/Shotgun), [BFG9000](https://doomwiki.org/wiki/BFG9000) |
| `Ammo` | Clips, shells, rockets, cells |
| `Health` | Stimpacks, medikits, health bonuses |
| `Armor` | Green armor, blue armor, armor bonuses |
| `Powerup` | [Invulnerability](https://doomwiki.org/wiki/Invulnerability), blur sphere, light amplification |
| `Key` | Keycards, skull keys |
| `Decoration` | Lamps, barrels, corpses, trees |
| `Obstacle` | Blocking decorations |
| `Misc` | Teleport destinations, sound sequences |

### Supported Games

The database includes things from:
- **DOOM** (Shareware + Registered)
- **DOOM II** (additional monsters and items)
- **Boom** (extensions like [MF_FRIEND](https://doomwiki.org/wiki/Friendly_monster))
- **MBF21** (further extensions)

## Linedef Database

The `LinedefDatabase` contains [linedef action](https://doomwiki.org/wiki/Linedef_type) definitions - the triggers and effects assigned to walls in map editors.

```csharp
// Look up by action number
var action = LinedefDatabase.Get(1);
// Door open wait close (standard DOOM action #1)
Console.WriteLine($"[{action.Id}] {action.Name}");
Console.WriteLine($"  Trigger: {action.Trigger}");
Console.WriteLine($"  Description: {action.Description}");

// Get all actions of a type
var doors = LinedefDatabase.GetByCategory("Door");
var lifts = LinedefDatabase.GetByCategory("Lift");
```

### DOOM Linedef Actions

Classic DOOM uses [linedef types](https://doomwiki.org/wiki/Linedef_type) numbered 1-141, with each number mapping to a specific effect (door, lift, crusher, teleport, etc.). [Boom](https://doomwiki.org/wiki/Boom) extended this with generalized linedef types (0x2F80-0x7FFF).

## Sector Database

The `SectorDatabase` contains [sector special](https://doomwiki.org/wiki/Sector#Special_sector_types) definitions - light effects and damage types assigned to floor/ceiling areas.

```csharp
var special = SectorDatabase.Get(1);
// Blink random
Console.WriteLine($"[{special.Id}] {special.Name}");
Console.WriteLine($"  Light: {special.LightEffect}");
Console.WriteLine($"  Damage: {special.Damage}");
```

### DOOM Sector Specials

| ID | Effect | Reference |
|----|--------|-----------|
| 0 | Normal | |
| 1 | Blink random | [Doom Wiki](https://doomwiki.org/wiki/Sector#Type_1_-_Blink_random) |
| 2 | Blink 0.5 second | [Doom Wiki](https://doomwiki.org/wiki/Sector#Type_2_-_Blink_0.5_second) |
| 3 | Blink 1.0 second | [Doom Wiki](https://doomwiki.org/wiki/Sector#Type_3_-_Blink_1.0_second) |
| 4 | 20% damage + blink | [Doom Wiki](https://doomwiki.org/wiki/Sector#Type_4_-_-10.2F20.25_damage.2C_blink_0.5_second) |
| 5 | 10% damage | |
| 7 | 5% damage | |
| 9 | Secret | [Doom Wiki](https://doomwiki.org/wiki/Secret) |
| 11 | 20% damage + end level | |
| 16 | 20% damage | |
| 17 | Blink fire flicker | |

## Format References

- [Doom Wiki - DoomEd number](https://doomwiki.org/wiki/DoomEd_number) - What DoomEd numbers are
- [Doom Wiki - Thing types](https://doomwiki.org/wiki/Thing_types) - Complete thing type tables
- [Doom Wiki - Linedef type](https://doomwiki.org/wiki/Linedef_type) - All linedef actions
- [Doom Wiki - Sector](https://doomwiki.org/wiki/Sector) - Sector types and specials
- [Doom Wiki - Boom](https://doomwiki.org/wiki/Boom) - Boom source port extensions
- [Doom Wiki - MBF21](https://doomwiki.org/wiki/MBF21) - MBF21 specification
- [ZDoom Wiki - Standard linedef types](https://zdoom.org/wiki/Linedef_type) - ZDoom linedef type reference
