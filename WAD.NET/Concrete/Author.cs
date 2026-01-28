using System;
using System.Collections.Generic;

namespace WAD.NET.Concrete
{
    public class Author
    {
        public string Name { get; set; } = string.Empty;
        public string? Website { get; set; }
        public IEnumerable<Wad> AuthoredWads { get; set; } = Array.Empty<Wad>(); // TODO: Two-way, one-to-many binding (both ends should populate from junction table)
    }
}