using WAD.NET.UDMF.Fields;

namespace WAD.NET.UDMF.Fields
{
    /// <summary>
    /// UDMF Side Definition.
    /// </summary>
    /// <remarks>
    /// As defined by UDMF 1.1 and zDoom extensions v1.15
    /// <see cref="https://github.com/coelckers/gzdoom/blob/master/specs/udmf.txt"/>
    /// <seealso cref="https://github.com/doomtech/slade/blob/trunk/udmf_zdoom.txt"/>
    /// </remarks>
    public struct SideDefinition
    {
        /// <summary>
        /// X Offset. Default = 0.
        /// </summary>
        public int OffsetX { get; set; }

        /// <summary>
        /// Y Offset. Default = 0.
        /// </summary>
        public int OffsetY { get; set; }

        /// <summary>
        /// Upper texture. Default = "-".
        /// </summary>
        public string TextureTop { get; set; }

        /// <summary>
        /// Lower texture. Default = "-".
        /// </summary>
        public string TextureBottom { get; set; }

        /// <summary>
        /// Middle texture. Default = "-".
        /// </summary>
        public string TextureMiddle { get; set; }

        /// <summary>
        /// Sector index. No valid default.
        /// </summary>
        public int Sector { get; set; }

        /// <summary>
        /// A comment.
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// X scale for upper texture. Default = 1.0.
        /// </summary>
        [FieldSpecification(Specification.UDMF_ZDOOM, 1, 15)]
        public float ScaleXTop { get; set; }

        /// <summary>
        /// Y scale for upper texture. Default = 1.0.
        /// </summary>
        [FieldSpecification(Specification.UDMF_ZDOOM, 1, 15)]
        public float ScaleYTop { get; set; }

        /// <summary>
        /// X scale for mid texture. Default = 1.0.
        /// </summary>
        [FieldSpecification(Specification.UDMF_ZDOOM, 1, 15)]
        public float ScaleXMid { get; set; }

        /// <summary>
        /// Y scale for mid texture. Default = 1.0.
        /// </summary>
        [FieldSpecification(Specification.UDMF_ZDOOM, 1, 15)]
        public float ScaleYMid { get; set; }

        /// <summary>
        /// X scale for lower texture. Default = 1.0.
        /// </summary>
        [FieldSpecification(Specification.UDMF_ZDOOM, 1, 15)]
        public float ScaleXBottom { get; set; }

        /// <summary>
        /// Y scale for lower texture. Default = 1.0.
        /// </summary>
        [FieldSpecification(Specification.UDMF_ZDOOM, 1, 15)]
        public float ScaleYBottom { get; set; }

        /// <summary>
        /// X offset for upper texture. Default = 0.0.
        /// </summary>
        [FieldSpecification(Specification.UDMF_ZDOOM, 1, 15)]
        public float OffsetXTop { get; set; }

        /// <summary>
        /// Y offset for upper texture. Default = 0.0.
        /// </summary>
        [FieldSpecification(Specification.UDMF_ZDOOM, 1, 15)]
        public float OffsetYTop { get; set; }

        /// <summary>
        /// X offset for mid texture. Default = 0.0.
        /// </summary>
        [FieldSpecification(Specification.UDMF_ZDOOM, 1, 15)]
        public float OffsetXMid { get; set; }

        /// <summary>
        /// Y offset for mid texture. Default = 0.0.
        /// </summary>
        [FieldSpecification(Specification.UDMF_ZDOOM, 1, 15)]
        public float OffsetYMid { get; set; }

        /// <summary>
        /// X offset for lower texture. Default = 0.0.
        /// </summary>
        [FieldSpecification(Specification.UDMF_ZDOOM, 1, 15)]
        public float OffsetXBottom { get; set; }

        /// <summary>
        /// Y offset for lower texture. Default = 0.0.
        /// </summary>
        [FieldSpecification(Specification.UDMF_ZDOOM, 1, 15)]
        public float OffsetYBottom { get; set; }

        /// <summary>
        /// This side's light level. Default is 0.
        /// </summary>
        [FieldSpecification(Specification.UDMF_ZDOOM, 1, 15)]
        public int Light { get; set; }

        /// <summary>
        /// true = 'light' is an absolute value.
        /// </summary>
        [FieldSpecification(Specification.UDMF_ZDOOM, 1, 15)]
        public bool LightAbsolute { get; set; }

        /// <summary>
        /// true = relative lighting is used even in foggy sectors.
        /// </summary>
        [FieldSpecification(Specification.UDMF_ZDOOM, 1, 15)]
        public bool LightFog { get; set; }

        /// <summary>
        /// Disables use of fake contrast on this sidedef.
        /// </summary>
        [FieldSpecification(Specification.UDMF_ZDOOM, 1, 15)]
        public bool NoFakeContrast { get; set; }

        /// <summary>
        /// Use smooth fake contrast.
        /// </summary>
        [FieldSpecification(Specification.UDMF_ZDOOM, 1, 15)]
        public bool SmoothLighting { get; set; }

        /// <summary>
        /// Side's mid textures are clipped to floor and ceiling.
        /// </summary>
        [FieldSpecification(Specification.UDMF_ZDOOM, 1, 15)]
        public bool ClipMidTex { get; set; }

        /// <summary>
        /// Side's mid textures are wrapped.
        /// </summary>
        [FieldSpecification(Specification.UDMF_ZDOOM, 1, 15)]
        public bool WrapMidTex { get; set; }

        /// <summary>
        /// Disables decals on the sidedef.
        /// </summary>
        [FieldSpecification(Specification.UDMF_ZDOOM, 1, 15)]
        public bool NoDecals { get; set; }

        public SideDefinition()
        {
            OffsetX = 0;
            OffsetY = 0;
            TextureTop = "-";
            TextureBottom = "-";
            TextureMiddle = "-";
            Sector = 0;
            Comment = null;
            ScaleXTop = 1.0f;
            ScaleYTop = 1.0f;
            ScaleXMid = 1.0f;
            ScaleYMid = 1.0f;
            ScaleXBottom = 1.0f;
            ScaleYBottom = 1.0f;
            OffsetXTop = 0;
            OffsetYTop = 0;
            OffsetXMid = 0;
            OffsetYMid = 0;
            OffsetXBottom = 0;
            OffsetYBottom = 0;
            Light = 0;
            LightAbsolute = false;
            LightFog = false;
            NoFakeContrast = false;
            SmoothLighting = false;
            ClipMidTex = false;
            WrapMidTex = false;
            NoDecals = false;
        }
    }
}
