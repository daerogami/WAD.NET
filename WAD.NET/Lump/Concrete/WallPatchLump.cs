using WAD.NET.Abstract;

namespace WAD.NET.Concrete
{
    public sealed class WallPatchLump : Lump
    {
        public WallPatchLump(string lumpName, string sourceWadFileName)
            : base(lumpName, sourceWadFileName)
        {

        }
    }
}