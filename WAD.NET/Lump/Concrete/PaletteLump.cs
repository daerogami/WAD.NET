using WAD.NET.Abstract;

namespace WAD.NET.Concrete
{
    public sealed class PaletteLump : Lump
    {
        public PaletteLump(string lumpName, string sourceWadFileName)
            : base(lumpName, sourceWadFileName)
        {

        }
    }
}