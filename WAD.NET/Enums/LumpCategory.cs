namespace WAD.NET.Enums
{
    /// <summary>
    /// Categories of lumps/files based on their location or name.
    /// </summary>
    public enum LumpCategory
    {
        /// <summary>Unknown or uncategorized lump.</summary>
        Unknown,

        /// <summary>Map data (maps folder or map lumps).</summary>
        Map,

        /// <summary>Sprite graphics.</summary>
        Sprite,

        /// <summary>Floor/ceiling textures.</summary>
        Flat,

        /// <summary>Wall textures.</summary>
        Texture,

        /// <summary>Texture patches.</summary>
        Patch,

        /// <summary>Sound effects.</summary>
        Sound,

        /// <summary>Music tracks.</summary>
        Music,

        /// <summary>Menu/UI graphics.</summary>
        Graphic,

        /// <summary>Compiled ACS scripts.</summary>
        ACS,

        /// <summary>Script source files (DECORATE, ZScript).</summary>
        Script,

        /// <summary>Definition files (MAPINFO, SNDINFO, etc.).</summary>
        Definition
    }
}
