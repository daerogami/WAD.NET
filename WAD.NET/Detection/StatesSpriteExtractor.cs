using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using WAD.NET.Definitions;

namespace WAD.NET.Detection
{
    /// <summary>
    /// Extracts sprite prefixes from DECORATE/ZScript States block bodies.
    /// </summary>
    public static class StatesSpriteExtractor
    {
        private static readonly HashSet<string> FlowKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Loop", "Stop", "Goto", "Wait", "Fail"
        };

        private static readonly HashSet<string> ExcludedSprites = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            SpriteConstants.Invisible, SpriteConstants.Null
        };

        // Matches: optional whitespace, 4-char sprite name, frame letters (A-Z or 0-9), then rest of line
        private static readonly Regex SpriteLinePattern = new Regex(
            @"^\s*([A-Za-z0-9_]{4})\s+([A-Za-z0-9]+)\s",
            RegexOptions.Compiled);

        /// <summary>
        /// Extracts unique 4-character sprite prefixes from a States block body.
        /// Excludes TNT1 (invisible sprite) and "----" (null sprite).
        /// </summary>
        /// <param name="statesBody">The text content inside the States { } block.</param>
        /// <returns>A set of unique, uppercased 4-character sprite prefixes.</returns>
        public static HashSet<string> ExtractSpritePrefixes(string statesBody)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrEmpty(statesBody))
                return result;

            var lines = statesBody.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                // Skip empty lines
                if (trimmed.Length == 0)
                    continue;

                // Skip comments
                if (trimmed.StartsWith("//"))
                    continue;

                // Skip labels (ending with ':')
                if (trimmed.EndsWith(":"))
                    continue;

                // Skip flow control keywords
                if (FlowKeywords.Contains(trimmed))
                    continue;

                var match = SpriteLinePattern.Match(line);
                if (match.Success)
                {
                    var sprite = match.Groups[1].Value.ToUpperInvariant();
                    if (!ExcludedSprites.Contains(sprite))
                        result.Add(sprite);
                }
            }

            // Return as uppercase-normalized set
            var normalized = new HashSet<string>(StringComparer.Ordinal);
            foreach (var s in result)
                normalized.Add(s.ToUpperInvariant());

            return normalized;
        }
    }
}
