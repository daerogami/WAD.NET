namespace WAD.NET.SourcePorts.Boom
{
    /// <summary>
    /// Contains detected Boom-specific features in a map.
    /// </summary>
    public class BoomFeatures
    {
        /// <summary>
        /// Whether the map uses Boom generalized linedefs.
        /// </summary>
        public bool HasGeneralizedLinedefs { get; set; }

        /// <summary>
        /// Whether the map uses the PassThru (ML_PASSUSE) flag.
        /// </summary>
        public bool HasPassThruFlag { get; set; }

        /// <summary>
        /// Whether the map uses deep water effects (sector type 242).
        /// </summary>
        public bool HasDeepWater { get; set; }

        /// <summary>
        /// Whether the map uses colormap transfer effects.
        /// </summary>
        public bool HasColormapTransfer { get; set; }

        /// <summary>
        /// Whether the map uses scrolling effects.
        /// </summary>
        public bool HasScrollers { get; set; }

        /// <summary>
        /// Whether the map uses translucency effects.
        /// </summary>
        public bool HasTranslucency { get; set; }

        /// <summary>
        /// Whether the map uses the translucent middle texture flag (MBF).
        /// </summary>
        public bool HasTranslucentMidtex { get; set; }

        /// <summary>
        /// Whether the map uses the MBF friendly monster flag.
        /// </summary>
        public bool HasFriendlyMonsters { get; set; }

        /// <summary>
        /// Returns true if any Boom-specific features are detected.
        /// </summary>
        public bool UsesBoomFeatures =>
            HasGeneralizedLinedefs || HasPassThruFlag ||
            HasDeepWater || HasColormapTransfer ||
            HasScrollers || HasTranslucency;

        /// <summary>
        /// Returns true if any MBF-specific features are detected.
        /// </summary>
        public bool UsesMbfFeatures =>
            HasTranslucentMidtex || HasFriendlyMonsters;
    }
}
