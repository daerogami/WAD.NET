using WAD.NET.Abstract;

namespace WAD.NET.Concrete
{
    public sealed class ExitLump : Lump
    {
        public readonly string Text;

        public ExitLump(string lumpName, string sourceWadFileName, string data)
            : base(lumpName, sourceWadFileName)
        {
            Text = data;
        }
    }
}