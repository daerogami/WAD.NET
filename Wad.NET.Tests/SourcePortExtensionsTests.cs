using System;
using Xunit;
using WAD.NET.SourcePorts.Boom;
using WAD.NET.SourcePorts.MBF21;
using WAD.NET.Parsers.Dehacked;
using WAD.NET.Parsers.MapInfo;
using WAD.NET.Parsers.SndInfo;
using WAD.NET.Detection;

namespace WAD.NET.Tests
{
    public class SourcePortExtensionsTests
    {
        #region Boom Generalized Linedef Tests

        [Fact]
        public void BoomGeneralizedLinedef_IsGeneralized_ShouldReturnTrueForGeneralizedTypes()
        {
            Assert.True(BoomGeneralizedLinedef.IsGeneralized(0x6000)); // Floor base
            Assert.True(BoomGeneralizedLinedef.IsGeneralized(0x4000)); // Ceiling base
            Assert.True(BoomGeneralizedLinedef.IsGeneralized(0x3C00)); // Door base
            Assert.True(BoomGeneralizedLinedef.IsGeneralized(0x2F80)); // Crusher base
        }

        [Fact]
        public void BoomGeneralizedLinedef_IsGeneralized_ShouldReturnFalseForStandardTypes()
        {
            Assert.False(BoomGeneralizedLinedef.IsGeneralized(0));
            Assert.False(BoomGeneralizedLinedef.IsGeneralized(1));   // Door
            Assert.False(BoomGeneralizedLinedef.IsGeneralized(11));  // Exit
            Assert.False(BoomGeneralizedLinedef.IsGeneralized(0x2F00)); // Below crusher base
        }

        [Fact]
        public void BoomGeneralizedLinedef_Parse_ShouldDecodeFloorAction()
        {
            // 0x6048 = FloorBase + offset
            // offset = 0x0048 = 72 = 0b1001000
            // Trigger = 0 (WalkOnce), Speed = 1 (Normal), bits 3-4
            ushort type = 0x6048;
            var action = BoomGeneralizedLinedef.Parse(type);

            Assert.NotNull(action);
            Assert.IsType<GeneralizedFloorAction>(action);
            var floor = (GeneralizedFloorAction)action;
            Assert.Equal(GeneralizedActionType.Floor, floor.ActionType);
            Assert.Equal(TriggerType.WalkOnce, floor.Trigger);
            Assert.Equal(SpeedType.Normal, floor.Speed);
        }

        [Fact]
        public void BoomGeneralizedLinedef_Parse_ShouldDecodeDoorAction()
        {
            // Door at base
            ushort type = 0x3C00;
            var action = BoomGeneralizedLinedef.Parse(type);

            Assert.NotNull(action);
            Assert.IsType<GeneralizedDoorAction>(action);
            var door = (GeneralizedDoorAction)action;
            Assert.Equal(GeneralizedActionType.Door, door.ActionType);
            Assert.Equal(TriggerType.WalkOnce, door.Trigger);
        }

        [Fact]
        public void BoomGeneralizedLinedef_Parse_ShouldReturnNullForNonGeneralized()
        {
            var action = BoomGeneralizedLinedef.Parse(0);
            Assert.Null(action);

            action = BoomGeneralizedLinedef.Parse(11);
            Assert.Null(action);
        }

        #endregion

        #region MBF21 Detector Tests

        [Fact]
        public void MBF21Detector_ShouldDetectMBF21Bits()
        {
            var deh = @"
Patch File for DeHackEd v3.0
Doom version = 21

Thing 1 (Zombie)
MBF21 Bits = 524288
";
            Assert.True(MBF21Detector.ContainsMBF21Features(deh));
        }

        [Fact]
        public void MBF21Detector_ShouldDetectMBF21Codepointers()
        {
            var deh = @"
[CODEPTR]
Frame 123 = A_SpawnObject
";
            Assert.True(MBF21Detector.ContainsMBF21Features(deh));
        }

