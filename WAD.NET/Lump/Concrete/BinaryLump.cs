using System;
using System.Collections.Generic;
using System.Text;
using WAD.NET.Abstract;

namespace WAD.NET.Concrete
{
    public class BinaryLump : Lump
    {
        public byte[] Data { get; set; }
    }
}
