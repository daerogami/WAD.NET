using System.Linq;
using WAD.NET.Definitions;
using WAD.NET.Definitions.GameData;
using Xunit;

namespace WAD.NET.Tests
{
    public class GameConstantsTests
    {
        // --- ThingDatabase ---

        [Fact]
        public void ShouldLookupZombiemanByDoomEdNum()
        {
            var thing = ThingDatabase.Get(3004);

            Assert.NotNull(thing);
            Assert.Equal("ZombieMan", thing.ClassName);
            Assert.Equal(ThingCategory.Monster, thing.Category);
            Assert.True(thing.Flags.HasFlag(ThingBehaviorFlags.IsMonster));
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
        public void ShouldLookupThingByLegacyName()
        {
            var thing = ThingDatabase.GetByName("Mancubus");

            Assert.NotNull(thing);
            Assert.Equal(67, thing.DoomEdNum);
            Assert.Equal("Fatso", thing.ClassName);
        }

        [Fact]
        public void ShouldIdentifyPlayerStarts()
        {
            var playerStarts = new[] { 1, 2, 3, 4, 11 };

            foreach (var doomEdNum in playerStarts)
            {
                var thing = ThingDatabase.Get(doomEdNum);
                Assert.NotNull(thing);
                Assert.Equal(ThingCategory.Player, thing.Category);
            }
        }

        [Fact]
        public void ShouldReturnNullForUnknownDoomEdNum()
        {
            var thing = ThingDatabase.Get(99999);
            Assert.Null(thing);
        }

        [Fact]
        public void ShouldReturnNullForUnknownName()
        {
            var thing = ThingDatabase.GetByName("NonExistentMonster");
            Assert.Null(thing);
        }

        [Fact]
        public void ShouldGetMonsters()
        {
            var monsters = ThingDatabase.GetMonsters().ToList();

            Assert.NotEmpty(monsters);
            Assert.All(monsters, m => Assert.Equal(ThingCategory.Monster, m.Category));
            Assert.Contains(monsters, m => m.ClassName == "ZombieMan");
            Assert.Contains(monsters, m => m.ClassName == "DoomImp");
            Assert.Contains(monsters, m => m.ClassName == "Revenant");
        }

        [Fact]
        public void ShouldGetWeapons()
        {
            var weapons = ThingDatabase.GetWeapons().ToList();

            Assert.NotEmpty(weapons);
            Assert.All(weapons, w => Assert.Equal(ThingCategory.Weapon, w.Category));
            Assert.Contains(weapons, w => w.ClassName == "Shotgun");
            Assert.Contains(weapons, w => w.ClassName == "BFG9000");
        }

        [Fact]
        public void ShouldGetByCategoryAndGameType()
        {
            var doom2Monsters = ThingDatabase.GetByCategory(ThingCategory.Monster, GameType.Doom2).ToList();

            Assert.Contains(doom2Monsters, m => m.ClassName == "Revenant");
            Assert.Contains(doom2Monsters, m => m.ClassName == "Archvile");
            // AllDoom monsters should also match Doom2
            Assert.Contains(doom2Monsters, m => m.ClassName == "ZombieMan");
        }

        [Fact]
        public void ShouldFilterDoom2OnlyMonstersFromDoom1()
        {
            var doom1Monsters = ThingDatabase.GetByCategory(ThingCategory.Monster, GameType.Doom).ToList();

            // Doom2-only monsters should not appear in Doom1 results
            Assert.DoesNotContain(doom1Monsters, m => m.ClassName == "Revenant");
            Assert.DoesNotContain(doom1Monsters, m => m.ClassName == "Archvile");
            // AllDoom monsters should appear
            Assert.Contains(doom1Monsters, m => m.ClassName == "ZombieMan");
        }

        [Fact]
        public void ShouldIdentifyKnownActors()
        {
            Assert.True(ThingDatabase.IsKnownActor("ZombieMan"));
            Assert.True(ThingDatabase.IsKnownActor("Cyberdemon"));
            Assert.True(ThingDatabase.IsKnownActor("Mancubus")); // Legacy name
            Assert.False(ThingDatabase.IsKnownActor("NonExistentActor"));
        }

        [Fact]
        public void WeaponsShouldHaveSlotValues()
        {
            var shotgun = ThingDatabase.GetByName("Shotgun");
            Assert.NotNull(shotgun);
            Assert.Equal(3, shotgun.Slot);

            var bfg = ThingDatabase.GetByName("BFG9000");
            Assert.NotNull(bfg);
            Assert.Equal(7, bfg.Slot);

            var fist = ThingDatabase.GetByName("Fist");
            Assert.NotNull(fist);
            Assert.Equal(1, fist.Slot);
        }

        [Fact]
        public void MonstersShouldHaveSpawnSprites()
        {
            var monsters = ThingDatabase.GetMonsters().ToList();

            foreach (var monster in monsters)
            {
                Assert.False(string.IsNullOrEmpty(monster.SpawnSprite),
                    $"Monster {monster.ClassName} should have a SpawnSprite");
            }
        }

        [Fact]
        public void ShouldLookupNameCaseInsensitively()
        {
            var thing1 = ThingDatabase.GetByName("zombieman");
            var thing2 = ThingDatabase.GetByName("ZOMBIEMAN");

            Assert.NotNull(thing1);
            Assert.NotNull(thing2);
            Assert.Equal(thing1.DoomEdNum, thing2.DoomEdNum);
        }

        // --- SectorDatabase ---

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
                Assert.True(SectorDatabase.IsDamaging(special),
                    $"Sector special {special} should be damaging");
            }

