using System;
using System.Buffers.Binary;
using System.Text;
using WAD.NET.Abstract;
using WAD.NET.Interfaces;

namespace WAD.NET.Concrete.Maps
{
    /// <summary>
    /// Lump containing ACS (Action Code Script) bytecode for Hexen format maps.
    /// </summary>
    public sealed class BehaviorLump : Lump, IMapLump
    {
        /// <summary>
        /// ACS bytecode marker.
        /// </summary>
        public const string AcsMarker = "ACS";

        /// <summary>
        /// Raw bytecode data.
        /// </summary>
        public byte[] Data { get; }

        /// <summary>
        /// True if this is valid ACS bytecode.
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// The ACS format marker (ACS\0 or ACSE/ACSE).
        /// </summary>
        public string Marker { get; }

        /// <summary>
        /// Offset to the script directory.
        /// </summary>
        public int DirectoryOffset { get; }

        /// <summary>
        /// Number of scripts in this lump (if parsed).
        /// </summary>
        public int ScriptCount { get; }

        /// <summary>
        /// Creates a new BehaviorLump.
        /// </summary>
        /// <param name="name">Lump name.</param>
        /// <param name="source">Source WAD name.</param>
        /// <param name="data">Binary lump data.</param>
        public BehaviorLump(string name, string source, ReadOnlySpan<byte> data)
            : base(name, source)
        {
            Data = data.ToArray();

            if (data.Length < 8)
            {
                IsValid = false;
                Marker = string.Empty;
                DirectoryOffset = 0;
                ScriptCount = 0;
                return;
            }

            // Read the marker (first 4 bytes)
#if NETSTANDARD2_1_OR_GREATER
            Marker = Encoding.ASCII.GetString(data.Slice(0, 4));
#else
            Marker = Encoding.ASCII.GetString(data.Slice(0, 4).ToArray());
#endif

            // Check for valid ACS markers
            IsValid = Marker.StartsWith(AcsMarker);

            if (!IsValid)
            {
                DirectoryOffset = 0;
                ScriptCount = 0;
                return;
            }

            // Read directory offset
            DirectoryOffset = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(4));

            // Try to read script count from directory
            if (DirectoryOffset >= 0 && DirectoryOffset + 4 <= data.Length)
            {
                ScriptCount = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(DirectoryOffset));
            }
        }

        /// <summary>
        /// Gets the format version based on the marker.
        /// </summary>
        public AcsFormat Format
        {
            get
            {
                if (!IsValid)
                    return AcsFormat.Unknown;

                return Marker switch
                {
                    "ACS\0" => AcsFormat.Old,
                    "ACSE" => AcsFormat.Enhanced,
                    "ACSe" => AcsFormat.EnhancedLittleEndian,
                    _ => AcsFormat.Unknown
                };
            }
        }
    }

    /// <summary>
    /// ACS bytecode format versions.
    /// </summary>
    public enum AcsFormat
    {
        /// <summary>
        /// Unknown or invalid format.
        /// </summary>
        Unknown,

        /// <summary>
        /// Original Hexen ACS format.
        /// </summary>
        Old,

        /// <summary>
        /// Enhanced ACS format (ZDoom).
        /// </summary>
        Enhanced,

        /// <summary>
        /// Enhanced ACS format with little-endian strings (ZDoom).
        /// </summary>
        EnhancedLittleEndian
    }
}
