using WAD.NET.Abstract;

namespace WAD.NET.Concrete
{
    public sealed class TextureListLump : Lump
    {
        public TextureListLump(string lumpName, string sourceWadFileName)
            : base(lumpName, sourceWadFileName)
        {

        }
    }
}