        [Fact]
        public void MBF21Detector_ShouldReturnFalseForStandardDehacked()
        {
            var deh = @"
Patch File for DeHackEd v3.0
Doom version = 19

Thing 1 (Zombie)
Hit points = 100
";
            Assert.False(MBF21Detector.ContainsMBF21Features(deh));
        }

        #endregion

        #region DEHACKED Parser Tests

        [Fact]
        public void DehackedParser_ShouldParseHeader()
        {
            var deh = @"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6
";
            var parser = new DehackedParser(deh);
            var patch = parser.Parse();

            Assert.Equal(19, patch.DoomVersion);
            Assert.Equal(6, patch.PatchFormat);
        }

        [Fact]
        public void DehackedParser_ShouldParseThing()
        {
            var deh = @"
Patch File for DeHackEd v3.0
Doom version = 19

Thing 1 (Zombie)
Hit points = 100
Speed = 12
Reaction time = 8
";
            var parser = new DehackedParser(deh);
            var patch = parser.Parse();

            Assert.Single(patch.Things);
            Assert.Equal(1, patch.Things[0].Index);
            Assert.Equal("Zombie", patch.Things[0].Name);
            Assert.Equal(100, patch.Things[0].HitPoints);
            Assert.Equal(12, patch.Things[0].Speed);
            Assert.Equal(8, patch.Things[0].ReactionTime);
        }

        [Fact]
        public void DehackedParser_ShouldParseMultipleThings()
        {
            var deh = @"
Thing 1 (Zombie)
Hit points = 100

Thing 2 (Imp)
Hit points = 200
";
            var parser = new DehackedParser(deh);
            var patch = parser.Parse();

            Assert.Equal(2, patch.Things.Count);
            Assert.Equal(100, patch.Things[0].HitPoints);
            Assert.Equal(200, patch.Things[1].HitPoints);
        }

        [Fact]
        public void DehackedParser_ShouldParseFrame()
        {
            var deh = @"
Frame 10
Sprite number = 3
Sprite subnumber = 0
Duration = 5
Next frame = 11
";
            var parser = new DehackedParser(deh);
            var patch = parser.Parse();

            Assert.Single(patch.Frames);
            Assert.Equal(10, patch.Frames[0].Index);
            Assert.Equal(3, patch.Frames[0].SpriteNumber);
            Assert.Equal(0, patch.Frames[0].SpriteSubnumber);
            Assert.Equal(5, patch.Frames[0].Duration);
            Assert.Equal(11, patch.Frames[0].NextState);
        }

        [Fact]
        public void DehackedParser_ShouldParseWeapon()
        {
            var deh = @"
Weapon 2 (Shotgun)
Ammo type = 1
Deselect frame = 67
Select frame = 68
";
            var parser = new DehackedParser(deh);
            var patch = parser.Parse();

            Assert.Single(patch.Weapons);
            Assert.Equal(2, patch.Weapons[0].Index);
            Assert.Equal("Shotgun", patch.Weapons[0].Name);
            Assert.Equal(1, patch.Weapons[0].AmmoType);
        }

        [Fact]
        public void DehackedParser_ShouldParseMisc()
        {
            var deh = @"
Misc 0
Initial Health = 100
Initial Bullets = 50
Max Health = 200
";
            var parser = new DehackedParser(deh);
            var patch = parser.Parse();

            Assert.NotNull(patch.Misc);
            Assert.Equal(100, patch.Misc.InitialHealth);
            Assert.Equal(50, patch.Misc.InitialBullets);
            Assert.Equal(200, patch.Misc.MaxHealth);
        }

        [Fact]
        public void DehackedParser_ShouldParseCodePointers()
        {
            var deh = @"
[CODEPTR]
Frame 10 = A_BFGSpray
Frame 20 = A_Fire
";
            var parser = new DehackedParser(deh);
            var patch = parser.Parse();

            Assert.Equal(2, patch.CodePointers.Count);
            Assert.Equal(10, patch.CodePointers[0].FrameIndex);
            Assert.Equal("A_BFGSpray", patch.CodePointers[0].CodePointerName);
        }

