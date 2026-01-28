using System;
using WAD.NET.Abstract;
using WAD.NET.Definitions;

namespace WAD.NET.Concrete
{
    /// <summary>
    /// Represents the PLAYPAL lump containing 14 256-color palettes.
    /// </summary>
    /// <remarks>
    /// Palette indices:
    /// 0: Normal gameplay
    /// 1-8: Pain (increasing red tint for damage)
    /// 9: Bonus pickup (gold/yellow tint)
    /// 10-12: Radiation suit (green tint)
    /// 13: Berserk (red tint)
    /// </remarks>
    public sealed class PaletteLump : Lump
    {
        /// <summary>
        /// Number of palettes in PLAYPAL.
        /// </summary>
        public const int PaletteCount = 14;

        /// <summary>
        /// Expected size of PLAYPAL lump in bytes.
        /// </summary>
        public const int ExpectedSize = Palette.ByteSize * PaletteCount;

        /// <summary>
        /// The 14 palettes contained in this lump.
        /// </summary>
        public Palette[] Palettes { get; }

        /// <summary>
        /// Gets the normal gameplay palette (index 0).
        /// </summary>
        public Palette NormalPalette => Palettes[0];

        public PaletteLump(string lumpName, string sourceWadFileName, ReadOnlySpan<byte> data)
            : base(lumpName, sourceWadFileName)
        {
            if (data.Length < ExpectedSize)
            {
                throw new FormatException(
                    $"PLAYPAL lump must be at least {ExpectedSize} bytes, got {data.Length}");
            }

            Palettes = new Palette[PaletteCount];
            for (int i = 0; i < PaletteCount; i++)
            {
                Palettes[i] = new Palette(data.Slice(i * Palette.ByteSize, Palette.ByteSize));
            }
        }

        /// <summary>
        /// Gets the palette at the specified index.
        /// </summary>
        public Palette this[int index] => Palettes[index];
    }
}
