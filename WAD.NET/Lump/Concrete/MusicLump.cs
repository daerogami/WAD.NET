using WAD.NET.Abstract;

namespace WAD.NET.Concrete
{
    public sealed class MusicLump : Lump
    {
        public byte[] MusicData { get; }


        public MusicLump(string lumpName, string sourceWadFileName, byte[] musicData):
            base(lumpName, sourceWadFileName)
        {
            MusicData = musicData;
        }
    }
}