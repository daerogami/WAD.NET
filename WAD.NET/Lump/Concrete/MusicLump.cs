using System;
using System.Buffers.Binary;
using WAD.NET.Abstract;

namespace WAD.NET.Concrete
{
    /// <summary>
    /// Represents a music lump (D_* prefix) in MUS or MIDI format.
    /// </summary>
    /// <remarks>
    /// MUS Format Header:
    /// - 4 bytes: Magic "MUS\x1A"
    /// - 2 bytes: Score length
    /// - 2 bytes: Score start offset
    /// - 2 bytes: Primary channels
    /// - 2 bytes: Secondary channels
    /// - 2 bytes: Instrument count
    /// - 2 bytes: Reserved
    /// - N*2 bytes: Instrument patches
    /// </remarks>
    public sealed class MusicLump : Lump
    {
        /// <summary>
        /// MUS format magic bytes.
        /// </summary>
        private static readonly byte[] MusMagic = { (byte)'M', (byte)'U', (byte)'S', 0x1A };

        /// <summary>
        /// MIDI format magic bytes.
        /// </summary>
        private static readonly byte[] MidiMagic = { (byte)'M', (byte)'T', (byte)'h', (byte)'d' };

        /// <summary>
        /// True if this lump contains MUS format music.
        /// </summary>
        public bool IsMus { get; }

        /// <summary>
        /// True if this lump contains MIDI format music.
        /// </summary>
        public bool IsMidi { get; }

        /// <summary>
        /// The raw music data.
        /// </summary>
        public byte[] RawData { get; }

        /// <summary>
        /// Instruments used in MUS format (empty array for MIDI or unknown).
        /// </summary>
        public ushort[] Instruments { get; }

        /// <summary>
        /// Score length for MUS format.
        /// </summary>
        public ushort ScoreLength { get; }

        /// <summary>
        /// Score start offset for MUS format.
        /// </summary>
        public ushort ScoreStartOffset { get; }

        /// <summary>
        /// Number of primary channels for MUS format.
        /// </summary>
        public ushort PrimaryChannels { get; }

        /// <summary>
        /// Number of secondary channels for MUS format.
        /// </summary>
        public ushort SecondaryChannels { get; }

        /// <summary>
        /// For backward compatibility - alias for RawData.
        /// </summary>
        public byte[] MusicData => RawData;

        public MusicLump(string lumpName, string sourceWadFileName, byte[] musicData)
            : base(lumpName, sourceWadFileName)
        {
            RawData = musicData ?? Array.Empty<byte>();
            Instruments = Array.Empty<ushort>();

            if (RawData.Length >= 4)
            {
                // Check for MUS magic
                if (RawData[0] == MusMagic[0] &&
                    RawData[1] == MusMagic[1] &&
                    RawData[2] == MusMagic[2] &&
                    RawData[3] == MusMagic[3])
                {
                    IsMus = true;

                    // Parse MUS header
                    if (RawData.Length >= 16)
                    {
                        var span = RawData.AsSpan();

                        ScoreLength = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(4));
                        ScoreStartOffset = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(6));
                        PrimaryChannels = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(8));
                        SecondaryChannels = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(10));
                        var instrumentCount = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(12));

                        // Parse instruments list
                        if (instrumentCount > 0 && RawData.Length >= 16 + instrumentCount * 2)
                        {
                            Instruments = new ushort[instrumentCount];
                            for (int i = 0; i < instrumentCount; i++)
                            {
                                Instruments[i] = BinaryPrimitives.ReadUInt16LittleEndian(
                                    span.Slice(16 + i * 2, 2));
                            }
                        }
                    }
                }
                // Check for MIDI magic
                else if (RawData[0] == MidiMagic[0] &&
                         RawData[1] == MidiMagic[1] &&
                         RawData[2] == MidiMagic[2] &&
                         RawData[3] == MidiMagic[3])
                {
                    IsMidi = true;
                }
            }
        }

        public MusicLump(string lumpName, string sourceWadFileName, ReadOnlySpan<byte> musicData)
            : this(lumpName, sourceWadFileName, musicData.ToArray())
        {
        }

        /// <summary>
        /// Converts MUS format to MIDI. Returns original data if already MIDI.
        /// </summary>
        public byte[] ToMidi()
        {
            if (IsMidi)
                return RawData;

            if (!IsMus)
                return RawData;

            return MusToMidiConverter.Convert(RawData);
        }
    }
}
