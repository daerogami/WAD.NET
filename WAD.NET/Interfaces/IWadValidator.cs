using WAD.NET.Concrete;

namespace WAD.NET.Interfaces
{
    public interface IWadValidator
    {
        bool IsValid(Wad wad);
        bool IsValid(IWadConfig wad);
    }
}
