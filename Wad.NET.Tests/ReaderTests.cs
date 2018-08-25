using Xunit;
using WAD.NET.Concrete;
using System.IO;
using WAD.NET.Enums;
using System;

namespace WAD.NET.Tests
{
    public class ReaderTests
    {
        public static string TestWadLocation = @"D:\Games\Zandronum\PWADS\";

        [Fact]
        public void ICanSuccessfullyReadTheDoomIWADFile()
        {
            Wad wad;

            using (var fileStream = File.OpenRead($"{TestWadLocation}DOOM.WAD"))
            using (var reader = new WadReader(fileStream))
            {
                wad = reader.ReadWad();
            };

            Assert.NotNull(wad);
            Assert.Equal(WadType.IWAD, wad.WadType);
        }

        [Fact]
        public void ICanSuccessfullyReadTheDoom2IWADFile()
        {
            Wad wad;

            using (var fileStream = File.OpenRead($"{TestWadLocation}DOOM2.WAD"))
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
            using (var fileStream = File.OpenRead($"{TestWadLocation}rdeimosa.zip"))
            using (var reader = new WadReader(fileStream))
            {
                Assert.Throws<FormatException>(()=> reader.ReadWad());
            };
        }
    }
}
