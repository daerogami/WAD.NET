namespace WAD.NET.UDMF
{
    /// <summary>
    /// Represents a sidedef in a UDMF map.
    /// </summary>
    public class UdmfSidedef
    {
        /// <summary>
        /// Sector index this sidedef faces.
        /// </summary>
        public int Sector { get; set; }

        /// <summary>
        /// X texture offset.
        /// </summary>
        public double OffsetX { get; set; }

        /// <summary>
        /// Y texture offset.
        /// </summary>
        public double OffsetY { get; set; }

        /// <summary>
        /// Upper texture name.
        /// </summary>
        public string TextureTop { get; set; } = "-";

        /// <summary>
        /// Lower texture name.
        /// </summary>
        public string TextureBottom { get; set; } = "-";

        /// <summary>
        /// Middle texture name.
        /// </summary>
        public string TextureMiddle { get; set; } = "-";

        /// <summary>
        /// Light level (ZDoom extension).
        /// </summary>
        public int? Light { get; set; }

        /// <summary>
        /// Light is absolute (ZDoom extension).
        /// </summary>
        public bool LightAbsolute { get; set; }

        /// <summary>
        /// X scale for upper texture (ZDoom extension).
        /// </summary>
        public double ScaleXTop { get; set; } = 1.0;

        /// <summary>
        /// Y scale for upper texture (ZDoom extension).
        /// </summary>
        public double ScaleYTop { get; set; } = 1.0;

        /// <summary>
        /// X scale for middle texture (ZDoom extension).
        /// </summary>
        public double ScaleXMid { get; set; } = 1.0;

        /// <summary>
        /// Y scale for middle texture (ZDoom extension).
        /// </summary>
        public double ScaleYMid { get; set; } = 1.0;

        /// <summary>
        /// X scale for lower texture (ZDoom extension).
        /// </summary>
        public double ScaleXBottom { get; set; } = 1.0;

        /// <summary>
        /// Y scale for lower texture (ZDoom extension).
        /// </summary>
        public double ScaleYBottom { get; set; } = 1.0;
    }
}
