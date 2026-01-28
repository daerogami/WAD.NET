using System;
using System.Collections.Generic;
using WAD.NET.Enums;
using WAD.NET.Interfaces;

namespace WAD.NET.Concrete
{
    public class Wad
    {
        public string Name { get; set; } = string.Empty;
        public Author? Author { get; set; } // TODO: Two-way, many-to-one binding (both ends should populate from junction table)
        public DateTime DateCreated { get; set; }
        public WadType WadType { get; set; }
        public Queue<ILump> Lumps { get; set; } = new Queue<ILump>();

        public Wad()
        {
        }
    }
}
