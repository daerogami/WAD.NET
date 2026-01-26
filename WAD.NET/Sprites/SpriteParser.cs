using System;
using System.Collections.Generic;
using WAD.NET.Concrete;
using WAD.NET.Definitions;

namespace WAD.NET.Sprites
{
    /// <summary>
    /// Utility class for parsing sprite lump names and organizing sprite frames.
    /// </summary>
    public static class SpriteParser
    {
        /// <summary>
        /// Minimum length for a valid sprite lump name.
        /// </summary>
        public const int MinSpriteLumpNameLength = 6;

        /// <summary>
        /// Maximum length for a sprite lump name (with mirrored rotation).
        /// </summary>
        public const int MaxSpriteLumpNameLength = 8;

        /// <summary>
        /// Parses a sprite lump name and data into a SpriteFrame.
        /// </summary>
        /// <param name="lumpName">The lump name (e.g., "POSSA1", "POSSA2A8").</param>
        /// <param name="data">The raw picture data.</param>
        /// <returns>The parsed sprite frame.</returns>
        public static SpriteFrame ParseSpriteLump(string lumpName, ReadOnlySpan<byte> data)
        {
            if (lumpName == null)
                throw new ArgumentNullException(nameof(lumpName));

            if (lumpName.Length < MinSpriteLumpNameLength)
                throw new FormatException(
                    $"Invalid sprite lump name '{lumpName}': must be at least {MinSpriteLumpNameLength} characters");

            var spriteName = lumpName.Substring(0, 4);
            var frame = lumpName[4];
            var rotation = lumpName[5] - '0';

            if (rotation < 0 || rotation > 8)
                throw new FormatException(
                    $"Invalid sprite rotation '{lumpName[5]}' in lump '{lumpName}': must be 0-8");

            // Parse the picture data
            var picture = PictureLump.ParsePicture(lumpName, data);

            return new SpriteFrame(
                spriteName,
                frame,
                rotation,
                isMirrored: false,
                picture);
        }

        /// <summary>
        /// Parses a sprite lump that may include mirrored frame information.
        /// </summary>
        /// <param name="lumpName">The lump name (e.g., "POSSA2A8").</param>
        /// <param name="data">The raw picture data.</param>
        /// <returns>One or two sprite frames (second is mirrored if applicable).</returns>
        public static List<SpriteFrame> ParseSpriteLumpWithMirror(string lumpName, ReadOnlySpan<byte> data)
        {
            if (lumpName == null)
                throw new ArgumentNullException(nameof(lumpName));

            if (lumpName.Length < MinSpriteLumpNameLength)
                throw new FormatException(
                    $"Invalid sprite lump name '{lumpName}': must be at least {MinSpriteLumpNameLength} characters");

            var result = new List<SpriteFrame>(2);

            var spriteName = lumpName.Substring(0, 4);
            var frame1 = lumpName[4];
            var rotation1 = lumpName[5] - '0';

            if (rotation1 < 0 || rotation1 > 8)
                throw new FormatException(
                    $"Invalid sprite rotation '{lumpName[5]}' in lump '{lumpName}': must be 0-8");

            // Parse the picture data
            var picture = PictureLump.ParsePicture(lumpName, data);

            // Add primary frame
            result.Add(new SpriteFrame(
                spriteName,
                frame1,
                rotation1,
                isMirrored: false,
                picture));

            // Check for mirrored frame (8-character names like "POSSA2A8")
            if (lumpName.Length >= 8)
            {
                var frame2 = lumpName[6];
                var rotation2 = lumpName[7] - '0';

                if (rotation2 >= 0 && rotation2 <= 8)
                {
                    result.Add(new SpriteFrame(
                        spriteName,
                        frame2,
                        rotation2,
                        isMirrored: true,
                        picture));
                }
            }

            return result;
        }

        /// <summary>
        /// Determines if a lump name follows the sprite naming convention.
        /// </summary>
        public static bool IsSpriteLumpName(string lumpName)
        {
            if (lumpName == null || lumpName.Length < MinSpriteLumpNameLength)
                return false;

            // Check frame letter (5th character should be A-Z)
            char frame = lumpName[4];
            if (frame < 'A' || frame > ']')  // ] is used in some source ports
                return false;

            // Check rotation (6th character should be 0-8)
            char rotation = lumpName[5];
            if (rotation < '0' || rotation > '8')
                return false;

            // If 8 characters, check second frame/rotation
            if (lumpName.Length >= 8)
            {
                char frame2 = lumpName[6];
                char rotation2 = lumpName[7];

                if ((frame2 < 'A' || frame2 > ']') ||
                    (rotation2 < '0' || rotation2 > '8'))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Builds a collection of sprite definitions from a list of sprite lumps.
        /// </summary>
        public static Dictionary<string, SpriteDefinition> BuildSpriteDefinitions(
            IEnumerable<(string Name, ReadOnlyMemory<byte> Data)> spriteLumps)
        {
            var definitions = new Dictionary<string, SpriteDefinition>(StringComparer.OrdinalIgnoreCase);

            foreach (var (name, data) in spriteLumps)
            {
                if (!IsSpriteLumpName(name))
                    continue;

                var spriteName = name.Substring(0, 4);

                if (!definitions.TryGetValue(spriteName, out var definition))
                {
                    definition = new SpriteDefinition(spriteName);
                    definitions[spriteName] = definition;
                }

                // Parse frames (handles mirrored frames automatically)
                foreach (var frame in ParseSpriteLumpWithMirror(name, data.Span))
                {
                    definition.AddFrame(frame);
                }
            }

            return definitions;
        }

        /// <summary>
        /// Extracts the sprite name (first 4 characters) from a lump name.
        /// </summary>
        public static string GetSpriteName(string lumpName)
        {
            if (lumpName == null || lumpName.Length < 4)
                return null;
            return lumpName.Substring(0, 4);
        }

        /// <summary>
        /// Extracts the frame letter from a sprite lump name.
        /// </summary>
        public static char? GetFrame(string lumpName)
        {
            if (lumpName == null || lumpName.Length < 5)
                return null;
            return lumpName[4];
        }

        /// <summary>
        /// Extracts the rotation number from a sprite lump name.
        /// </summary>
        public static int? GetRotation(string lumpName)
        {
            if (lumpName == null || lumpName.Length < 6)
                return null;
            int rotation = lumpName[5] - '0';
            if (rotation < 0 || rotation > 8)
                return null;
            return rotation;
        }
    }
}
