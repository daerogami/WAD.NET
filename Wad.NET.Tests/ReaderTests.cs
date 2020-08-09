using Xunit;
using WAD.NET.Concrete;
using System.IO;
using WAD.NET.Enums;
using System;
using System.Linq;

namespace WAD.NET.Tests
{
    public class ReaderTests
    {
        public static string TestWadLocation = @"D:\Games\Zandronum\PWADS\";

        [Fact]
        public void ICanSuccessfullyReadTheDoomIWADFile()
        {
            const string wadName = "DOOM.WAD";
            Wad wad;

            using (var fileStream = File.OpenRead($"{TestWadLocation}{wadName}"))
            using (var reader = new WadReader(fileStream, wadName: wadName))
            {
                wad = reader.ReadWad();
            };

            Assert.NotNull(wad);
            Assert.Equal(WadType.IWAD, wad.WadType);
        }

        [Fact]
        public void ICanSuccessfullyReadTheDoom2IWADFile()
        {
            const string wadName = "DOOM2.WAD";
            Wad wad;

            using (var fileStream = File.OpenRead($"{TestWadLocation}{wadName}"))
            using (var reader = new WadReader(fileStream))
            {
                wad = reader.ReadWad();
            };

            Assert.NotNull(wad);
            Assert.Equal(WadType.IWAD, wad.WadType);
        }

        [Fact]
        public void ThrowFormatExceptionWhenReadingACompressedFile()
        {
            const string wadName = "rdeimosa.zip";
            using (var fileStream = File.OpenRead($"{TestWadLocation}{wadName}"))
            using (var reader = new WadReader(fileStream))
            {
                Assert.Throws<FormatException>(() => reader.ReadWad());
            };
        }

        [Fact]
        public void TestFile()
        {
            const string wadName = "3ha3.wad";
            Wad wad;

            using (var fileStream = File.OpenRead($@"{TestWadLocation}{wadName}"))
            using (var reader = new WadReader(fileStream))
            {
                wad = reader.ReadWad();
                var thatSong = wad.Lumps.Single(x => x.Name == "D_OPENIN") as MusicLump;
                //File.WriteAllBytes(@"C:/Users/markj/Desktop/d_openin.mid", thatSong.MusicData);
            };
        }
    }
}
