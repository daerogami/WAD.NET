namespace WAD.NET.UDMF.Fields
{
    /// <summary>
    /// Sector
    /// </summary>
    /// <remarks>
    /// As defined by UDMF 1.1
    /// <see cref="https://github.com/coelckers/gzdoom/blob/master/specs/udmf.txt"/>
    /// </remarks>
    struct Sector
    {
        /// <summary>
        /// Floor height. Default = 0.
        /// </summary>
        int heightFloor;

        /// <summary>
        /// Ceiling height. Default = 0.
        /// </summary>
        int heightceiling;

        /// <summary>
        /// Floor flat. No valid default.
        /// </summary>
        string texturefloor;

        /// <summary>
        /// Ceiling flat. No valid default.
        /// </summary>
        string textureceiling;

        /// <summary>
        /// Light level. Default = 160.
        /// </summary>
        int lightlevel;

        /// <summary>
        /// Sector special. Default = 0.
        /// </summary>
        int special;

        /// <summary>
        /// Sector tag/id. Default = 0.
        /// </summary>
        int id;

        /// <summary>
        /// A comment. Implementors should attach no special semantic meaning to this field.
        /// </summary>
        string comment;
    }
}
