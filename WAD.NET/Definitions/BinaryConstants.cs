namespace WAD.NET.Definitions
{
    /// <summary>
    /// Binary format magic values and constants for WAD and related formats.
    /// </summary>
    public static class BinaryConstants
    {
        /// <summary>IWAD file header magic string.</summary>
        public const string IwadMarker = "IWAD";

        /// <summary>PWAD file header magic string.</summary>
        public const string PwadMarker = "PWAD";

        /// <summary>Size of a blockmap block in map units.</summary>
        public const int BlockmapBlockSize = 128;

        /// <summary>Terminator value in blockmap block lists.</summary>
        public const ushort BlockmapTerminator = 0xFFFF;

        /// <summary>Empty block marker in blockmap.</summary>
        public const ushort BlockmapEmptyBlock = 0x0000;

        /// <summary>Flag bit used to identify subsector references in BSP nodes.</summary>
        public const ushort SubsectorFlag = 0x8000;

        /// <summary>ACS bytecode magic marker.</summary>
        public const string AcsMarker = "ACS";

        /// <summary>Enhanced ACS bytecode magic marker.</summary>
        public const string AcsEnhancedMarker = "ACSE";

        /// <summary>Little-endian ACS bytecode magic marker.</summary>
        public const string AcsLittleEndianMarker = "ACSe";

        /// <summary>Size of the ENDOOM lump (80x25 characters, 2 bytes each).</summary>
        public const int EndoomSize = 4000;
    }
}
