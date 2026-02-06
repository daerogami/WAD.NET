namespace WAD.NET.Definitions.GameData
{
    /// <summary>
    /// Map naming convention format.
    /// </summary>
    /// <remarks>
    /// Not to be confused with <see cref="Enums.MapFormat"/> which identifies the
    /// binary format of a map (DOOM/Hexen/UDMF).
    /// </remarks>
    public enum MapNameFormat
    {
        /// <summary>DOOM episode format (E#M#).</summary>
        Doom,

        /// <summary>DOOM II format (MAP##).</summary>
        Doom2,

        /// <summary>Hexen format (MAP## with BEHAVIOR).</summary>
        Hexen,

        /// <summary>Non-standard map naming.</summary>
        Custom
    }
}
