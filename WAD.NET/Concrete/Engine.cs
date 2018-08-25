using System;
using System.Collections.Generic;

namespace WAD.NET.Concrete
{
    // LOW: This may be used in the seed method for WadDB, maybe even set the ids as enums to lock in an engine reference
    public class EngineProvider
    {
        public static IReadOnlyCollection<Engine> SupportedEngines = new List<Engine> {
            new Engine(1, "Doom", new Version(1,9)),
            new Engine(2, "Zandronum", new Version(3,0)),
            new Engine(3, "Odamex", new Version(0,7,0))
        };
    }

    public class Engine
    {
        public int Id { get; }
        public string Name { get; }
        public Version Version { get; }

        public Engine(int id, string name, Version version)
        {

        }
    }
}