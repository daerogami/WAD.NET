using System.Collections;
using System.Collections.Generic;

namespace WAD.NET.Concrete
{
    public class Author
    {
        public string Name { get; set; }
        public string Website { get; set; }
        public IEnumerable<Wad> AuthoredWads { get; set; } // TODO: Two-way, one-to-many binding (both ends should populate from junction table)
    }
}