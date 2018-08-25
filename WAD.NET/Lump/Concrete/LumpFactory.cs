using WAD.NET.Abstract;

namespace WAD.NET.Concrete
{
    public static class LumpFactory
    {
        public static Lump GenerateBinaryLumpFromData(string name, byte[] data)
        {
            return new BinaryLump { Name = name, Data = data};
        }
    }
}
