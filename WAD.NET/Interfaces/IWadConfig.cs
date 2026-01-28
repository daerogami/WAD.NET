using System.Collections.Generic;
using WAD.NET.Concrete;

namespace WAD.NET.Interfaces
{
    public interface IWadConfig
    {
        Engine CompatibleEngines { get; set; }
        Wad IWad { get; set; }
        Queue<Wad> PWads { get; }
        IDictionary<string, string> AdditionalParamsCollection { get; set; }
    }
}
