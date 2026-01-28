using Xunit;
using WAD.NET.LaunchConfig;

namespace WAD.NET.Tests
{
    public class LaunchConfigTests
    {
        [Fact]
        public void Defaults_FilesAndExtraParametersInitialized()
        {
            var config = new WadLaunchConfiguration();

            Assert.NotNull(config.Files);
            Assert.Empty(config.Files);
            Assert.NotNull(config.ExtraParameters);
            Assert.Empty(config.ExtraParameters);
            Assert.Null(config.Map);
            Assert.Null(config.Skill);
            Assert.Null(config.Mode);
            Assert.Null(config.Players);
            Assert.Null(config.Port);
        }

        [Fact]
        public void AllOptionalProperties_CanBeSet()
        {
            var config = new WadLaunchConfiguration
            {
                Iwad = "doom2.wad",
                Files = { "mod1.pk3", "mod2.wad" },
                Map = "MAP01",
                Skill = 4,
                Mode = GameMode.Cooperative,
                Players = 4,
                Port = 10666
            };

            Assert.Equal("doom2.wad", config.Iwad);
            Assert.Equal(2, config.Files.Count);
            Assert.Equal("MAP01", config.Map);
            Assert.Equal(4, config.Skill);
            Assert.Equal(GameMode.Cooperative, config.Mode);
            Assert.Equal(4, config.Players);
            Assert.Equal(10666, config.Port);
        }

        [Fact]
        public void ExtraParameters_Work()
        {
            var config = new WadLaunchConfiguration
            {
                Iwad = "doom2.wad"
            };
            config.ExtraParameters["dmflags"] = "1234";
            config.ExtraParameters["custom"] = "value";

            Assert.Equal(2, config.ExtraParameters.Count);
            Assert.Equal("1234", config.ExtraParameters["dmflags"]);
            Assert.Equal("value", config.ExtraParameters["custom"]);
        }

        [Fact]
        public void GameMode_AllValuesExist()
        {
            Assert.Equal(0, (int)GameMode.SinglePlayer);
            Assert.Equal(1, (int)GameMode.Cooperative);
            Assert.Equal(2, (int)GameMode.Deathmatch);
            Assert.Equal(3, (int)GameMode.TeamDeathmatch);
            Assert.Equal(4, (int)GameMode.CaptureTheFlag);
            Assert.Equal(5, (int)GameMode.LastManStanding);
            Assert.Equal(6, (int)GameMode.Survival);
        }
    }
}
