using System;
using System.Linq;
using System.Reflection;
using Xunit;
using WAD.NET;
using WAD.NET.Definitions.Classic.Map;
using WAD.NET.Definitions.Hexen;
using WAD.NET.SourcePorts.MBF21;
using WAD.NET.SourcePorts.Boom;

namespace WAD.NET.Tests
{
    /// <summary>
    /// Tests for entity type definitions including ThingType, LinedefType enums,
    /// and attribute decorations (HexenFlagAttribute, BoomFlagAttribute, etc.).
    /// </summary>
    public class EntityTypeDefinitionsTests
    {
        #region ThingType Enum Tests

        [Fact]
        public void ThingType_ShouldContainPlayerSpawn()
        {
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.PLAYER));
            Assert.Equal(0, (int)ThingType.PLAYER);
        }

        [Fact]
        public void ThingType_ShouldContainCommonMonsters()
        {
            // Verify common DOOM monsters are defined
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.TROOPER));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.SHOTGUY));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.IMP));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.DEMON));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.SPECTRE));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.CACODEMON));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.BARONOFHELL));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.CYBERDEMON));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.SPIDERMASTERMIND));
        }

        [Fact]
        public void ThingType_ShouldContainDoom2Monsters()
        {
            // DOOM 2 specific monsters
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.ARCHVILE));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.REVENANT));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.MANCUBUS));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.CHAINGUY));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.ARACHNOTRON));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.PAINELEMENTAL));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.HELLKNIGHT));
        }

        [Fact]
        public void ThingType_ShouldContainWeapons()
        {
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.SHOTGUN));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.SUPERSHOTGUN));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.CHAINGUN));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.RLAUNCHER));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.PLASMAGUN));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.BFG));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.CHAINSAW));
        }

        [Fact]
        public void ThingType_ShouldContainAmmo()
        {
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.CLIP));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.BULLETBOX));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.SHELLS));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.SHELLBOX));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.ROCKET));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.ROCKETBOX));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.ECELL));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.ECELLPACK));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.BACKPACK));
        }

        [Fact]
        public void ThingType_ShouldContainHealth()
        {
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.HEALTHPOTION));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.STIMPACK));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.MEDIKIT));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.SUPERCHARGE));
        }

        [Fact]
        public void ThingType_ShouldContainArmor()
        {
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.GREENARMOR));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.BLUEARMOR));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.ARMORHELMET));
        }

        [Fact]
        public void ThingType_ShouldContainKeys()
        {
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.BLUEKEYCARD));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.REDKEYCARD));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.YELLOWKEYCARD));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.BLUESKULLKEY));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.REDSKULLKEY));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.YELLOWSKULLKEY));
        }

        [Fact]
        public void ThingType_ShouldContainPowerups()
        {
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.INVULNERABILITY));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.BESERKPACK));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.INVISIBILITY));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.RADSUIT));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.AUTOMAP));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.LITEAMP));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.MEGASPHERE));
        }

        [Fact]
        public void ThingType_ShouldContainDecorations()
        {
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.BARREL));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.TALLTECHLAMP));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.SHORTTECHLAMP));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.FLOORLAMP));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.CANDLE));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.CANDELABRA));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.TREE));
        }

        [Fact]
        public void ThingType_ShouldContainProjectiles()
        {
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.IMPSHOT));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.CACOSHOT));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.FLYINGROCKET));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.FLYINGPLASMA));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.FLYINGBFG));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.BARONSHOT));
        }

        [Fact]
        public void ThingType_ShouldContainMiscObjects()
        {
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.TELEPORTMAN));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.BOSSBRAIN));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.BOSSSPIT));
            Assert.True(Enum.IsDefined(typeof(ThingType), ThingType.BOSSTARGET));
        }

        #endregion

        #region LinedefFlags Enum Tests

        [Fact]
        public void LinedefFlags_ShouldBeFlagsEnum()
        {
            var attr = typeof(LinedefFlags).GetCustomAttribute<FlagsAttribute>();
            Assert.NotNull(attr);
        }

        [Fact]
        public void LinedefFlags_ShouldContainStandardFlags()
        {
            Assert.Equal(0x0000, (ushort)LinedefFlags.None);
            Assert.Equal(0x0001, (ushort)LinedefFlags.Impassable);
            Assert.Equal(0x0002, (ushort)LinedefFlags.BlockMonsters);
            Assert.Equal(0x0004, (ushort)LinedefFlags.TwoSided);
            Assert.Equal(0x0008, (ushort)LinedefFlags.UpperUnpegged);
            Assert.Equal(0x0010, (ushort)LinedefFlags.LowerUnpegged);
            Assert.Equal(0x0020, (ushort)LinedefFlags.Secret);
            Assert.Equal(0x0040, (ushort)LinedefFlags.BlockSound);
            Assert.Equal(0x0080, (ushort)LinedefFlags.NotOnMap);
            Assert.Equal(0x0100, (ushort)LinedefFlags.AlreadyOnMap);
        }

        [Fact]
        public void LinedefFlags_ShouldContainBoomFlags()
        {
            Assert.Equal(0x0200, (ushort)LinedefFlags.PassThru);
            Assert.Equal(0x0400, (ushort)LinedefFlags.Translucent);
        }

        [Fact]
        public void LinedefFlags_ShouldContainStrifeFlags()
        {
            Assert.Equal(0x0800, (ushort)LinedefFlags.JumpOver);
            Assert.Equal(0x1000, (ushort)LinedefFlags.BlockFloaters);
        }

        [Fact]
        public void LinedefFlags_Combinations_ShouldWork()
        {
            var flags = LinedefFlags.Impassable | LinedefFlags.TwoSided | LinedefFlags.Secret;
            Assert.Equal(0x0025, (ushort)flags);

            Assert.True(flags.HasFlag(LinedefFlags.Impassable));
            Assert.True(flags.HasFlag(LinedefFlags.TwoSided));
            Assert.True(flags.HasFlag(LinedefFlags.Secret));
            Assert.False(flags.HasFlag(LinedefFlags.BlockMonsters));
        }

        #endregion

        #region ThingFlags Enum Tests

        [Fact]
        public void ThingFlags_ShouldBeFlagsEnum()
        {
            var attr = typeof(ThingFlags).GetCustomAttribute<FlagsAttribute>();
            Assert.NotNull(attr);
        }

        [Fact]
        public void ThingFlags_ShouldContainSkillFlags()
        {
            Assert.Equal(0x0001, (ushort)ThingFlags.SkillEasy);
            Assert.Equal(0x0002, (ushort)ThingFlags.SkillMedium);
            Assert.Equal(0x0004, (ushort)ThingFlags.SkillHard);
        }

        [Fact]
        public void ThingFlags_ShouldContainStandardFlags()
        {
            Assert.Equal(0x0008, (ushort)ThingFlags.Ambush);
            Assert.Equal(0x0010, (ushort)ThingFlags.Multiplayer);
        }

        [Fact]
        public void ThingFlags_ShouldContainBoomFlags()
        {
            Assert.Equal(0x0020, (ushort)ThingFlags.NotInDeathmatch);
            Assert.Equal(0x0040, (ushort)ThingFlags.NotInCoop);
        }

        [Fact]
        public void ThingFlags_ShouldContainMBFFlags()
        {
            Assert.Equal(0x0080, (ushort)ThingFlags.Friendly);
        }

        [Fact]
        public void ThingFlags_AllSkills_ShouldCombine()
        {
            var allSkills = ThingFlags.SkillEasy | ThingFlags.SkillMedium | ThingFlags.SkillHard;
            Assert.Equal(0x0007, (ushort)allSkills);
        }

        #endregion

        #region HexenLinedefFlags Enum Tests

        [Fact]
        public void HexenLinedefFlags_ShouldBeFlagsEnum()
        {
            var attr = typeof(HexenLinedefFlags).GetCustomAttribute<FlagsAttribute>();
            Assert.NotNull(attr);
        }

        [Fact]
        public void HexenLinedefFlags_ShouldContainStandardFlags()
        {
            Assert.Equal(0x0001, (ushort)HexenLinedefFlags.Impassable);
            Assert.Equal(0x0002, (ushort)HexenLinedefFlags.BlockMonsters);
            Assert.Equal(0x0004, (ushort)HexenLinedefFlags.TwoSided);
            Assert.Equal(0x0008, (ushort)HexenLinedefFlags.UpperUnpegged);
            Assert.Equal(0x0010, (ushort)HexenLinedefFlags.LowerUnpegged);
            Assert.Equal(0x0020, (ushort)HexenLinedefFlags.Secret);
            Assert.Equal(0x0040, (ushort)HexenLinedefFlags.BlockSound);
            Assert.Equal(0x0080, (ushort)HexenLinedefFlags.NotOnMap);
            Assert.Equal(0x0100, (ushort)HexenLinedefFlags.AlreadyOnMap);
        }

        [Fact]
        public void HexenLinedefFlags_ShouldContainRepeatableFlag()
        {
            Assert.Equal(0x0200, (ushort)HexenLinedefFlags.Repeatable);
        }

        [Fact]
        public void HexenLinedefFlags_ShouldContainActivationFlags()
        {
            Assert.Equal(0x0000, (ushort)HexenLinedefFlags.ActivatePlayerCross);
            Assert.Equal(0x0400, (ushort)HexenLinedefFlags.ActivatePlayerUse);
            Assert.Equal(0x0800, (ushort)HexenLinedefFlags.ActivateMonsterCross);
            Assert.Equal(0x0C00, (ushort)HexenLinedefFlags.ActivateProjectileHit);
            Assert.Equal(0x1000, (ushort)HexenLinedefFlags.ActivatePlayerBump);
            Assert.Equal(0x1400, (ushort)HexenLinedefFlags.ActivateProjectileCross);
            Assert.Equal(0x1800, (ushort)HexenLinedefFlags.ActivatePlayerUsePassthrough);
        }

        [Fact]
        public void HexenLinedefFlags_ShouldContainBlockAllFlag()
        {
            Assert.Equal(0x8000, (ushort)HexenLinedefFlags.BlockAll);
        }

        [Fact]
        public void HexenActivationType_ShouldMatchLinedefFlags()
        {
            Assert.Equal((ushort)HexenLinedefFlags.ActivatePlayerCross, (ushort)HexenActivationType.PlayerCross);
            Assert.Equal((ushort)HexenLinedefFlags.ActivatePlayerUse, (ushort)HexenActivationType.PlayerUse);
            Assert.Equal((ushort)HexenLinedefFlags.ActivateMonsterCross, (ushort)HexenActivationType.MonsterCross);
            Assert.Equal((ushort)HexenLinedefFlags.ActivateProjectileHit, (ushort)HexenActivationType.ProjectileHit);
            Assert.Equal((ushort)HexenLinedefFlags.ActivatePlayerBump, (ushort)HexenActivationType.PlayerBump);
            Assert.Equal((ushort)HexenLinedefFlags.ActivateProjectileCross, (ushort)HexenActivationType.ProjectileCross);
            Assert.Equal((ushort)HexenLinedefFlags.ActivatePlayerUsePassthrough, (ushort)HexenActivationType.PlayerUsePassthrough);
        }

        #endregion

        #region HexenThingFlags Enum Tests

        [Fact]
        public void HexenThingFlags_ShouldBeFlagsEnum()
        {
            var attr = typeof(HexenThingFlags).GetCustomAttribute<FlagsAttribute>();
            Assert.NotNull(attr);
        }

        [Fact]
        public void HexenThingFlags_ShouldContainSkillFlags()
        {
            Assert.Equal(0x0001, (ushort)HexenThingFlags.SkillEasy);
            Assert.Equal(0x0002, (ushort)HexenThingFlags.SkillMedium);
            Assert.Equal(0x0004, (ushort)HexenThingFlags.SkillHard);
        }

        [Fact]
        public void HexenThingFlags_ShouldContainClassFlags()
        {
            Assert.Equal(0x0020, (ushort)HexenThingFlags.Class1);
            Assert.Equal(0x0040, (ushort)HexenThingFlags.Class2);
            Assert.Equal(0x0080, (ushort)HexenThingFlags.Class3);
        }

        [Fact]
        public void HexenThingFlags_ShouldContainGameModeFlags()
        {
            Assert.Equal(0x0100, (ushort)HexenThingFlags.Single);
            Assert.Equal(0x0200, (ushort)HexenThingFlags.Coop);
            Assert.Equal(0x0400, (ushort)HexenThingFlags.Deathmatch);
        }

        [Fact]
        public void HexenThingFlags_ShouldContainHexenSpecificFlags()
        {
            Assert.Equal(0x0008, (ushort)HexenThingFlags.Ambush);
            Assert.Equal(0x0010, (ushort)HexenThingFlags.Dormant);
        }

        #endregion

        #region MBF21LinedefFlags Enum Tests

        [Fact]
        public void MBF21LinedefFlags_ShouldBeFlagsEnum()
        {
            var attr = typeof(MBF21LinedefFlags).GetCustomAttribute<FlagsAttribute>();
            Assert.NotNull(attr);
        }

        [Fact]
        public void MBF21LinedefFlags_ShouldContainStandardFlags()
        {
            Assert.Equal(0x00000001u, (uint)MBF21LinedefFlags.Impassable);
            Assert.Equal(0x00000002u, (uint)MBF21LinedefFlags.BlockMonsters);
            Assert.Equal(0x00000004u, (uint)MBF21LinedefFlags.TwoSided);
            Assert.Equal(0x00000008u, (uint)MBF21LinedefFlags.UpperUnpegged);
            Assert.Equal(0x00000010u, (uint)MBF21LinedefFlags.LowerUnpegged);
            Assert.Equal(0x00000020u, (uint)MBF21LinedefFlags.Secret);
            Assert.Equal(0x00000040u, (uint)MBF21LinedefFlags.BlockSound);
            Assert.Equal(0x00000080u, (uint)MBF21LinedefFlags.NotOnMap);
            Assert.Equal(0x00000100u, (uint)MBF21LinedefFlags.AlreadyOnMap);
        }

        [Fact]
        public void MBF21LinedefFlags_ShouldContainBoomFlags()
        {
            Assert.Equal(0x00000200u, (uint)MBF21LinedefFlags.PassThru);
        }

        [Fact]
        public void MBF21LinedefFlags_ShouldContainMBF21ExtendedFlags()
        {
            Assert.Equal(0x00010000u, (uint)MBF21LinedefFlags.BlockPlayers);
            Assert.Equal(0x00020000u, (uint)MBF21LinedefFlags.BlockFloaters);
            Assert.Equal(0x00040000u, (uint)MBF21LinedefFlags.BlockLandMonsters);
        }

        [Fact]
        public void MBF21LinedefFlags_ExtendedFlagsInUpperBits()
        {
            // MBF21 extended flags should be in bits 16+
            var extendedFlags = MBF21LinedefFlags.BlockPlayers |
                               MBF21LinedefFlags.BlockFloaters |
                               MBF21LinedefFlags.BlockLandMonsters;

            Assert.True((uint)extendedFlags >= 0x00010000u);
            Assert.Equal(0x00070000u, (uint)extendedFlags);
        }

        #endregion

        #region MBF21ThingFlags Enum Tests

        [Fact]
        public void MBF21ThingFlags_ShouldBeFlagsEnum()
        {
            var attr = typeof(MBF21ThingFlags).GetCustomAttribute<FlagsAttribute>();
            Assert.NotNull(attr);
        }

        [Fact]
        public void MBF21ThingFlags_ShouldContainExtendedFlags()
        {
            Assert.Equal(0x00010000u, (uint)MBF21ThingFlags.LogKill);
            Assert.Equal(0x00020000u, (uint)MBF21ThingFlags.FullVolSounds);
            Assert.Equal(0x00040000u, (uint)MBF21ThingFlags.IsMonster);
            Assert.Equal(0x00080000u, (uint)MBF21ThingFlags.CountKill);
        }

        [Fact]
        public void MBF21ThingFlags_ShouldContainBossFlags()
        {
            Assert.Equal(0x00100000u, (uint)MBF21ThingFlags.Map07Boss1);
            Assert.Equal(0x00200000u, (uint)MBF21ThingFlags.Map07Boss2);
            Assert.Equal(0x00400000u, (uint)MBF21ThingFlags.E1M8Boss);
            Assert.Equal(0x00800000u, (uint)MBF21ThingFlags.E2M8Boss);
            Assert.Equal(0x01000000u, (uint)MBF21ThingFlags.E3M8Boss);
            Assert.Equal(0x02000000u, (uint)MBF21ThingFlags.E4M6Boss);
            Assert.Equal(0x04000000u, (uint)MBF21ThingFlags.E4M8Boss);
        }

        [Fact]
        public void MBF21ThingFlags_ShouldContainMiscFlags()
        {
            Assert.Equal(0x08000000u, (uint)MBF21ThingFlags.RipSound);
            Assert.Equal(0x10000000u, (uint)MBF21ThingFlags.NoBossSpawn);
        }

        [Fact]
        public void MBF21ThingFlags_AllBossFlags_ShouldCombine()
        {
            var bossFlags = MBF21ThingFlags.Map07Boss1 |
                           MBF21ThingFlags.Map07Boss2 |
                           MBF21ThingFlags.E1M8Boss |
                           MBF21ThingFlags.E2M8Boss |
                           MBF21ThingFlags.E3M8Boss |
                           MBF21ThingFlags.E4M6Boss |
                           MBF21ThingFlags.E4M8Boss;

            Assert.Equal(0x07F00000u, (uint)bossFlags);
        }

        #endregion

        #region Attribute Definition Tests

        [Fact]
        public void BoomFlagAttribute_ShouldExist()
        {
            var type = Type.GetType("WAD.NET.BoomFlagAttribute, WAD.NET");
            Assert.NotNull(type);
        }

        [Fact]
        public void HexenFlagAttribute_ShouldExist()
        {
            var type = Type.GetType("WAD.NET.HexenFlagAttribute, WAD.NET");
            Assert.NotNull(type);
        }

        [Fact]
        public void StrifeFlagAttribute_ShouldExist()
        {
            var type = Type.GetType("WAD.NET.StrifeFlagAttribute, WAD.NET");
            Assert.NotNull(type);
        }

        [Fact]
        public void SPACFlagAttribute_ShouldExist()
        {
            var type = Type.GetType("WAD.NET.SPACFlagAttribute, WAD.NET");
            Assert.NotNull(type);
        }

        [Fact]
        public void AllAttributeTypes_ShouldInheritFromAttribute()
        {
            var attributeTypes = new[]
            {
                Type.GetType("WAD.NET.BoomFlagAttribute, WAD.NET"),
                Type.GetType("WAD.NET.HexenFlagAttribute, WAD.NET"),
                Type.GetType("WAD.NET.StrifeFlagAttribute, WAD.NET"),
                Type.GetType("WAD.NET.SPACFlagAttribute, WAD.NET")
            };

            foreach (var type in attributeTypes)
            {
                if (type != null)
                {
                    Assert.True(typeof(Attribute).IsAssignableFrom(type),
                        $"{type.Name} should inherit from Attribute");
                }
            }
        }

        #endregion

        #region BoomLinedefFlags Tests

        [Fact]
        public void BoomLinedefFlags_ShouldBeFlagsEnum()
        {
            var attr = typeof(BoomLinedefFlags).GetCustomAttribute<FlagsAttribute>();
            Assert.NotNull(attr);
        }

        [Fact]
        public void BoomLinedefFlags_ShouldContainStandardDoomFlags()
        {
            Assert.Equal(0x0001, (ushort)BoomLinedefFlags.Impassable);
            Assert.Equal(0x0002, (ushort)BoomLinedefFlags.BlockMonsters);
            Assert.Equal(0x0004, (ushort)BoomLinedefFlags.TwoSided);
        }

        [Fact]
        public void BoomLinedefFlags_ShouldContainBoomSpecificFlags()
        {
            Assert.Equal(0x0200, (ushort)BoomLinedefFlags.PassThru);
            Assert.Equal(0x1000, (ushort)BoomLinedefFlags.TranslucentMidtex);
        }

        #endregion

        #region Flag Combination Tests

        [Fact]
        public void LinedefFlags_StandardWall_ShouldHaveCorrectFlags()
        {
            // A standard impassable wall
            var flags = LinedefFlags.Impassable;
            Assert.Equal(0x0001, (ushort)flags);
        }

        [Fact]
        public void LinedefFlags_TwoSidedDoor_ShouldHaveCorrectFlags()
        {
            // A two-sided door linedef
            var flags = LinedefFlags.TwoSided | LinedefFlags.UpperUnpegged | LinedefFlags.LowerUnpegged;
            Assert.Equal(0x001C, (ushort)flags);
        }

        [Fact]
        public void LinedefFlags_SecretDoor_ShouldHaveCorrectFlags()
        {
            var flags = LinedefFlags.TwoSided | LinedefFlags.Secret;
            Assert.Equal(0x0024, (ushort)flags);
        }

        [Fact]
        public void ThingFlags_EasySkillOnly_ShouldHaveCorrectFlag()
        {
            var flags = ThingFlags.SkillEasy;
            Assert.Equal(0x0001, (ushort)flags);
        }

        [Fact]
        public void ThingFlags_AllSinglePlayerSkills_ShouldCombine()
        {
            var flags = ThingFlags.SkillEasy | ThingFlags.SkillMedium | ThingFlags.SkillHard;
            Assert.Equal(0x0007, (ushort)flags);
        }

        [Fact]
        public void ThingFlags_MultiplayerOnly_ShouldHaveCorrectFlag()
        {
            var flags = ThingFlags.Multiplayer;
            Assert.Equal(0x0010, (ushort)flags);
        }

        [Fact]
        public void ThingFlags_AmbushMonster_ShouldHaveCorrectFlags()
        {
            // An ambush monster appearing on all skills
            var flags = ThingFlags.SkillEasy | ThingFlags.SkillMedium | ThingFlags.SkillHard | ThingFlags.Ambush;
            Assert.Equal(0x000F, (ushort)flags);
        }

        #endregion

        #region Enum Coverage Tests

        [Fact]
        public void ThingType_ShouldHaveMultipleValues()
        {
            var values = Enum.GetValues(typeof(ThingType));
            Assert.True(values.Length > 50, "ThingType should have many defined values");
        }

        [Fact]
        public void LinedefFlags_ShouldHaveMultipleValues()
        {
            var values = Enum.GetValues(typeof(LinedefFlags));
            Assert.True(values.Length >= 10, "LinedefFlags should have multiple defined values");
        }

        [Fact]
        public void ThingFlags_ShouldHaveMultipleValues()
        {
            var values = Enum.GetValues(typeof(ThingFlags));
            Assert.True(values.Length >= 5, "ThingFlags should have multiple defined values");
        }

        [Fact]
        public void HexenLinedefFlags_ShouldHaveMultipleValues()
        {
            var values = Enum.GetValues(typeof(HexenLinedefFlags));
            Assert.True(values.Length >= 10, "HexenLinedefFlags should have multiple defined values");
        }

        [Fact]
        public void HexenThingFlags_ShouldHaveMultipleValues()
        {
            var values = Enum.GetValues(typeof(HexenThingFlags));
            Assert.True(values.Length >= 8, "HexenThingFlags should have multiple defined values");
        }

        #endregion
    }
}
