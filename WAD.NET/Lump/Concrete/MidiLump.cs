using WAD.NET.Abstract;

namespace WAD.NET.Concrete
{
    public sealed class MidiLump : Lump
    {
        public MidiLump(string lumpName, string sourceWadFileName)
            : base(lumpName, sourceWadFileName)
        {

        }
    }
}