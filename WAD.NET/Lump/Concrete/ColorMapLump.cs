using WAD.NET.Abstract;

namespace WAD.NET.Concrete
{
    public sealed class ColorMapLump : Lump
    {
        public ColorMapLump(string lumpName, string sourceWadFileName)
            : base(lumpName, sourceWadFileName)
        {

        }
    }
}