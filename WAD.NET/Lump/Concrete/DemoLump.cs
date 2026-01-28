using WAD.NET.Abstract;

namespace WAD.NET.Concrete
{
    public sealed class DemoLump : Lump
    {
        public DemoLump(string lumpName, string sourceWadFileName)
            : base(lumpName, sourceWadFileName)
        {

        }
    }
}