        [Fact]
        public void DehackedParser_ShouldParseStrings()
        {
            var deh = @"
[STRINGS]
HUSTR_E1M1 = Hangar
HUSTR_E1M2 = Nuclear Plant
";
            var parser = new DehackedParser(deh);
            var patch = parser.Parse();

            Assert.Equal(2, patch.Strings.Count);
            Assert.Equal("HUSTR_E1M1", patch.Strings[0].Mnemonic);
            Assert.Equal("Hangar", patch.Strings[0].Value);
        }

        [Fact]
        public void DehackedParser_ShouldParsePars()
        {
            var deh = @"
[PARS]
par 1 1 30
par 1 2 60
par 5 120
";
            var parser = new DehackedParser(deh);
            var patch = parser.Parse();

            Assert.Equal(3, patch.Pars.Count);
            Assert.Equal(1, patch.Pars[0].Episode);
            Assert.Equal(1, patch.Pars[0].Map);
            Assert.Equal(30, patch.Pars[0].Seconds);
            // DOOM II format
            Assert.Equal(0, patch.Pars[2].Episode);
            Assert.Equal(5, patch.Pars[2].Map);
        }

        #endregion

        #region MAPINFO Parser Tests

        [Fact]
        public void MapInfoParser_ShouldParseBasicMap()
        {
            var mapinfo = @"
map E1M1 ""Hangar""
{
    levelnum = 1
    titlepatch = ""WILV00""
    next = ""E1M2""
    secretnext = ""E1M9""
    sky1 = ""SKY1""
    music = ""D_E1M1""
    cluster = 1
    par = 30
}
";
            var parser = new MapInfoParser(mapinfo);
            var info = parser.Parse();

            Assert.Single(info.Maps);
            Assert.True(info.Maps.ContainsKey("E1M1"));
            var map = info.Maps["E1M1"];
            Assert.Equal("Hangar", map.NiceName);
            Assert.Equal(1, map.LevelNum);
            Assert.Equal("E1M2", map.Next);
            Assert.Equal("E1M9", map.SecretNext);
            Assert.Equal("SKY1", map.Sky1);
            Assert.Equal("D_E1M1", map.Music);
            Assert.Equal(1, map.Cluster);
            Assert.Equal(30, map.Par);
        }

        [Fact]
        public void MapInfoParser_ShouldParseMapFlags()
        {
            var mapinfo = @"
map MAP01 ""Test""
{
    nointermission
    allowjump
    nocrouch
    nofreelook
}
";
            var parser = new MapInfoParser(mapinfo);
            var info = parser.Parse();

            Assert.Single(info.Maps);
            var map = info.Maps["MAP01"];
            Assert.True(map.NoIntermission);
            Assert.True(map.Flags.AllowJump);
            Assert.True(map.Flags.NoCrouch);
            Assert.True(map.Flags.NoFreeLook);
        }

        [Fact]
        public void MapInfoParser_ShouldParseCluster()
        {
            var mapinfo = @"
cluster 1
{
    flat = ""FLOOR4_8""
    music = ""D_READ_M""
    exittext = ""Once you beat the big badasses...""
}
";
            var parser = new MapInfoParser(mapinfo);
            var info = parser.Parse();

            Assert.Single(info.Clusters);
            Assert.True(info.Clusters.ContainsKey(1));
            var cluster = info.Clusters[1];
            Assert.Equal("FLOOR4_8", cluster.Flat);
            Assert.Equal("D_READ_M", cluster.Music);
            Assert.Contains("Once you beat", cluster.ExitText);
        }

        [Fact]
        public void MapInfoParser_ShouldParseEpisode()
        {
            var mapinfo = @"
clearepisodes
episode E1M1
{
    name = ""Knee-Deep in the Dead""
    picname = ""M_EPI1""
    key = k
}
";
            var parser = new MapInfoParser(mapinfo);
            var info = parser.Parse();

            Assert.True(info.ClearEpisodes);
            Assert.Single(info.Episodes);
            var episode = info.Episodes[1];
            Assert.Equal("E1M1", episode.StartMap);
            Assert.Equal("Knee-Deep in the Dead", episode.Name);
            Assert.Equal("M_EPI1", episode.PicName);
            Assert.Equal('k', episode.Key);
        }

