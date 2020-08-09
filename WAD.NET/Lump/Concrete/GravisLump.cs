using WAD.NET.Abstract;

namespace WAD.NET.Concrete
{
    public sealed class GravisLump : Lump
    {
        public string Data { get; }

        public GravisLump(string lumpName, string sourceWadFileName, string data)
            : base(lumpName, sourceWadFileName)
        {
            Data = data;
        }
    }
}