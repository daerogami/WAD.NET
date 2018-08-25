using System;
using WAD.NET.Concrete;

namespace WAD.NET.Interfaces
{
    interface IWadReader: IDisposable
    {
        Wad ReadWad();
    }
}