        [Fact]
        public void MapInfoParser_ShouldHandleComments()
        {
            var mapinfo = @"
// This is a comment
map E1M1 ""Test"" /* inline comment */
{
    par = 30 // par time
}
";
            var parser = new MapInfoParser(mapinfo);
            var info = parser.Parse();

            Assert.Single(info.Maps);
            Assert.Equal(30, info.Maps["E1M1"].Par);
        }

        #endregion

        #region DECORATE Scanner Tests

        [Fact]
        public void DecorateScanner_ShouldDetectActors()
        {
            var decorate = @"
actor SuperImp : DoomImp replaces DoomImp 3001
{
    health 200
    radius 20
    height 56
    speed 10
    +ISMONSTER
}
";
            var info = DecorateScanner.Scan(decorate);

            Assert.Single(info.Actors);
            Assert.True(info.HasActors);
            var actor = info.Actors[0];
            Assert.Equal("SuperImp", actor.Name);
            Assert.Equal("DoomImp", actor.Parent);
            Assert.Equal("DoomImp", actor.Replaces);
            Assert.Equal(3001, actor.EditorNumber);
        }

        [Fact]
        public void DecorateScanner_ShouldExtractProperties()
        {
            var decorate = @"
actor TestActor 10000
{
    health 100
    radius 16
    height 32
    speed 8
    damage 5
    +ISMONSTER
    +MISSILE
}
";
            var info = DecorateScanner.Scan(decorate);

            Assert.Single(info.Actors);
            var actor = info.Actors[0];
            Assert.Equal(100, actor.SpawnHealth);
            Assert.Equal(16, actor.Radius);
            Assert.Equal(32, actor.Height);
            Assert.Equal(8, actor.Speed);
            Assert.Equal(5, actor.Damage);
            Assert.True(actor.IsMonster);
            Assert.True(actor.IsProjectile);
        }

        [Fact]
        public void DecorateScanner_ShouldDetectIncludes()
        {
            var decorate = @"
#include ""actors/monsters.txt""
#include ""actors/weapons.txt""
";
            var info = DecorateScanner.Scan(decorate);

            Assert.Equal(2, info.Includes.Count);
            Assert.Contains("actors/monsters.txt", info.Includes);
            Assert.Contains("actors/weapons.txt", info.Includes);
        }

        [Fact]
        public void DecorateScanner_IsDecorate_ShouldReturnTrue()
        {
            Assert.True(DecorateScanner.IsDecorate("actor Test { }"));
            Assert.True(DecorateScanner.IsDecorate("ACTOR MyMonster : DoomImp"));
        }

        [Fact]
        public void DecorateScanner_IsDecorate_ShouldReturnFalse()
        {
            Assert.False(DecorateScanner.IsDecorate(""));
            Assert.False(DecorateScanner.IsDecorate("class Test { }"));
            Assert.False(DecorateScanner.IsDecorate("// just a comment"));
        }

        #endregion

        #region ZScript Scanner Tests

        [Fact]
        public void ZScriptScanner_ShouldDetectVersion()
        {
            var zscript = @"
version ""4.3.0""

class Test : Actor
{
}
";
            var info = ZScriptScanner.Scan(zscript);

            Assert.Equal("4.3.0", info.Version);
        }

        [Fact]
        public void ZScriptScanner_ShouldDetectClasses()
        {
            var zscript = @"
class SuperImp : DoomImp replaces DoomImp
{
    Default
    {
        Health 200;
    }
}
";
            var info = ZScriptScanner.Scan(zscript);

            Assert.Single(info.Classes);
            var cls = info.Classes[0];
            Assert.Equal("SuperImp", cls.Name);
            Assert.Equal("DoomImp", cls.Parent);
            Assert.Equal("DoomImp", cls.Replaces);
            Assert.True(cls.IsActor);
        }

