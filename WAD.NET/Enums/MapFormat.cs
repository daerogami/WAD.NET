namespace WAD.NET.Enums
{
    /// <summary>
    /// Identifies the format of a map within a WAD file.
    /// </summary>
    public enum MapFormat
    {
        /// <summary>
        /// Unknown or unrecognized map format.
        /// </summary>
        Unknown,

        /// <summary>
        /// Original DOOM format (DOOM, DOOM II, Heretic).
        /// Binary lumps with 10-byte things, 14-byte linedefs.
        /// </summary>
        Doom,

        /// <summary>
        /// Hexen extended format with ACS support.
        /// Binary lumps with 20-byte things, 16-byte linedefs, and BEHAVIOR lump.
        /// </summary>
        Hexen,

        /// <summary>
        /// Universal Doom Map Format (text-based).
        /// Uses TEXTMAP lump with human-readable syntax.
        /// </summary>
        UDMF
    }
}
