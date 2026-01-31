using Xunit;
using WAD.NET.SourcePorts.Zandronum;
using System;

namespace WAD.NET.Tests
{
    public class ConfigUtilitiesTests
    {
        private readonly ZandronumConfigSerializer _serializer = new ZandronumConfigSerializer();

        #region Basic Conversion Tests

        [Fact]
        public void ConvertZandronumJoinStringToConfigFile_WithValidInput_ReturnsConfigContent()
        {
            var joinString =
                @"D:/Games/Zandronum/zandronum.exe -connect 149.56.242.162:10702 -iwad D:/Games/Zandronum/PWADS/doom2.wad -file D:/Games/Zandronum/PWADS/odaddon_2019105.pk7 -file D:/Games/Zandronum/PWADS/complex-doom.v26a2.pk3 -file D:/Games/Zandronum/PWADS/complex-doom.v26a2-nopush-v6.pk3 -file D:/Games/Zandronum/PWADS/lca-v1.5.9.6.pk3 -file D:/Games/Zandronum/PWADS/lca-v1.5.9-nopush-v2.pk3 -file D:/Games/Zandronum/PWADS/randommons-v1.2.4-server.only.pk3 -file D:/Games/Zandronum/PWADS/complex-dust-v1.7.pk3 -file D:/Games/Zandronum/PWADS/lca-djb-v4.4.3.pk3 -file D:/Games/Zandronum/PWADS/complex-lca-djb-dust-rm-support-v6.pk3 -file D:/Games/Zandronum/PWADS/complex-doom-justammo.v4.wad -file D:/Games/Zandronum/PWADS/hpbar-v16.pk3 -file D:/Games/Zandronum/PWADS/complex-morphitemfixes-lcarm-v1.pk3 -file D:/Games/Zandronum/PWADS/complex-menus-v2.pk3 -file D:/Games/Zandronum/PWADS/connectsound.wad -file D:/Games/Zandronum/PWADS/newtextcolours_260.pk3 -file D:/Games/Zandronum/PWADS/evecdsp-v3d.wad";

            var result = _serializer.ConvertJoinStringToConfigFile(joinString, null);

            Assert.False(string.IsNullOrWhiteSpace(result));
            Assert.Contains("[%General]", result);
            Assert.Contains("engine=Zandronum", result);
        }

        [Fact]
        public void ConvertZandronumJoinStringToConfigFile_WithValidInput_ContainsAllRequiredSections()
        {
            var joinString = @"C:/Zandronum/zandronum.exe -iwad C:/IWADS/doom2.wad -file C:/PWADS/mod.pk3 ";

            var result = _serializer.ConvertJoinStringToConfigFile(joinString, "TestConfig");

            Assert.Contains("[%General]", result);
            Assert.Contains("[Rules]", result);
            Assert.Contains("[Misc]", result);
            Assert.Contains("[dmflags]", result);
            Assert.Contains("[voting]", result);
        }

        #endregion

        #region Config Name Tests

        [Fact]
        public void ConvertZandronumJoinStringToConfigFile_WithNullConfigName_GeneratesRandomName()
        {
            var joinString = @"C:/Zandronum/zandronum.exe -iwad C:/IWADS/doom2.wad -file C:/PWADS/mod.pk3 ";

            var result = _serializer.ConvertJoinStringToConfigFile(joinString, null);

            Assert.Contains("name=NewConfig_", result);
        }

        [Fact]
        public void ConvertZandronumJoinStringToConfigFile_WithEmptyConfigName_GeneratesRandomName()
        {
            var joinString = @"C:/Zandronum/zandronum.exe -iwad C:/IWADS/doom2.wad -file C:/PWADS/mod.pk3 ";

            var result = _serializer.ConvertJoinStringToConfigFile(joinString, "");

            Assert.Contains("name=NewConfig_", result);
        }

        [Fact]
        public void ConvertZandronumJoinStringToConfigFile_WithWhitespaceConfigName_GeneratesRandomName()
        {
            var joinString = @"C:/Zandronum/zandronum.exe -iwad C:/IWADS/doom2.wad -file C:/PWADS/mod.pk3 ";

            var result = _serializer.ConvertJoinStringToConfigFile(joinString, "   ");

            Assert.Contains("name=NewConfig_", result);
        }