        [Fact]
        public void ZScriptScanner_ShouldDetectEventHandlers()
        {
            var zscript = @"
class MyHandler : EventHandler
{
    override void WorldTick()
    {
    }
}
";
            var info = ZScriptScanner.Scan(zscript);

            Assert.Single(info.Classes);
            Assert.True(info.Classes[0].IsEventHandler);
        }

        [Fact]
        public void ZScriptScanner_ShouldDetectStructsAndEnums()
        {
            var zscript = @"
struct MyData
{
    int value;
}

enum MyEnum
{
    Value1,
    Value2
}
";
            var info = ZScriptScanner.Scan(zscript);

            Assert.Single(info.Structs);
            Assert.Equal("MyData", info.Structs[0]);
            Assert.Single(info.Enums);
            Assert.Equal("MyEnum", info.Enums[0]);
        }

        [Fact]
        public void ZScriptScanner_IsZScript_ShouldReturnTrue()
        {
            Assert.True(ZScriptScanner.IsZScript("version \"4.0\""));
            Assert.True(ZScriptScanner.IsZScript("class Test : Actor { Default { } }"));
        }

        [Fact]
        public void ZScriptScanner_IsZScript_ShouldReturnFalse()
        {
            Assert.False(ZScriptScanner.IsZScript(""));
            Assert.False(ZScriptScanner.IsZScript("actor Test { }"));  // DECORATE, not ZScript
        }

        #endregion

        #region SNDINFO Parser Tests

        [Fact]
        public void SndInfoParser_ShouldParseSoundDefinitions()
        {
            var sndinfo = @"
pistol          DSPISTOL
shotgn          DSSHOTGN
weapons/sshotf  DSDBOPN
";
            var parser = new SndInfoParser();
            var info = parser.Parse(sndinfo);

            Assert.Equal(3, info.Sounds.Count);
            Assert.Equal("DSPISTOL", info.Sounds["pistol"]);
            Assert.Equal("DSSHOTGN", info.Sounds["shotgn"]);
            Assert.Equal("DSDBOPN", info.Sounds["weapons/sshotf"]);
        }

        [Fact]
        public void SndInfoParser_ShouldParseRandomSounds()
        {
            var sndinfo = @"
$random grunt/death { grunt/death1 grunt/death2 grunt/death3 }
";
            var parser = new SndInfoParser();
            var info = parser.Parse(sndinfo);

            Assert.Single(info.RandomSounds);
            Assert.True(info.RandomSounds.ContainsKey("grunt/death"));
            Assert.Equal(3, info.RandomSounds["grunt/death"].Length);
        }

        [Fact]
        public void SndInfoParser_ShouldParseAlias()
        {
            var sndinfo = @"
$alias menu/choose weapons/pistol
";
            var parser = new SndInfoParser();
            var info = parser.Parse(sndinfo);

            Assert.Single(info.Aliases);
            Assert.Equal("weapons/pistol", info.Aliases["menu/choose"]);
        }

        [Fact]
        public void SndInfoParser_ShouldParseLimit()
        {
            var sndinfo = @"
$limit weapons/pistol 4
";
            var parser = new SndInfoParser();
            var info = parser.Parse(sndinfo);

            Assert.Single(info.Limits);
            Assert.Equal(4, info.Limits["weapons/pistol"]);
        }

        [Fact]
        public void SndInfoParser_ShouldParsePitchShiftRange()
        {
            var sndinfo = @"
$pitchshiftrange 4
";
            var parser = new SndInfoParser();
            var info = parser.Parse(sndinfo);

            Assert.Equal(4, info.PitchShiftRange);
        }

        [Fact]
        public void SndInfoParser_ShouldParseVolume()
        {
            var sndinfo = @"
$volume weapons/pistol 0.5
";
            var parser = new SndInfoParser();
            var info = parser.Parse(sndinfo);

            Assert.Single(info.Volumes);
            Assert.Equal(0.5f, info.Volumes["weapons/pistol"]);
        }

