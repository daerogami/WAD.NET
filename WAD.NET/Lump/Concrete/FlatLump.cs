using WAD.NET.Abstract;

namespace WAD.NET.Concrete
{
    public sealed class FlatLump : Lump
    {
        public FlatLump(string lumpName, string sourceWadFileName)
            : base(lumpName, sourceWadFileName)
        {

        }
    }
}