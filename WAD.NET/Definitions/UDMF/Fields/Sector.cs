namespace WAD.NET.UDMF.Fields
{
    /// <summary>
    /// UDMF Sector definition.
    /// </summary>
    /// <remarks>
    /// As defined by UDMF 1.1
    /// <see cref="https://github.com/coelckers/gzdoom/blob/master/specs/udmf.txt"/>
    /// </remarks>
    public struct Sector
    {
        /// <summary>
        /// Floor height. Default = 0.
        /// </summary>
        public int HeightFloor { get; set; }

        /// <summary>
        /// Ceiling height. Default = 0.
        /// </summary>
        public int HeightCeiling { get; set; }

        /// <summary>
        /// Floor flat. No valid default.
        /// </summary>
        public string? TextureFloor { get; set; }

        /// <summary>
        /// Ceiling flat. No valid default.
        /// </summary>
        public string? TextureCeiling { get; set; }

        /// <summary>
        /// Light level. Default = 160.
        /// </summary>
        public int LightLevel { get; set; }

        /// <summary>
        /// Sector special. Default = 0.
        /// </summary>
        public int Special { get; set; }

        /// <summary>
        /// Sector tag/id. Default = 0.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// A comment. Implementors should attach no special semantic meaning to this field.
        /// </summary>
        public string? Comment { get; set; }

        public Sector()
        {
            HeightFloor = 0;
            HeightCeiling = 0;
            TextureFloor = null;
            TextureCeiling = null;
            LightLevel = 160;
            Special = 0;
            Id = 0;
            Comment = null;
        }
    }
}
