using System;
using System.Collections.Generic;
using System.Linq;

namespace WAD.NET.Definitions.GameData
{
    /// <summary>
    /// Database of thing types (DoomEd numbers) for all supported games.
    /// </summary>
    public static class ThingDatabase
    {
        private static readonly Dictionary<int, ThingDefinition> _things = new Dictionary<int, ThingDefinition>();
        private static readonly Dictionary<string, int> _nameToId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, ThingDefinition> _nameToThing = new Dictionary<string, ThingDefinition>(StringComparer.OrdinalIgnoreCase);

        static ThingDatabase()
        {
            RegisterDoomThings();
            RegisterDoom2Things();
            RegisterBoomThings();
            RegisterMBF21Things();
        }

        /// <summary>
        /// Looks up a thing definition by its DoomEd number.
        /// </summary>
        /// <param name="doomEdNum">The DoomEd number to look up.</param>
        /// <returns>The thing definition, or null if not found.</returns>
        public static ThingDefinition? Get(int doomEdNum)
        {
            return _things.TryGetValue(doomEdNum, out var def) ? def : null;
        }

        /// <summary>
        /// Looks up a thing definition by class name or legacy name.
        /// </summary>
        /// <param name="name">The class name or legacy name to look up.</param>
        /// <returns>The thing definition, or null if not found.</returns>
        public static ThingDefinition? GetByName(string name)
        {
            return _nameToThing.TryGetValue(name, out var def) ? def : null;
        }

        /// <summary>
        /// Gets all thing definitions in a given category.
        /// </summary>
        /// <param name="category">The category to filter by.</param>
        /// <returns>All things in the specified category.</returns>
        public static IEnumerable<ThingDefinition> GetByCategory(ThingCategory category)
        {
            return _things.Values.Where(t => t.Category == category);
        }

        /// <summary>
        /// Gets all thing definitions in a given category for a specific game.
        /// </summary>
        /// <param name="category">The category to filter by.</param>
        /// <param name="game">The game type to filter by (uses flag matching).</param>
        /// <returns>All things matching both category and game.</returns>
        public static IEnumerable<ThingDefinition> GetByCategory(ThingCategory category, GameType game)
        {
            return _things.Values.Where(t => t.Category == category && (t.Game & game) != 0);
        }

        /// <summary>
        /// Gets all monster thing definitions.
        /// </summary>
        /// <returns>All things in the Monster category.</returns>
        public static IEnumerable<ThingDefinition> GetMonsters()
        {
            return GetByCategory(ThingCategory.Monster);
        }

        /// <summary>
        /// Gets all weapon thing definitions.
        /// </summary>
        /// <returns>All things in the Weapon category.</returns>
        public static IEnumerable<ThingDefinition> GetWeapons()
        {
            return GetByCategory(ThingCategory.Weapon);
        }

        /// <summary>
        /// Checks if a class name is a known base game actor.
        /// </summary>
        /// <param name="className">The class name to check.</param>
        /// <returns>True if the name matches a registered thing.</returns>
        public static bool IsKnownActor(string className)
        {
            return _nameToThing.ContainsKey(className);
        }