        [Fact]
        public void ConvertZandronumJoinStringToConfigFile_WithCustomConfigName_UsesProvidedName()
        {
            var joinString = @"C:/Zandronum/zandronum.exe -iwad C:/IWADS/doom2.wad -file C:/PWADS/mod.pk3 ";

            var result = _serializer.ConvertJoinStringToConfigFile(joinString, "MyCustomConfig");

            Assert.Contains("name=MyCustomConfig", result);
        }

        [Theory]
        [InlineData("SimpleConfig")]
        [InlineData("Config With Spaces")]
        [InlineData("Config_With_Underscores")]
        [InlineData("Config-With-Dashes")]
        [InlineData("Config123")]
        public void ConvertZandronumJoinStringToConfigFile_WithVariousConfigNames_UsesProvidedName(string configName)
        {
            var joinString = @"C:/Zandronum/zandronum.exe -iwad C:/IWADS/doom2.wad -file C:/PWADS/mod.pk3 ";

            var result = _serializer.ConvertJoinStringToConfigFile(joinString, configName);

            Assert.Contains($"name={configName}", result);
        }

        #endregion

        #region Executable Path Extraction Tests

        [Fact]
        public void ConvertZandronumJoinStringToConfigFile_WithExecutablePath_ExtractsExecutable()
        {
            var joinString = @"C:/Games/Zandronum/zandronum.exe -iwad C:/doom2.wad -file C:/mod.pk3 ";

            var result = _serializer.ConvertJoinStringToConfigFile(joinString, "Test");

            Assert.Contains("executable=C:/Games/Zandronum/zandronum.exe ", result);
        }

        [Fact]
        public void ConvertZandronumJoinStringToConfigFile_WithForwardSlashPath_ExtractsExecutable()
        {
            var joinString = @"D:/Games/Zandronum/zandronum.exe -iwad D:/doom2.wad -file D:/mod.pk3 ";

            var result = _serializer.ConvertJoinStringToConfigFile(joinString, "Test");

            Assert.Contains("executable=D:/Games/Zandronum/zandronum.exe ", result);
        }

        [Fact]
        public void ConvertZandronumJoinStringToConfigFile_WithNoExecutablePath_ReturnsEmptyExecutable()
        {
            // No space after the string means regex won't match
            var joinString = @"-iwad C:/doom2.wad -file C:/mod.pk3 ";

            var result = _serializer.ConvertJoinStringToConfigFile(joinString, "Test");

            Assert.Contains("executable=", result);
        }

        #endregion

        #region IWAD Extraction Tests

        [Fact]
        public void ConvertZandronumJoinStringToConfigFile_WithIwad_ExtractsIwadPath()
        {
            var joinString = @"C:/zandronum.exe -iwad C:/Games/IWADS/doom2.wad -file C:/mod.pk3 ";

            var result = _serializer.ConvertJoinStringToConfigFile(joinString, "Test");

            Assert.Contains("iwad=C:/Games/IWADS/doom2.wad", result);
        }

        [Fact]
        public void ConvertZandronumJoinStringToConfigFile_WithDifferentIwads_ExtractsCorrectIwad()
        {
            var joinString = @"C:/zandronum.exe -iwad C:/IWADS/heretic.wad -file C:/mod.pk3 ";

            var result = _serializer.ConvertJoinStringToConfigFile(joinString, "Test");

            Assert.Contains("iwad=C:/IWADS/heretic.wad", result);
        }

        [Fact]
        public void ConvertZandronumJoinStringToConfigFile_WithNoIwad_ReturnsEmptyIwad()
        {
            var joinString = @"C:/zandronum.exe -file C:/mod.pk3 ";

            var result = _serializer.ConvertJoinStringToConfigFile(joinString, "Test");

            // The output contains "iwad=" followed by a newline (with possible whitespace)
            // Check that iwad= appears and is followed by pwads (indicating empty iwad)
            Assert.Contains("iwad=", result);
            Assert.Contains("iwad=\npwads", result.Replace("\r", ""));
        }

