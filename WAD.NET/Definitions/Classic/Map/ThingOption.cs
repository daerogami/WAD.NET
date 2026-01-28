using System;

namespace WAD.NET.Definitions.Classic.Map
{
    [Flags]
    public enum ThingOption
    {
        None = 0,
        SkillsOneAndTwo = 1,
        SkillThree = 2,
        SkillsFourAndFive = 4,
        Deaf = 8,
        MultiplayerOnly = 16
    }
}