using System.Collections.Generic;
using WAD.NET.Abstract;
using WAD.NET.Enums;

namespace WAD.NET.Concrete
{
    public class Wad
    {
        public WadType WadType { get; set; }
        public Queue<Lump> Lumps { get; set; }

        public Wad()
        {
            Lumps = new Queue<Lump>();
        }
    }
}
