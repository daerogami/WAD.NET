using WAD.NET.Interfaces;

namespace WAD.NET.Abstract
{
    public abstract class Lump : ILump
    {
        public string Name { get; }
        public string SourceWad { get; }


        public Lump(string lumpName, string sourceWadFileName)
        {
            Name = lumpName;
            SourceWad = sourceWadFileName;
        }
    }
}
