using WAD.NET.Definitions.Classic.Map;
using WAD.NET.Maps;

namespace WAD.NET.SourcePorts.Boom
{
    /// <summary>
    /// Detects Boom-specific features in maps.
    /// </summary>
    public static class BoomDetector
    {
        // Boom generalized linedef specials
        private const ushort ScrollerLinedefStart = 245;
        private const ushort ScrollerLinedefEnd = 255;

        // Deep water and colormap transfer sector types
        private const ushort DeepWaterSectorType = 242;
        private const ushort ColormapTransferStart = 213;
        private const ushort ColormapTransferEnd = 224;

        /// <summary>
        /// Detects Boom-specific features in a map.
        /// </summary>
        /// <param name="map">The map to analyze.</param>
        /// <returns>A BoomFeatures object describing detected features.</returns>
        public static BoomFeatures DetectFeatures(IMap map)
        {
            var features = new BoomFeatures();

            if (map is DoomMap doomMap)
            {
                DetectLinedefFeatures(doomMap, features);
                DetectSectorFeatures(doomMap, features);
                DetectThingFeatures(doomMap, features);
            }

            return features;
        }

        private static void DetectLinedefFeatures(DoomMap map, BoomFeatures features)
        {
            if (map.Linedefs == null) return;

            foreach (var linedef in map.Linedefs)
            {
                // Check for generalized linedefs
                if (BoomGeneralizedLinedef.IsGeneralized(linedef.Special))
                {
                    features.HasGeneralizedLinedefs = true;
                }

                // Check for extended flags
                ushort flags = (ushort)linedef.Flags;

                if ((flags & (ushort)BoomLinedefFlags.PassThru) != 0)
                {
                    features.HasPassThruFlag = true;
                }

                if ((flags & (ushort)BoomLinedefFlags.TranslucentMidtex) != 0)
                {
                    features.HasTranslucentMidtex = true;
                }

                // Check for scroller linedef types
                if (linedef.Special >= ScrollerLinedefStart && linedef.Special <= ScrollerLinedefEnd)
                {
                    features.HasScrollers = true;
                }

                // Check for translucency linedef type (260)
                if (linedef.Special == 260)
                {
                    features.HasTranslucency = true;
                }
            }
        }

        private static void DetectSectorFeatures(DoomMap map, BoomFeatures features)
        {
            if (map.Sectors == null) return;

            foreach (var sector in map.Sectors)
            {
                // Check for deep water
                if (sector.Special == DeepWaterSectorType)
                {
                    features.HasDeepWater = true;
                }

                // Check for colormap transfer
                if (sector.Special >= ColormapTransferStart && sector.Special <= ColormapTransferEnd)
                {
                    features.HasColormapTransfer = true;
                }
            }
        }

        private static void DetectThingFeatures(DoomMap map, BoomFeatures features)
        {
            if (map.Things == null) return;

            foreach (var thing in map.Things)
            {
                // Check for MBF friendly flag
                if ((thing.Flags & ThingFlags.Friendly) != 0)
                {
                    features.HasFriendlyMonsters = true;
                }
            }
        }
    }
}
