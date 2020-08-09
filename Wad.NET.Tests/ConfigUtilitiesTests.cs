using Xunit;
using WAD.NET.Concrete;
using System.IO;
using WAD.NET.Enums;
using System;

namespace WAD.NET.Tests
{
    public class ConfigUtilitiesTests
    {
        public static string TestWadLocation = @"D:\Games\Zandronum\PWADS\";

        [Fact]
        public void SuccessfullyConvertJoinCommand()
        {
            var joinString =
                @"D:/Games/Zandronum/zandronum.exe -connect 149.56.242.162:10702 -iwad D:/Games/Zandronum/PWADS/doom2.wad -file D:/Games/Zandronum/PWADS/odaddon_2019105.pk7 -file D:/Games/Zandronum/PWADS/complex-doom.v26a2.pk3 -file D:/Games/Zandronum/PWADS/complex-doom.v26a2-nopush-v6.pk3 -file D:/Games/Zandronum/PWADS/lca-v1.5.9.6.pk3 -file D:/Games/Zandronum/PWADS/lca-v1.5.9-nopush-v2.pk3 -file D:/Games/Zandronum/PWADS/randommons-v1.2.4-server.only.pk3 -file D:/Games/Zandronum/PWADS/complex-dust-v1.7.pk3 -file D:/Games/Zandronum/PWADS/lca-djb-v4.4.3.pk3 -file D:/Games/Zandronum/PWADS/complex-lca-djb-dust-rm-support-v6.pk3 -file D:/Games/Zandronum/PWADS/complex-doom-justammo.v4.wad -file D:/Games/Zandronum/PWADS/hpbar-v16.pk3 -file D:/Games/Zandronum/PWADS/complex-morphitemfixes-lcarm-v1.pk3 -file D:/Games/Zandronum/PWADS/complex-menus-v2.pk3 -file D:/Games/Zandronum/PWADS/connectsound.wad -file D:/Games/Zandronum/PWADS/newtextcolours_260.pk3 -file D:/Games/Zandronum/PWADS/evecdsp-v3d.wad";
            
            var result = ConfigUtilities.ConvertZandronumJoinStringToConfigFile(joinString, null);

            Assert.True(!string.IsNullOrWhiteSpace(result));
        }
    }
}