        [Fact]
        public void SndInfoParser_ShouldSkipComments()
        {
            var sndinfo = @"
// This is a comment
pistol DSPISTOL
; Another comment style
shotgn DSSHOTGN
";
            var parser = new SndInfoParser();
            var info = parser.Parse(sndinfo);

            Assert.Equal(2, info.Sounds.Count);
        }

        #endregion

        #region Compatibility Analyzer Tests

        [Fact]
        public void ModCompatibility_Description_ShouldReturnCorrectDescription()
        {
            var compat = new ModCompatibility { MinimumPort = SourcePort.Vanilla };
            Assert.Contains("vanilla", compat.Description.ToLower());

            compat.MinimumPort = SourcePort.Boom;
            Assert.Contains("boom", compat.Description.ToLower());

            compat.MinimumPort = SourcePort.GZDoom;
            Assert.Contains("gzdoom", compat.Description.ToLower());
        }

        [Fact]
        public void ModCompatibility_IsBoomCompatible_ShouldReturnCorrectValue()
        {
            var compat = new ModCompatibility { MinimumPort = SourcePort.Vanilla };
            Assert.True(compat.IsBoomCompatible);

            compat.MinimumPort = SourcePort.MBF21;
            Assert.True(compat.IsBoomCompatible);

            compat.MinimumPort = SourcePort.ZDoom;
            Assert.False(compat.IsBoomCompatible);
        }

        [Fact]
        public void ModCompatibility_RequiresZDoomFamily_ShouldReturnCorrectValue()
        {
            var compat = new ModCompatibility { MinimumPort = SourcePort.Boom };
            Assert.False(compat.RequiresZDoomFamily);

            compat.MinimumPort = SourcePort.ZDoom;
            Assert.True(compat.RequiresZDoomFamily);

            compat.MinimumPort = SourcePort.GZDoom;
            Assert.True(compat.RequiresZDoomFamily);
        }

        #endregion

        #region Parser Bug Fix Regression Tests

        // Bug 1: Comments in strings - MapInfo regex comment stripping destroyed URLs
        [Fact]
        public void MapInfoParser_ShouldPreserveUrlsInStrings()
        {
            var mapinfo = @"
map MAP01 ""http://example.com/mymap""
{
    par = 30
}";
            var parser = new MapInfoParser(mapinfo);
            var info = parser.Parse();
            Assert.Equal("http://example.com/mymap", info.Maps["MAP01"].NiceName);
        }

        // Bug 2: mustconfirm greedy consume - should not eat the next keyword
        [Fact]
        public void MapInfoParser_MustConfirm_ShouldNotConsumeNextKeyword()
        {
            var mapinfo = @"
skill nightmare
{
    mustconfirm
    fastmonsters
    name = ""Nightmare!""
}";
            var parser = new MapInfoParser(mapinfo);
            var info = parser.Parse();
            var skill = info.Skills[0];
            Assert.True(skill.MustConfirm);
            Assert.Null(skill.MustConfirmMessage);
            Assert.True(skill.FastMonsters);
        }

        // Bug 3: Episode parsing eating next keyword
        [Fact]
        public void MapInfoParser_Episode_ShouldNotEatNextTopLevelKeyword()
        {
            var mapinfo = @"
episode E1M1
{
    name = ""Knee-Deep in the Dead""
}
map E1M1 ""Hangar""
{
    par = 30
}";
            var parser = new MapInfoParser(mapinfo);
            var info = parser.Parse();
            Assert.Single(info.Episodes);
            Assert.Single(info.Maps);
            Assert.True(info.Maps.ContainsKey("E1M1"));
        }

        // Bug 4: Multi-line $random
        [Fact]
        public void SndInfoParser_ShouldParseMultiLineRandom()
        {
            var sndinfo = @"
$random weapons/shotgun {
    weapons/shotgun1
    weapons/shotgun2
    weapons/shotgun3
}
";
            var parser = new SndInfoParser();
            var info = parser.Parse(sndinfo);
            Assert.True(info.RandomSounds.ContainsKey("weapons/shotgun"));
            Assert.Equal(3, info.RandomSounds["weapons/shotgun"].Length);
        }