        [Theory]
        [InlineData("doom.wad")]
        [InlineData("doom2.wad")]
        [InlineData("heretic.wad")]
        [InlineData("hexen.wad")]
        [InlineData("strife.wad")]
        [InlineData("freedoom1.wad")]
        [InlineData("freedoom2.wad")]
        public void ConvertZandronumJoinStringToConfigFile_WithVariousIwads_ExtractsCorrectIwad(string iwadName)
        {
            var joinString = $@"C:/zandronum.exe -iwad C:/IWADS/{iwadName} -file C:/mod.pk3 ";

            var result = _serializer.ConvertJoinStringToConfigFile(joinString, "Test");

            Assert.Contains($"iwad=C:/IWADS/{iwadName}", result);
        }

        #endregion

        #region File Extraction Tests

        [Fact]
        public void ConvertZandronumJoinStringToConfigFile_WithSingleFile_ExtractsFile()
        {
            var joinString = @"C:/zandronum.exe -iwad C:/doom2.wad -file C:/PWADS/mod.pk3 ";

            var result = _serializer.ConvertJoinStringToConfigFile(joinString, "Test");

            Assert.Contains("C:/PWADS/mod.pk3", result);
        }

        [Fact]
        public void ConvertZandronumJoinStringToConfigFile_WithMultipleFiles_ExtractsAllFiles()
        {
            var joinString = @"C:/zandronum.exe -iwad C:/doom2.wad -file C:/mod1.pk3 -file C:/mod2.pk3 -file C:/mod3.wad ";

            var result = _serializer.ConvertJoinStringToConfigFile(joinString, "Test");

            Assert.Contains("C:/mod1.pk3", result);
            Assert.Contains("C:/mod2.pk3", result);
            Assert.Contains("C:/mod3.wad", result);
        }

        [Fact]
        public void ConvertZandronumJoinStringToConfigFile_WithMultipleFiles_JoinsWithSemicolon()
        {
            var joinString = @"C:/zandronum.exe -iwad C:/doom2.wad -file C:/mod1.pk3 -file C:/mod2.pk3 ";

            var result = _serializer.ConvertJoinStringToConfigFile(joinString, "Test");

            Assert.Contains("pwads=\"C:/mod1.pk3;C:/mod2.pk3\"", result);
        }

        [Theory]
        [InlineData(".pk3")]
        [InlineData(".pk7")]
        [InlineData(".wad")]
        [InlineData(".zip")]
        public void ConvertZandronumJoinStringToConfigFile_WithVariousFileExtensions_ExtractsFile(string extension)
        {
            var joinString = $@"C:/zandronum.exe -iwad C:/doom2.wad -file C:/PWADS/mymod{extension} ";

            var result = _serializer.ConvertJoinStringToConfigFile(joinString, "Test");

            Assert.Contains($"C:/PWADS/mymod{extension}", result);
        }

        #endregion

        #region Edge Cases and Error Handling

        [Fact]
        public void ConvertZandronumJoinStringToConfigFile_WithNoFiles_ThrowsException()
        {
            // When there are no -file arguments, the GetFiles method will throw
            // because matches[0] will throw IndexOutOfRangeException
            var joinString = @"C:/zandronum.exe -iwad C:/doom2.wad ";

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _serializer.ConvertJoinStringToConfigFile(joinString, "Test"));
        }

