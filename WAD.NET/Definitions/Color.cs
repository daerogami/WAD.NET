using System;

namespace WAD.NET.Definitions
{
    /// <summary>
    /// Represents an RGB color from a DOOM palette.
    /// </summary>
    public readonly struct Color : IEquatable<Color>
    {
        /// <summary>
        /// Red component (0-255).
        /// </summary>
        public byte R { get; }

        /// <summary>
        /// Green component (0-255).
        /// </summary>
        public byte G { get; }

        /// <summary>
        /// Blue component (0-255).
        /// </summary>
        public byte B { get; }

        public Color(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
        }

        public static Color FromRgb(byte r, byte g, byte b) => new Color(r, g, b);

        public bool Equals(Color other) => R == other.R && G == other.G && B == other.B;

        public override bool Equals(object obj) => obj is Color other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(R, G, B);

        public override string ToString() => $"#{R:X2}{G:X2}{B:X2}";

        public static bool operator ==(Color left, Color right) => left.Equals(right);

        public static bool operator !=(Color left, Color right) => !left.Equals(right);
    }
}