            Assert.False(SectorDatabase.IsDamaging(9));  // Secret, not damage
            Assert.False(SectorDatabase.IsDamaging(0));   // None
            Assert.False(SectorDatabase.IsDamaging(1));   // Light only
        }

        [Fact]
        public void ShouldIdentifyLightEffects()
        {
            Assert.True(SectorDatabase.IsLightEffect(1));  // Blink random
            Assert.True(SectorDatabase.IsLightEffect(2));  // Blink 0.5s
            Assert.True(SectorDatabase.IsLightEffect(3));  // Blink 1s
            Assert.True(SectorDatabase.IsLightEffect(4));  // Damage + light
            Assert.True(SectorDatabase.IsLightEffect(8));  // Oscillate
            Assert.True(SectorDatabase.IsLightEffect(17)); // Fire flicker

            Assert.False(SectorDatabase.IsLightEffect(9));  // Secret
            Assert.False(SectorDatabase.IsLightEffect(5));  // Damage only
        }

        [Fact]
        public void ShouldLookupSectorSpecial()
        {
            var special = SectorDatabase.Get(4);

            Assert.NotNull(special);
            Assert.Equal(SectorSpecialType.Damage, special.Type);
            Assert.Equal(20, special.DamageAmount);
            Assert.Equal(LightEffect.BlinkHalf, special.LightEffect);
        }

        [Fact]
        public void ShouldReturnNullForUnknownSectorSpecial()
        {
            var special = SectorDatabase.Get(999);
            Assert.Null(special);
        }

        // --- LinedefDatabase ---

        [Fact]
        public void ShouldIdentifyExitLinedefs()
        {
            Assert.True(LinedefDatabase.IsExit(11));   // Switch exit
            Assert.True(LinedefDatabase.IsExit(51));   // Secret exit switch
            Assert.True(LinedefDatabase.IsExit(52));   // Walk exit
            Assert.True(LinedefDatabase.IsExit(124));  // Secret exit walk

            Assert.False(LinedefDatabase.IsExit(1));   // Door
            Assert.False(LinedefDatabase.IsExit(39));  // Teleport
        }

        [Fact]
        public void ShouldIdentifyTeleporterLinedefs()
        {
            Assert.True(LinedefDatabase.IsTeleporter(39));   // Teleport walk
            Assert.True(LinedefDatabase.IsTeleporter(97));   // Teleport retrigger
            Assert.True(LinedefDatabase.IsTeleporter(125));  // Monster teleport
            Assert.True(LinedefDatabase.IsTeleporter(126));  // Monster teleport retrigger

            Assert.False(LinedefDatabase.IsTeleporter(11));  // Exit
            Assert.False(LinedefDatabase.IsTeleporter(1));   // Door
        }

        [Fact]
        public void ShouldLookupLinedefAction()
        {
            var action = LinedefDatabase.Get(1);

            Assert.NotNull(action);
            Assert.Equal(LinedefActionType.Door, action.ActionType);
            Assert.Equal(TriggerType.Push, action.Trigger);
            Assert.True(action.Repeatable);
        }

        [Fact]
        public void ShouldReturnNullForUnknownLinedefAction()
        {
            var action = LinedefDatabase.Get(9999);
            Assert.Null(action);
        }

        [Fact]
        public void ShouldReturnFalseForUnknownLinedefExit()
        {
            Assert.False(LinedefDatabase.IsExit(9999));
        }