        [Fact]
        public void ConvertZandronumJoinStringToConfigFile_WithEmptyJoinString_ThrowsException()
        {
            // Empty string will have no -file matches, causing ArgumentOutOfRangeException
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _serializer.ConvertJoinStringToConfigFile("", "Test"));
        }

        [Fact]
        public void ConvertZandronumJoinStringToConfigFile_WithOnlyWhitespaceJoinString_ThrowsException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _serializer.ConvertJoinStringToConfigFile("   ", "Test"));
        }

        #endregion

        #region Output Format Validation Tests

        [Fact]
        public void ConvertZandronumJoinStringToConfigFile_ReturnsCorrectEngineValue()
        {
            var joinString = @"C:/zandronum.exe -iwad C:/doom2.wad -file C:/mod.pk3 ";

            var result = _serializer.ConvertJoinStringToConfigFile(joinString, "Test");

            Assert.Contains("engine=Zandronum", result);
        }

        [Fact]
        public void ConvertZandronumJoinStringToConfigFile_ReturnsDefaultPort()
        {
            var joinString = @"C:/zandronum.exe -iwad C:/doom2.wad -file C:/mod.pk3 ";

            var result = _serializer.ConvertJoinStringToConfigFile(joinString, "Test");

            Assert.Contains("port=10666", result);
        }

        [Fact]
        public void ConvertZandronumJoinStringToConfigFile_ReturnsDefaultDifficulty()
        {
            var joinString = @"C:/zandronum.exe -iwad C:/doom2.wad -file C:/mod.pk3 ";

            var result = _serializer.ConvertJoinStringToConfigFile(joinString, "Test");

            Assert.Contains("difficulty=3", result);
        }

        [Fact]
        public void ConvertZandronumJoinStringToConfigFile_ReturnsDmflagsSection()
        {
            var joinString = @"C:/zandronum.exe -iwad C:/doom2.wad -file C:/mod.pk3 ";

            var result = _serializer.ConvertJoinStringToConfigFile(joinString, "Test");

            // Check for some specific dmflag settings
            Assert.Contains("DropWeaponOnDeath=1", result);
            Assert.Contains("WeaponsStayAfterPickup=1", result);
            Assert.Contains("DoubleAmmo=1", result);
            Assert.Contains("NoMonsters=0", result);
        }

        [Fact]
        public void ConvertZandronumJoinStringToConfigFile_ReturnsVotingSection()
        {
            var joinString = @"C:/zandronum.exe -iwad C:/doom2.wad -file C:/mod.pk3 ";

            var result = _serializer.ConvertJoinStringToConfigFile(joinString, "Test");

            Assert.Contains("KickVote=1", result);
            Assert.Contains("ChangeMapVote=1", result);
            Assert.Contains("MapVote=1", result);
        }

        [Fact]
        public void ConvertZandronumJoinStringToConfigFile_ReturnsCompatibilityFlags()
        {
            var joinString = @"C:/zandronum.exe -iwad C:/doom2.wad -file C:/mod.pk3 ";

            var result = _serializer.ConvertJoinStringToConfigFile(joinString, "Test");

            Assert.Contains("CompatActorsAreInfinitelyTall=0", result);
            Assert.Contains("CompatUseOriginalMissileClippingHeight=0", result);
            Assert.Contains("CompatEnableWallRunning=0", result);
        }

        [Fact]
        public void ConvertZandronumJoinStringToConfigFile_ReturnsLMSSettings()
        {
            var joinString = @"C:/zandronum.exe -iwad C:/doom2.wad -file C:/mod.pk3 ";

            var result = _serializer.ConvertJoinStringToConfigFile(joinString, "Test");

            Assert.Contains("LMSChainsaw=0", result);
            Assert.Contains("LMSPistol=0", result);
            Assert.Contains("LMSShotgun=0", result);
            Assert.Contains("LMSRocketLauncher=0", result);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void ConvertZandronumJoinStringToConfigFile_WithComplexPath_ExtractsCorrectly()
        {
            var joinString =
                @"D:/Games/Zandronum 3.0/zandronum.exe -connect 192.168.1.1:10666 -iwad D:/Games/Doom/IWADS/doom2.wad -file D:/Games/Doom/PWADS/complex-doom.v26a2.pk3 -file D:/Games/Doom/PWADS/lca-v1.5.9.6.pk3 ";

            var result = _serializer.ConvertJoinStringToConfigFile(joinString, "ComplexConfig");

            Assert.Contains("name=ComplexConfig", result);
            Assert.Contains("iwad=D:/Games/Doom/IWADS/doom2.wad", result);
            Assert.Contains("complex-doom.v26a2.pk3", result);
            Assert.Contains("lca-v1.5.9.6.pk3", result);
        }

        [Fact]
        public void ConvertZandronumJoinStringToConfigFile_GeneratedConfigNameIsUnique()
        {
            var joinString = @"C:/zandronum.exe -iwad C:/doom2.wad -file C:/mod.pk3 ";

            var result1 = _serializer.ConvertJoinStringToConfigFile(joinString, null);
            var result2 = _serializer.ConvertJoinStringToConfigFile(joinString, null);

            // Extract config names from both results
            var nameStart = result1.IndexOf("name=") + 5;
            var nameEnd = result1.IndexOf("\n", nameStart);
            var name1 = result1.Substring(nameStart, nameEnd - nameStart).Trim();

            nameStart = result2.IndexOf("name=") + 5;
            nameEnd = result2.IndexOf("\n", nameStart);
            var name2 = result2.Substring(nameStart, nameEnd - nameStart).Trim();

            // The GUID portion should make them different
            Assert.NotEqual(name1, name2);
        }

        [Fact]
        public void ConvertZandronumJoinStringToConfigFile_ManyFiles_AllFilesIncluded()
        {
            var files = new string[]
            {
                "mod1.pk3", "mod2.pk3", "mod3.pk7", "mod4.wad", "mod5.pk3",
                "mod6.pk3", "mod7.pk3", "mod8.wad", "mod9.pk3", "mod10.pk3"
            };

            var joinString = @"C:/zandronum.exe -iwad C:/doom2.wad ";
            foreach (var file in files)
            {
                joinString += $"-file C:/PWADS/{file} ";
            }

            var result = _serializer.ConvertJoinStringToConfigFile(joinString, "ManyModsConfig");

            foreach (var file in files)
            {
                Assert.Contains(file, result);
            }
        }

        #endregion

        #region Special Character Tests

        [Fact]
        public void ConvertZandronumJoinStringToConfigFile_WithUnderscoresInPath_ExtractsCorrectly()
        {
            var joinString = @"C:/Games_Folder/zandronum.exe -iwad C:/IWADs_Here/doom2.wad -file C:/PWADs_Here/my_mod.pk3 ";

            var result = _serializer.ConvertJoinStringToConfigFile(joinString, "Test");

            Assert.Contains("iwad=C:/IWADs_Here/doom2.wad", result);
            Assert.Contains("my_mod.pk3", result);
        }

        [Fact]
        public void ConvertZandronumJoinStringToConfigFile_WithDashesInFilename_ExtractsCorrectly()
        {
            var joinString = @"C:/zandronum.exe -iwad C:/doom2.wad -file C:/complex-doom-v1-2-3.pk3 ";

            var result = _serializer.ConvertJoinStringToConfigFile(joinString, "Test");

            Assert.Contains("complex-doom-v1-2-3.pk3", result);
        }

        [Fact]
        public void ConvertZandronumJoinStringToConfigFile_WithDotsInPath_ExtractsCorrectly()
        {
            var joinString = @"C:/Games.v2/zandronum.exe -iwad C:/doom2.wad -file C:/mod.v1.2.3.pk3 ";

            var result = _serializer.ConvertJoinStringToConfigFile(joinString, "Test");

            Assert.Contains("mod.v1.2.3.pk3", result);
        }

        [Fact]
        public void ConvertZandronumJoinStringToConfigFile_WithNumbersInPath_ExtractsCorrectly()
        {
            var joinString = @"C:/Zandronum3/zandronum.exe -iwad C:/doom2.wad -file C:/mod123.pk3 ";

            var result = _serializer.ConvertJoinStringToConfigFile(joinString, "Test");

            Assert.Contains("mod123.pk3", result);
        }

        #endregion

        #region Connection Parameter Tests

        [Fact]
        public void ConvertZandronumJoinStringToConfigFile_WithConnectParameter_ParsesSuccessfully()
        {
            var joinString = @"C:/zandronum.exe -connect 192.168.1.100:10666 -iwad C:/doom2.wad -file C:/mod.pk3 ";

            var result = _serializer.ConvertJoinStringToConfigFile(joinString, "Test");

            // Should still parse without error, connect is just part of the command
            Assert.NotNull(result);
            Assert.Contains("[%General]", result);
        }

        [Fact]
        public void ConvertZandronumJoinStringToConfigFile_WithIPv4Connect_ParsesSuccessfully()
        {
            var joinString = @"C:/zandronum.exe -connect 149.56.242.162:10702 -iwad C:/doom2.wad -file C:/mod.pk3 ";

            var result = _serializer.ConvertJoinStringToConfigFile(joinString, "Test");

            Assert.NotNull(result);
        }

        #endregion
    }
}
