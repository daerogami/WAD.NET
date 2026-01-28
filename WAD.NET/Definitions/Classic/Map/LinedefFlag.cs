using System;

namespace WAD.NET.Definitions.Classic.Map
{
    [Flags]
    public enum LinedefFlag
    {
        None = 0,
        Impassible = 1,
        BlockMonsters = 2,
        TwoSided = 4,
        UpperUnpegged = 8,
        LowerUnpegged = 16,
        Secret = 32,
        BlockSound = 64,
        NotOnMap = 128,
        AlreadyOnMap = 256,
        Unused9 = 512,
        Unused10 = 1024,
        Unused11 = 2048,
        Unused12 = 4096,
        Unused13 = 8192,
        Unused14 = 16384,
        Unused15 = 32768
    }
}