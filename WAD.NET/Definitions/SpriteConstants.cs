using System;

namespace WAD.NET.Definitions
{
    /// <summary>
    /// Well-known sprite names and their meanings.
    /// </summary>
    public static class SpriteConstants
    {
        /// <summary>Invisible sprite - thing has no visual representation.</summary>
        public const string Invisible = "TNT1";

        /// <summary>Null/missing sprite marker.</summary>
        public const string Null = "----";

        /// <summary>Standard sprite name length.</summary>
        public const int SpriteNameLength = 4;

        /// <summary>All-angle rotation indicator (frame letter followed by 0).</summary>
        public const char AllAngles = '0';

        /// <summary>
        /// Checks if a sprite name represents an invisible/null sprite.
        /// </summary>
        /// <param name="spriteName">The sprite name to check.</param>
        /// <returns>True if the sprite is invisible or null.</returns>
        public static bool IsInvisible(string spriteName)
        {
            return string.Equals(spriteName, Invisible, StringComparison.OrdinalIgnoreCase)
                || string.Equals(spriteName, Null, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Validates a sprite name format.
        /// </summary>
        /// <param name="name">The sprite name to validate.</param>
        /// <returns>True if the name is a valid 4-character sprite name.</returns>
        public static bool IsValidSpriteName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            if (name.Length != SpriteNameLength)
                return false;

            foreach (char c in name)
            {
                if (!char.IsLetterOrDigit(c) && c != '_' && c != '-')
                    return false;
            }

            return true;
        }
    }
}