        // Bug 5: Block comments
        [Fact]
        public void SndInfoParser_ShouldHandleBlockComments()
        {
            var sndinfo = @"
pistol DSPISTOL
/* this is
   a multi-line
   comment */
shotgn DSSHOTGN
";
            var parser = new SndInfoParser();
            var info = parser.Parse(sndinfo);
            Assert.Equal(2, info.Sounds.Count);
        }

        // Bug 7: Hex overflow
        [Fact]
        public void DehackedParser_ShouldHandleLargeHexValues()
        {
            var deh = @"
Thing 1 (Test)
Bits = 0x80000000
";
            var parser = new DehackedParser(deh);
            var patch = parser.Parse();
            Assert.Equal(unchecked((uint)0x80000000), patch.Things[0].Flags);
        }

        // Bug 8: Section terminators - strings section should stop at Ammo section
        [Fact]
        public void DehackedParser_ShouldStopStringsSectionAtAmmo()
        {
            var deh = @"
[STRINGS]
HUSTR_E1M1 = Hangar

Ammo 0
Max ammo = 200
";
            var parser = new DehackedParser(deh);
            var patch = parser.Parse();
            Assert.Single(patch.Strings);
            Assert.Equal("HUSTR_E1M1", patch.Strings[0].Mnemonic);
            Assert.Single(patch.Ammo);
        }

        // Bug 9: Null cheat handling
        [Fact]
        public void DehackedParser_ShouldNotAddNullCheats()
        {
            var deh = @"
Cheat badlineformat
";
            var parser = new DehackedParser(deh);
            var patch = parser.Parse();
            // Should not crash and should not have null entries
            foreach (var cheat in patch.Cheats)
                Assert.NotNull(cheat);
        }

        #endregion

        #region Boom Features Tests

        [Fact]
        public void BoomFeatures_UsesBoomFeatures_ShouldReturnFalseWhenNoFeatures()
        {
            var features = new BoomFeatures();
            Assert.False(features.UsesBoomFeatures);
        }

        [Fact]
        public void BoomFeatures_UsesBoomFeatures_ShouldReturnTrueWhenAnyFeatureSet()
        {
            var features = new BoomFeatures { HasGeneralizedLinedefs = true };
            Assert.True(features.UsesBoomFeatures);

            features = new BoomFeatures { HasDeepWater = true };
            Assert.True(features.UsesBoomFeatures);
        }

        [Fact]
        public void BoomFeatures_UsesMbfFeatures_ShouldReturnCorrectly()
        {
            var features = new BoomFeatures();
            Assert.False(features.UsesMbfFeatures);

            features.HasFriendlyMonsters = true;
            Assert.True(features.UsesMbfFeatures);

            features = new BoomFeatures { HasTranslucentMidtex = true };
            Assert.True(features.UsesMbfFeatures);
        }

        #endregion

        #region Flag Enum Tests

        [Fact]
        public void BoomLinedefFlags_ShouldHaveCorrectValues()
        {
            Assert.Equal(0x0001, (ushort)BoomLinedefFlags.Impassable);
            Assert.Equal(0x0200, (ushort)BoomLinedefFlags.PassThru);
            Assert.Equal(0x1000, (ushort)BoomLinedefFlags.TranslucentMidtex);
        }

        [Fact]
        public void MBF21ThingFlags_ShouldHaveCorrectValues()
        {
            Assert.Equal(0x00010000u, (uint)MBF21ThingFlags.LogKill);
            Assert.Equal(0x00100000u, (uint)MBF21ThingFlags.Map07Boss1);
            Assert.Equal(0x10000000u, (uint)MBF21ThingFlags.NoBossSpawn);
        }

        [Fact]
        public void MBF21LinedefFlags_ShouldHaveCorrectValues()
        {
            Assert.Equal(0x00010000u, (uint)MBF21LinedefFlags.BlockPlayers);
            Assert.Equal(0x00020000u, (uint)MBF21LinedefFlags.BlockFloaters);
            Assert.Equal(0x00040000u, (uint)MBF21LinedefFlags.BlockLandMonsters);
        }

        #endregion
    }
}
