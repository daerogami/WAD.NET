namespace WAD.NET.UDMF.Fields
{
    /// <summary>
    /// Side Definition
    /// </summary>
    /// <remarks>
    /// As defined by UDMF 1.1
    /// <see cref="https://github.com/coelckers/gzdoom/blob/master/specs/udmf.txt"/>
    /// </remarks>
    struct SideDefinition
    {
        /// <summary>
        /// X Offset. Default = 0.
        /// </summary>
        int offsetx;

        /// <summary>
        /// Y Offset. Default = 0.
        /// </summary>
        int offsety;

        /// <summary>
        /// Upper texture. Default = "-".
        /// </summary>
        string texturetop;

        /// <summary>
        /// Lower texture. Default = "-".
        /// </summary>
        string texturebottom;

        /// <summary>
        /// Middle texture. Default = "-".
        /// </summary>
        string texturemiddle;

        /// <summary>
        /// Sector index. No valid default.
        /// </summary>
        int sector;

        /// <summary>
        /// A comment. Implementors should attach no special semantic meaning to this field.
        /// </summary>
        string comment;
    }
}