        // --- SpriteConstants ---

        [Fact]
        public void ShouldIdentifyInvisibleSprites()
        {
            Assert.True(SpriteConstants.IsInvisible("TNT1"));
            Assert.True(SpriteConstants.IsInvisible("----"));
            Assert.True(SpriteConstants.IsInvisible("tnt1")); // case insensitive
            Assert.False(SpriteConstants.IsInvisible("POSS"));
            Assert.False(SpriteConstants.IsInvisible(""));
        }

        [Fact]
        public void ShouldValidateSpriteName()
        {
            Assert.True(SpriteConstants.IsValidSpriteName("POSS"));
            Assert.True(SpriteConstants.IsValidSpriteName("TNT1"));
            Assert.True(SpriteConstants.IsValidSpriteName("----"));
            Assert.False(SpriteConstants.IsValidSpriteName("POS"));   // too short
            Assert.False(SpriteConstants.IsValidSpriteName("POSSX")); // too long
            Assert.False(SpriteConstants.IsValidSpriteName(""));
            Assert.False(SpriteConstants.IsValidSpriteName(null!));
        }

        // --- TextureConstants ---

        [Fact]
        public void ShouldIdentifyNullTextures()
        {
            Assert.True(TextureConstants.IsNull("-"));
            Assert.True(TextureConstants.IsNull(""));
            Assert.True(TextureConstants.IsNull(null!));
            Assert.False(TextureConstants.IsNull("STARTAN2"));
        }

        // --- MapNamePatterns ---

        [Fact]
        public void ShouldParseDoomMapName()
        {
            var map = MapNamePatterns.Parse("E1M1");

            Assert.NotNull(map);
            Assert.Equal(MapNameFormat.Doom, map.Format);
            Assert.Equal(1, map.Episode);
            Assert.Equal(1, map.Map);
        }

        [Fact]
        public void ShouldParseDoomMapNameCaseInsensitive()
        {
            var map = MapNamePatterns.Parse("e3m5");

            Assert.NotNull(map);
            Assert.Equal(MapNameFormat.Doom, map.Format);
            Assert.Equal(3, map.Episode);
            Assert.Equal(5, map.Map);
        }

        [Fact]
        public void ShouldParseDoom2MapName()
        {
            var map = MapNamePatterns.Parse("MAP23");

            Assert.NotNull(map);
            Assert.Equal(MapNameFormat.Doom2, map.Format);
            Assert.Null(map.Episode);
            Assert.Equal(23, map.Map);
        }

        [Fact]
        public void ShouldParseDoom2MapNameCaseInsensitive()
        {
            var map = MapNamePatterns.Parse("map01");

            Assert.NotNull(map);
            Assert.Equal(MapNameFormat.Doom2, map.Format);
            Assert.Equal(1, map.Map);
        }

        [Fact]
        public void ShouldParseCustomMapName()
        {
            var map = MapNamePatterns.Parse("MYMAP01");

            Assert.NotNull(map);
            Assert.Equal(MapNameFormat.Custom, map.Format);
            Assert.Equal("MYMAP01", map.RawName);
            Assert.Null(map.Episode);
            Assert.Null(map.Map);
        }

        [Fact]
        public void ShouldReturnNullForEmptyMapName()
        {
            Assert.Null(MapNamePatterns.Parse(""));
            Assert.Null(MapNamePatterns.Parse(null!));
        }

        [Fact]
        public void ShouldCalculateSortOrder()
        {
            var e1m1 = MapNamePatterns.Parse("E1M1");
            var e2m3 = MapNamePatterns.Parse("E2M3");
            var map05 = MapNamePatterns.Parse("MAP05");

            Assert.Equal(11, e1m1!.SortOrder);
            Assert.Equal(23, e2m3!.SortOrder);
            Assert.Equal(5, map05!.SortOrder);
        }

        // --- BinaryConstants ---

        [Fact]
        public void BinaryConstantsShouldHaveCorrectValues()
        {
            Assert.Equal("IWAD", BinaryConstants.IwadMarker);
            Assert.Equal("PWAD", BinaryConstants.PwadMarker);
            Assert.Equal(128, BinaryConstants.BlockmapBlockSize);
            Assert.Equal((ushort)0xFFFF, BinaryConstants.BlockmapTerminator);
            Assert.Equal((ushort)0x8000, BinaryConstants.SubsectorFlag);
            Assert.Equal(4000, BinaryConstants.EndoomSize);
        }
    }
}
