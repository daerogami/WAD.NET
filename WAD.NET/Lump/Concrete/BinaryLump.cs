using WAD.NET.Abstract;

namespace WAD.NET.Concrete
{
    public sealed class BinaryLump : Lump
    {
        public byte[] Data { get; set; }


        public BinaryLump(string name, string sourceWadFileName, byte[] data)
            :base(name, sourceWadFileName)
        {
            Data = data;
        }
    }
}
