namespace WAD.NET.UDMF
{
    /// <summary>
    /// Represents a sector in a UDMF map.
    /// </summary>
    public class UdmfSector
    {
        /// <summary>
        /// Sector ID for scripting.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Floor height.
        /// </summary>
        public int HeightFloor { get; set; }

        /// <summary>
        /// Ceiling height.
        /// </summary>
        public int HeightCeiling { get; set; }

        /// <summary>
        /// Floor texture name.
        /// </summary>
        public string TextureFloor { get; set; }

        /// <summary>
        /// Ceiling texture name.
        /// </summary>
        public string TextureCeiling { get; set; }

        /// <summary>
        /// Light level (0-255).
        /// </summary>
        public int LightLevel { get; set; } = 160;

        /// <summary>
        /// Special type.
        /// </summary>
        public int Special { get; set; }

        /// <summary>
        /// Comment (not used by engine).
        /// </summary>
        public string Comment { get; set; }

        // ZDoom extensions
        /// <summary>
        /// Floor X offset (ZDoom extension).
        /// </summary>
        public double XPanningFloor { get; set; }

        /// <summary>
        /// Floor Y offset (ZDoom extension).
        /// </summary>
        public double YPanningFloor { get; set; }

        /// <summary>
        /// Ceiling X offset (ZDoom extension).
        /// </summary>
        public double XPanningCeiling { get; set; }

        /// <summary>
        /// Ceiling Y offset (ZDoom extension).
        /// </summary>
        public double YPanningCeiling { get; set; }

        /// <summary>
        /// Floor X scale (ZDoom extension).
        /// </summary>
        public double XScaleFloor { get; set; } = 1.0;

        /// <summary>
        /// Floor Y scale (ZDoom extension).
        /// </summary>
        public double YScaleFloor { get; set; } = 1.0;

        /// <summary>
        /// Ceiling X scale (ZDoom extension).
        /// </summary>
        public double XScaleCeiling { get; set; } = 1.0;

        /// <summary>
        /// Ceiling Y scale (ZDoom extension).
        /// </summary>
        public double YScaleCeiling { get; set; } = 1.0;

        /// <summary>
        /// Floor rotation angle (ZDoom extension).
        /// </summary>
        public double RotationFloor { get; set; }

        /// <summary>
        /// Ceiling rotation angle (ZDoom extension).
        /// </summary>
        public double RotationCeiling { get; set; }

        /// <summary>
        /// Floor light level (ZDoom extension).
        /// </summary>
        public int? LightFloor { get; set; }

        /// <summary>
        /// Ceiling light level (ZDoom extension).
        /// </summary>
        public int? LightCeiling { get; set; }

        /// <summary>
        /// Floor light is absolute (ZDoom extension).
        /// </summary>
        public bool LightFloorAbsolute { get; set; }

        /// <summary>
        /// Ceiling light is absolute (ZDoom extension).
        /// </summary>
        public bool LightCeilingAbsolute { get; set; }

        /// <summary>
        /// Gravity factor (ZDoom extension).
        /// </summary>
        public double Gravity { get; set; } = 1.0;
    }
}