        private static void Register(ThingDefinition def)
        {
            _things[def.DoomEdNum] = def;
            if (!string.IsNullOrEmpty(def.ClassName))
            {
                _nameToId[def.ClassName] = def.DoomEdNum;
                _nameToThing[def.ClassName] = def;
            }
            if (def.LegacyName != null)
            {
                _nameToId[def.LegacyName] = def.DoomEdNum;
                _nameToThing[def.LegacyName] = def;
            }
        }

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
                Flags = ThingBehaviorFlags.IsMonster | ThingBehaviorFlags.Countkill | ThingBehaviorFlags.Solid | ThingBehaviorFlags.Shootable,
                Game = GameType.AllDoom,
                Health = 20, Speed = 8, Radius = 20, Height = 56, PainChance = 200,
                SpawnSprite = "POSS"
            });
            Register(new ThingDefinition
            {
                DoomEdNum = 9,
                ClassName = "ShotgunGuy",
                DisplayName = "Former Sergeant",
                Category = ThingCategory.Monster,
                Flags = ThingBehaviorFlags.IsMonster | ThingBehaviorFlags.Countkill | ThingBehaviorFlags.Solid | ThingBehaviorFlags.Shootable,
                Game = GameType.AllDoom,
                Health = 30, Speed = 8, Radius = 20, Height = 56, PainChance = 170,
                SpawnSprite = "SPOS"
            });
            Register(new ThingDefinition
            {
                DoomEdNum = 3001,
                ClassName = "DoomImp",
                LegacyName = "Imp",
                DisplayName = "Imp",
                Category = ThingCategory.Monster,
                Flags = ThingBehaviorFlags.IsMonster | ThingBehaviorFlags.Countkill | ThingBehaviorFlags.Solid | ThingBehaviorFlags.Shootable,
                Game = GameType.AllDoom,
                Health = 60, Speed = 8, Radius = 20, Height = 56, PainChance = 200,
                SpawnSprite = "TROO"
            });
            Register(new ThingDefinition
            {
                DoomEdNum = 3002,
                ClassName = "Demon",
                LegacyName = "Pinky",
                DisplayName = "Demon",
                Category = ThingCategory.Monster,
                Flags = ThingBehaviorFlags.IsMonster | ThingBehaviorFlags.Countkill | ThingBehaviorFlags.Solid | ThingBehaviorFlags.Shootable,
                Game = GameType.AllDoom,
                Health = 150, Speed = 10, Radius = 30, Height = 56, PainChance = 180,
                SpawnSprite = "SARG"
            });
            Register(new ThingDefinition
            {
                DoomEdNum = 58,
                ClassName = "Spectre",
                DisplayName = "Spectre",
                Category = ThingCategory.Monster,
                Flags = ThingBehaviorFlags.IsMonster | ThingBehaviorFlags.Countkill | ThingBehaviorFlags.Solid | ThingBehaviorFlags.Shootable | ThingBehaviorFlags.Translucent,
                Game = GameType.AllDoom,
                Health = 150, Speed = 10, Radius = 30, Height = 56, PainChance = 180,
                SpawnSprite = "SARG"
            });
            Register(new ThingDefinition
            {
                DoomEdNum = 3006,
                ClassName = "LostSoul",
                DisplayName = "Lost Soul",
                Category = ThingCategory.Monster,
                Flags = ThingBehaviorFlags.IsMonster | ThingBehaviorFlags.Countkill | ThingBehaviorFlags.Float | ThingBehaviorFlags.NoGravity | ThingBehaviorFlags.Shootable,
                Game = GameType.AllDoom,
                Health = 100, Speed = 8, Radius = 16, Height = 56, Damage = 3, PainChance = 256,
                SpawnSprite = "SKUL"
            });
            Register(new ThingDefinition
            {
                DoomEdNum = 3005,
                ClassName = "Cacodemon",
                DisplayName = "Cacodemon",
                Category = ThingCategory.Monster,
                Flags = ThingBehaviorFlags.IsMonster | ThingBehaviorFlags.Countkill | ThingBehaviorFlags.Float | ThingBehaviorFlags.NoGravity | ThingBehaviorFlags.Solid | ThingBehaviorFlags.Shootable,
                Game = GameType.AllDoom,
                Health = 400, Speed = 8, Radius = 31, Height = 56, PainChance = 128,
                SpawnSprite = "HEAD"
            });
            Register(new ThingDefinition
            {
                DoomEdNum = 3003,
                ClassName = "BaronOfHell",
                LegacyName = "Baron",
                DisplayName = "Baron of Hell",
                Category = ThingCategory.Monster,
                Flags = ThingBehaviorFlags.IsMonster | ThingBehaviorFlags.Countkill | ThingBehaviorFlags.Solid | ThingBehaviorFlags.Shootable,
                Game = GameType.AllDoom,
                Health = 1000, Speed = 8, Radius = 24, Height = 64, PainChance = 50,
                SpawnSprite = "BOSS"
            });
            Register(new ThingDefinition
            {
                DoomEdNum = 16,
                ClassName = "Cyberdemon",
                DisplayName = "Cyberdemon",
                Category = ThingCategory.Boss,
                Flags = ThingBehaviorFlags.IsMonster | ThingBehaviorFlags.Countkill | ThingBehaviorFlags.Solid | ThingBehaviorFlags.Shootable,
                Game = GameType.AllDoom,
                Health = 4000, Speed = 16, Radius = 40, Height = 110, PainChance = 20,
                SpawnSprite = "CYBR"
            });
            Register(new ThingDefinition
            {
                DoomEdNum = 7,
                ClassName = "SpiderMastermind",
                DisplayName = "Spider Mastermind",
                Category = ThingCategory.Boss,
                Flags = ThingBehaviorFlags.IsMonster | ThingBehaviorFlags.Countkill | ThingBehaviorFlags.Solid | ThingBehaviorFlags.Shootable,
                Game = GameType.AllDoom,
                Health = 3000, Speed = 12, Radius = 128, Height = 100, PainChance = 40,
                SpawnSprite = "SPID"
            });

            // Weapons (intrinsic - no DoomEd number, registered with 0 for name lookup)
            Register(new ThingDefinition
            {
                DoomEdNum = 0,
                ClassName = "Fist",
                DisplayName = "Fist",
                Category = ThingCategory.Weapon,
                Flags = ThingBehaviorFlags.Pickup,
                Game = GameType.AllDoom,
                SpawnSprite = "PUNG",
                Slot = 1
            });
            Register(new ThingDefinition
            {
                DoomEdNum = 2005,
                ClassName = "Chainsaw",
                DisplayName = "Chainsaw",
                Category = ThingCategory.Weapon,
                Flags = ThingBehaviorFlags.Pickup,
                Game = GameType.AllDoom,
                SpawnSprite = "CSAW",
                Slot = 1
            });
            Register(new ThingDefinition
            {
                DoomEdNum = 0,
                ClassName = "Pistol",
                DisplayName = "Pistol",
                Category = ThingCategory.Weapon,
                Flags = ThingBehaviorFlags.Pickup,
                Game = GameType.AllDoom,
                SpawnSprite = "PISG",
                Slot = 2
            });
            Register(new ThingDefinition
            {
                DoomEdNum = 2001,
                ClassName = "Shotgun",
                DisplayName = "Shotgun",
                Category = ThingCategory.Weapon,
                Flags = ThingBehaviorFlags.Pickup,
                Game = GameType.AllDoom,
                SpawnSprite = "SHOT",
                Slot = 3
            });
            Register(new ThingDefinition
            {
                DoomEdNum = 2002,
                ClassName = "Chaingun",
                DisplayName = "Chaingun",
                Category = ThingCategory.Weapon,
                Flags = ThingBehaviorFlags.Pickup,
                Game = GameType.AllDoom,
                SpawnSprite = "MGUN",
                Slot = 4
            });
            Register(new ThingDefinition
            {
                DoomEdNum = 2003,
                ClassName = "RocketLauncher",
                DisplayName = "Rocket Launcher",
                Category = ThingCategory.Weapon,
                Flags = ThingBehaviorFlags.Pickup,
                Game = GameType.AllDoom,
                SpawnSprite = "LAUN",
                Slot = 5
            });
            Register(new ThingDefinition
            {
                DoomEdNum = 2004,
                ClassName = "PlasmaRifle",
                DisplayName = "Plasma Rifle",
                Category = ThingCategory.Weapon,
                Flags = ThingBehaviorFlags.Pickup,
                Game = GameType.AllDoom,
                SpawnSprite = "PLAS",
                Slot = 6
            });
            Register(new ThingDefinition
            {
                DoomEdNum = 2006,
                ClassName = "BFG9000",
                DisplayName = "BFG 9000",
                Category = ThingCategory.Weapon,
                Flags = ThingBehaviorFlags.Pickup,
                Game = GameType.AllDoom,
                SpawnSprite = "BFUG",
                Slot = 7
            });

            // Ammo
            Register(new ThingDefinition { DoomEdNum = 2007, ClassName = "Clip", DisplayName = "Ammo Clip", Category = ThingCategory.Ammo, Flags = ThingBehaviorFlags.Pickup, Game = GameType.AllDoom, SpawnSprite = "CLIP" });
            Register(new ThingDefinition { DoomEdNum = 2048, ClassName = "BoxOfAmmo", LegacyName = "Box of Ammo", DisplayName = "Box of Bullets", Category = ThingCategory.Ammo, Flags = ThingBehaviorFlags.Pickup, Game = GameType.AllDoom, SpawnSprite = "AMMO" });
            Register(new ThingDefinition { DoomEdNum = 2008, ClassName = "Shell", DisplayName = "Shotgun Shells", Category = ThingCategory.Ammo, Flags = ThingBehaviorFlags.Pickup, Game = GameType.AllDoom, SpawnSprite = "SHEL" });
            Register(new ThingDefinition { DoomEdNum = 2049, ClassName = "ShellBox", DisplayName = "Box of Shells", Category = ThingCategory.Ammo, Flags = ThingBehaviorFlags.Pickup, Game = GameType.AllDoom, SpawnSprite = "SBOX" });
            Register(new ThingDefinition { DoomEdNum = 2010, ClassName = "RocketAmmo", DisplayName = "Rocket", Category = ThingCategory.Ammo, Flags = ThingBehaviorFlags.Pickup, Game = GameType.AllDoom, SpawnSprite = "ROCK" });
            Register(new ThingDefinition { DoomEdNum = 2046, ClassName = "RocketBox", DisplayName = "Box of Rockets", Category = ThingCategory.Ammo, Flags = ThingBehaviorFlags.Pickup, Game = GameType.AllDoom, SpawnSprite = "BROK" });
            Register(new ThingDefinition { DoomEdNum = 2047, ClassName = "Cell", DisplayName = "Energy Cell", Category = ThingCategory.Ammo, Flags = ThingBehaviorFlags.Pickup, Game = GameType.AllDoom, SpawnSprite = "CELL" });
            Register(new ThingDefinition { DoomEdNum = 17, ClassName = "CellPack", DisplayName = "Energy Cell Pack", Category = ThingCategory.Ammo, Flags = ThingBehaviorFlags.Pickup, Game = GameType.AllDoom, SpawnSprite = "CELP" });

            // Health & Armor
            Register(new ThingDefinition { DoomEdNum = 2011, ClassName = "Stimpack", DisplayName = "Stimpack", Category = ThingCategory.Health, Flags = ThingBehaviorFlags.Pickup, Game = GameType.AllDoom, SpawnSprite = "STIM" });
            Register(new ThingDefinition { DoomEdNum = 2012, ClassName = "Medikit", DisplayName = "Medikit", Category = ThingCategory.Health, Flags = ThingBehaviorFlags.Pickup, Game = GameType.AllDoom, SpawnSprite = "MEDI" });
            Register(new ThingDefinition { DoomEdNum = 2014, ClassName = "HealthBonus", DisplayName = "Health Bonus", Category = ThingCategory.Health, Flags = ThingBehaviorFlags.Pickup | ThingBehaviorFlags.CountItem, Game = GameType.AllDoom, SpawnSprite = "BON1" });
            Register(new ThingDefinition { DoomEdNum = 2015, ClassName = "ArmorBonus", DisplayName = "Armor Bonus", Category = ThingCategory.Armor, Flags = ThingBehaviorFlags.Pickup | ThingBehaviorFlags.CountItem, Game = GameType.AllDoom, SpawnSprite = "BON2" });
            Register(new ThingDefinition { DoomEdNum = 2018, ClassName = "GreenArmor", DisplayName = "Green Armor", Category = ThingCategory.Armor, Flags = ThingBehaviorFlags.Pickup, Game = GameType.AllDoom, SpawnSprite = "ARM1" });
            Register(new ThingDefinition { DoomEdNum = 2019, ClassName = "BlueArmor", DisplayName = "Blue Armor", Category = ThingCategory.Armor, Flags = ThingBehaviorFlags.Pickup, Game = GameType.AllDoom, SpawnSprite = "ARM2" });

            // Powerups
            Register(new ThingDefinition { DoomEdNum = 2022, ClassName = "InvulnerabilitySphere", DisplayName = "Invulnerability", Category = ThingCategory.Powerup, Flags = ThingBehaviorFlags.Pickup | ThingBehaviorFlags.CountItem, Game = GameType.AllDoom, SpawnSprite = "PINV" });
            Register(new ThingDefinition { DoomEdNum = 2023, ClassName = "Berserk", DisplayName = "Berserk", Category = ThingCategory.Powerup, Flags = ThingBehaviorFlags.Pickup | ThingBehaviorFlags.CountItem, Game = GameType.AllDoom, SpawnSprite = "PSTR" });
            Register(new ThingDefinition { DoomEdNum = 2024, ClassName = "BlurSphere", DisplayName = "Partial Invisibility", Category = ThingCategory.Powerup, Flags = ThingBehaviorFlags.Pickup | ThingBehaviorFlags.CountItem, Game = GameType.AllDoom, SpawnSprite = "PINS" });
            Register(new ThingDefinition { DoomEdNum = 2025, ClassName = "RadSuit", DisplayName = "Radiation Suit", Category = ThingCategory.Powerup, Flags = ThingBehaviorFlags.Pickup, Game = GameType.AllDoom, SpawnSprite = "SUIT" });
            Register(new ThingDefinition { DoomEdNum = 2026, ClassName = "Allmap", DisplayName = "Computer Map", Category = ThingCategory.Powerup, Flags = ThingBehaviorFlags.Pickup | ThingBehaviorFlags.CountItem, Game = GameType.AllDoom, SpawnSprite = "PMAP" });
            Register(new ThingDefinition { DoomEdNum = 2045, ClassName = "Infrared", DisplayName = "Light Amplification Visor", Category = ThingCategory.Powerup, Flags = ThingBehaviorFlags.Pickup | ThingBehaviorFlags.CountItem, Game = GameType.AllDoom, SpawnSprite = "PVIS" });
            Register(new ThingDefinition { DoomEdNum = 2013, ClassName = "Soulsphere", DisplayName = "Soulsphere", Category = ThingCategory.Powerup, Flags = ThingBehaviorFlags.Pickup | ThingBehaviorFlags.CountItem, Game = GameType.AllDoom, SpawnSprite = "SOUL" });

            // Keys
            Register(new ThingDefinition { DoomEdNum = 5, ClassName = "BlueCard", DisplayName = "Blue Keycard", Category = ThingCategory.Key, Flags = ThingBehaviorFlags.Pickup, Game = GameType.AllDoom, SpawnSprite = "BKEY" });
            Register(new ThingDefinition { DoomEdNum = 6, ClassName = "YellowCard", DisplayName = "Yellow Keycard", Category = ThingCategory.Key, Flags = ThingBehaviorFlags.Pickup, Game = GameType.AllDoom, SpawnSprite = "YKEY" });
            Register(new ThingDefinition { DoomEdNum = 13, ClassName = "RedCard", DisplayName = "Red Keycard", Category = ThingCategory.Key, Flags = ThingBehaviorFlags.Pickup, Game = GameType.AllDoom, SpawnSprite = "RKEY" });
            Register(new ThingDefinition { DoomEdNum = 40, ClassName = "BlueSkull", DisplayName = "Blue Skull Key", Category = ThingCategory.Key, Flags = ThingBehaviorFlags.Pickup, Game = GameType.AllDoom, SpawnSprite = "BSKU" });
            Register(new ThingDefinition { DoomEdNum = 39, ClassName = "YellowSkull", DisplayName = "Yellow Skull Key", Category = ThingCategory.Key, Flags = ThingBehaviorFlags.Pickup, Game = GameType.AllDoom, SpawnSprite = "YSKU" });
            Register(new ThingDefinition { DoomEdNum = 38, ClassName = "RedSkull", DisplayName = "Red Skull Key", Category = ThingCategory.Key, Flags = ThingBehaviorFlags.Pickup, Game = GameType.AllDoom, SpawnSprite = "RSKU" });
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
                Flags = ThingBehaviorFlags.IsMonster | ThingBehaviorFlags.Countkill | ThingBehaviorFlags.Solid | ThingBehaviorFlags.Shootable,
                Game = GameType.Doom2,
                Health = 70, Speed = 8, Radius = 20, Height = 56, PainChance = 170,
                SpawnSprite = "CPOS"
            });
            Register(new ThingDefinition
            {
                DoomEdNum = 66,
                ClassName = "Revenant",
                DisplayName = "Revenant",
                Category = ThingCategory.Monster,
                Flags = ThingBehaviorFlags.IsMonster | ThingBehaviorFlags.Countkill | ThingBehaviorFlags.Solid | ThingBehaviorFlags.Shootable,
                Game = GameType.Doom2,
                Health = 300, Speed = 10, Radius = 20, Height = 64, PainChance = 100,
                SpawnSprite = "SKEL"
            });
            Register(new ThingDefinition
            {
                DoomEdNum = 67,
                ClassName = "Fatso",
                LegacyName = "Mancubus",
                DisplayName = "Mancubus",
                Category = ThingCategory.Monster,
                Flags = ThingBehaviorFlags.IsMonster | ThingBehaviorFlags.Countkill | ThingBehaviorFlags.Solid | ThingBehaviorFlags.Shootable,
                Game = GameType.Doom2,
                Health = 600, Speed = 8, Radius = 48, Height = 64, PainChance = 80,
                SpawnSprite = "FATT"
            });
            Register(new ThingDefinition
            {
                DoomEdNum = 68,
                ClassName = "Arachnotron",
                DisplayName = "Arachnotron",
                Category = ThingCategory.Monster,
                Flags = ThingBehaviorFlags.IsMonster | ThingBehaviorFlags.Countkill | ThingBehaviorFlags.Solid | ThingBehaviorFlags.Shootable,
                Game = GameType.Doom2,
                Health = 500, Speed = 12, Radius = 64, Height = 64, PainChance = 128,
                SpawnSprite = "BSPI"
            });
            Register(new ThingDefinition
            {
                DoomEdNum = 69,
                ClassName = "HellKnight",
                DisplayName = "Hell Knight",
                Category = ThingCategory.Monster,
                Flags = ThingBehaviorFlags.IsMonster | ThingBehaviorFlags.Countkill | ThingBehaviorFlags.Solid | ThingBehaviorFlags.Shootable,
                Game = GameType.Doom2,
                Health = 500, Speed = 8, Radius = 24, Height = 64, PainChance = 50,
                SpawnSprite = "BOS2"
            });
            Register(new ThingDefinition
            {
                DoomEdNum = 71,
                ClassName = "PainElemental",
                DisplayName = "Pain Elemental",
                Category = ThingCategory.Monster,
                Flags = ThingBehaviorFlags.IsMonster | ThingBehaviorFlags.Countkill | ThingBehaviorFlags.Float | ThingBehaviorFlags.NoGravity | ThingBehaviorFlags.Solid | ThingBehaviorFlags.Shootable,
                Game = GameType.Doom2,
                Health = 400, Speed = 8, Radius = 31, Height = 56, PainChance = 128,
                SpawnSprite = "PAIN"
            });
            Register(new ThingDefinition
            {
                DoomEdNum = 64,
                ClassName = "Archvile",
                LegacyName = "ArchVile",
                DisplayName = "Arch-vile",
                Category = ThingCategory.Monster,
                Flags = ThingBehaviorFlags.IsMonster | ThingBehaviorFlags.Countkill | ThingBehaviorFlags.Solid | ThingBehaviorFlags.Shootable,
                Game = GameType.Doom2,
                Health = 700, Speed = 15, Radius = 20, Height = 56, PainChance = 10,
                SpawnSprite = "VILE"
            });
            Register(new ThingDefinition
            {
                DoomEdNum = 72,
                ClassName = "CommanderKeen",
                DisplayName = "Commander Keen",
                Category = ThingCategory.Monster,
                Flags = ThingBehaviorFlags.Solid | ThingBehaviorFlags.Shootable | ThingBehaviorFlags.SpawnCeiling | ThingBehaviorFlags.NoGravity,
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
                Flags = ThingBehaviorFlags.IsMonster | ThingBehaviorFlags.Countkill | ThingBehaviorFlags.Solid | ThingBehaviorFlags.Shootable,
                Game = GameType.Doom2,
                Health = 50, Speed = 8, Radius = 20, Height = 56, PainChance = 170,
                SpawnSprite = "SSWV"
            });

            // DOOM II exclusive weapons
            Register(new ThingDefinition
            {
                DoomEdNum = 82,
                ClassName = "SuperShotgun",
                DisplayName = "Super Shotgun",
                Category = ThingCategory.Weapon,
                Flags = ThingBehaviorFlags.Pickup,
                Game = GameType.Doom2,
                SpawnSprite = "SGN2",
                Slot = 3
            });

            // DOOM II exclusive bosses
            Register(new ThingDefinition
            {
                DoomEdNum = 88,
                ClassName = "BossBrain",
                DisplayName = "Boss Brain",
                Category = ThingCategory.Boss,
                Flags = ThingBehaviorFlags.Solid | ThingBehaviorFlags.Shootable,
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

            // DOOM II exclusive powerup
            Register(new ThingDefinition { DoomEdNum = 83, ClassName = "Megasphere", DisplayName = "Megasphere", Category = ThingCategory.Powerup, Flags = ThingBehaviorFlags.Pickup | ThingBehaviorFlags.CountItem, Game = GameType.Doom2, SpawnSprite = "MEGA" });
        }

        private static void RegisterBoomThings()
        {
            // Boom extensions - stub for future expansion
        }

        private static void RegisterMBF21Things()
        {
            // MBF21 extensions - stub for future expansion
        }
    }
}
