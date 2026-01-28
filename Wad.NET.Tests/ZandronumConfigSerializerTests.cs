using System.Linq;
using Xunit;
using WAD.NET.LaunchConfig;
using WAD.NET.SourcePorts.Zandronum;

namespace WAD.NET.Tests
{
    public class ZandronumConfigSerializerTests
    {
        private readonly ZandronumConfigSerializer _serializer = new ZandronumConfigSerializer();

        [Fact]
        public void ToCommandLineArgs_ProducesCorrectArgs()
        {
            var config = new WadLaunchConfiguration
            {
                Iwad = "doom2.wad",
                Files = { "brutal.pk3", "maps.wad" },
                Map = "MAP01",
                Skill = 4
            };

            var args = _serializer.ToCommandLineArgs(config);

            Assert.Contains("-iwad", args);
            Assert.Contains("doom2.wad", args);
            Assert.Contains("-file", args);
            Assert.Contains("brutal.pk3", args);
            Assert.Contains("maps.wad", args);
            Assert.Contains("+map", args);
            Assert.Contains("MAP01", args);
            Assert.Contains("-skill", args);
            Assert.Contains("4", args);
        }

        [Fact]
        public void ToCommandLineArgs_WithPort_IncludesPort()
        {
            var config = new WadLaunchConfiguration
            {
                Iwad = "doom2.wad",
                Port = 12345
            };

            var args = _serializer.ToCommandLineArgs(config);

            Assert.Contains("-port", args);
            Assert.Contains("12345", args);
        }

        [Fact]
        public void ToCommandLineArgs_WithMultiplePlayers_IncludesHost()
        {
            var config = new WadLaunchConfiguration
            {
                Iwad = "doom2.wad",
                Players = 4
            };

            var args = _serializer.ToCommandLineArgs(config);

            Assert.Contains("-host", args);
            Assert.Contains("4", args);
        }

        [Fact]
        public void ToCommandLineArgs_SinglePlayer_NoHostFlag()
        {
            var config = new WadLaunchConfiguration
            {
                Iwad = "doom2.wad",
                Players = 1
            };

            var args = _serializer.ToCommandLineArgs(config);

            Assert.DoesNotContain("-host", args);
        }

        [Fact]
        public void Serialize_ProducesConfigContent()
        {
            var config = new WadLaunchConfiguration
            {
                Iwad = "doom2.wad",
                Files = { "mod.pk3" },
                Map = "MAP01",
                Skill = 4,
                Mode = GameMode.Deathmatch,
                Port = 10666
            };

            var result = _serializer.Serialize(config);

            Assert.Contains("[%General]", result);
            Assert.Contains("engine=Zandronum", result);
            Assert.Contains("iwad=doom2.wad", result);
            Assert.Contains("map=MAP01", result);
            Assert.Contains("port=10666", result);
            Assert.Contains("gamemode=2", result);
            Assert.Contains("[Rules]", result);
            Assert.Contains("difficulty=4", result);
        }

        [Fact]
        public void Deserialize_ParsesJoinString()
        {
            var joinString =
                @"C:/zandronum.exe -iwad C:/doom2.wad -file C:/mod1.pk3 -file C:/mod2.wad -skill 3 +map MAP05 ";

            var config = _serializer.Deserialize(joinString);

            Assert.Equal("C:/doom2.wad", config.Iwad);
            Assert.Equal(2, config.Files.Count);
            Assert.Contains("C:/mod1.pk3", config.Files);
            Assert.Contains("C:/mod2.wad", config.Files);
            Assert.Equal(3, config.Skill);
            Assert.Equal("MAP05", config.Map);
        }

        [Fact]
        public void Deserialize_ParsesIniConfig()
        {
            var ini = @"
[%General]
engine=Zandronum
iwad=doom2.wad
pwads=""mod1.pk3;mod2.wad""
map=MAP03
port=10666
gamemode=1

[Rules]
difficulty=4
maxPlayers=8
";

            var config = _serializer.Deserialize(ini);

            Assert.Equal("doom2.wad", config.Iwad);
            Assert.Equal(2, config.Files.Count);
            Assert.Equal("MAP03", config.Map);
            Assert.Equal(10666, config.Port);
            Assert.Equal(GameMode.Cooperative, config.Mode);
            Assert.Equal(4, config.Skill);
            Assert.Equal(8, config.Players);
        }

        [Fact]
        public void RoundTrip_SerializeThenDeserialize_PreservesFields()
        {
            var original = new WadLaunchConfiguration
            {
                Iwad = "doom2.wad",
                Files = { "mod1.pk3", "mod2.wad" },
                Map = "MAP01",
                Skill = 4,
                Mode = GameMode.Deathmatch,
                Players = 8,
                Port = 10666
            };

            var serialized = _serializer.Serialize(original);
            var deserialized = _serializer.Deserialize(serialized);

            Assert.Equal(original.Iwad, deserialized.Iwad);
            Assert.Equal(original.Files.Count, deserialized.Files.Count);
            Assert.Equal(original.Map, deserialized.Map);
            Assert.Equal(original.Skill, deserialized.Skill);
            Assert.Equal(original.Mode, deserialized.Mode);
            Assert.Equal(original.Players, deserialized.Players);
            Assert.Equal(original.Port, deserialized.Port);
        }

        [Fact]
        public void ToCommandLineArgs_NoOptionalFields_OnlyIwad()
        {
            var config = new WadLaunchConfiguration { Iwad = "doom2.wad" };
            var args = _serializer.ToCommandLineArgs(config);

            Assert.Equal(new[] { "-iwad", "doom2.wad" }, args);
        }

        [Fact]
        public void Serialize_DmflagsInExtraParameters_Used()
        {
            var config = new WadLaunchConfiguration
            {
                Iwad = "doom2.wad"
            };
            config.ExtraParameters["NoMonsters"] = "1";

            var result = _serializer.Serialize(config);

            Assert.Contains("NoMonsters=1", result);
        }
    }
}
