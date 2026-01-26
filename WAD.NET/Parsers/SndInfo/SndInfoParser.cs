using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace WAD.NET.Parsers.SndInfo
{
    /// <summary>
    /// Parser for SNDINFO lumps.
    /// </summary>
    public class SndInfoParser
    {
        /// <summary>
        /// Parses SNDINFO content.
        /// </summary>
        /// <param name="content">The SNDINFO content.</param>
        /// <returns>A parsed SndInfo object.</returns>
        public SndInfo Parse(string content)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            var info = new SndInfo();
            var lines = content.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();

                // Skip empty lines
                if (string.IsNullOrEmpty(line))
                    continue;

                // Remove inline comments
                var commentIndex = line.IndexOf("//");
                if (commentIndex >= 0)
                    line = line.Substring(0, commentIndex).Trim();

                // Skip full-line comments
                if (line.StartsWith("//") || line.StartsWith(";") || string.IsNullOrEmpty(line))
                    continue;

                // Handle multi-line comments (/* */)
                if (line.StartsWith("/*"))
                {
                    while (i < lines.Length && !lines[i].Contains("*/"))
                        i++;
                    continue;
                }

                if (line.StartsWith("$"))
                {
                    ParseDirective(line, info, lines, ref i);
                }
                else
                {
                    // Sound definition: logical_name lump_name
                    var parts = SplitTokens(line);
                    if (parts.Length >= 2)
                    {
                        info.Sounds[parts[0].ToLowerInvariant()] = parts[1].ToUpperInvariant();
                    }
                }
            }

            return info;
        }

        private void ParseDirective(string line, SndInfo info, string[] lines, ref int lineIndex)
        {
            var tokens = SplitTokens(line);
            if (tokens.Length == 0)
                return;

            var directive = tokens[0].ToLowerInvariant();

            switch (directive)
            {
                case "$pitchshiftrange":
                    if (tokens.Length >= 2 && int.TryParse(tokens[1], out int pitchRange))
                        info.PitchShiftRange = pitchRange;
                    break;

                case "$random":
                    ParseRandom(line, info);
                    break;

                case "$alias":
                    if (tokens.Length >= 3)
                        info.Aliases[tokens[1].ToLowerInvariant()] = tokens[2].ToLowerInvariant();
                    break;

                case "$limit":
                    if (tokens.Length >= 3 && int.TryParse(tokens[2], out int limit))
                        info.Limits[tokens[1].ToLowerInvariant()] = limit;
                    break;

                case "$volume":
                    if (tokens.Length >= 3 && float.TryParse(tokens[2], out float volume))
                        info.Volumes[tokens[1].ToLowerInvariant()] = volume;
                    break;

                case "$rolloff":
                    if (tokens.Length >= 4 &&
                        float.TryParse(tokens[2], out float minDist) &&
                        float.TryParse(tokens[3], out float maxDist))
                        info.Rolloffs[tokens[1].ToLowerInvariant()] = (minDist, maxDist);
                    break;

                case "$singular":
                    if (tokens.Length >= 2)
                        info.SingularSounds.Add(tokens[1].ToLowerInvariant());
                    break;

                case "$attenuation":
                    // $attenuation <name> <type>
                    // Skip for now, not commonly used
                    break;

                case "$playersound":
                    // $playersound <player class> <gender> <slot> <sound>
                    if (tokens.Length >= 5)
                    {
                        info.PlayerSounds.Add(new PlayerSoundDefinition
                        {
                            PlayerClass = tokens[1],
                            Gender = tokens[2],
                            SlotName = tokens[3],
                            SoundName = tokens[4]
                        });
                    }
                    break;

                case "$playersounddup":
                    // $playersounddup <player class> <gender> <slot> <source slot>
                    // Links one slot to another
                    break;

                case "$playercompat":
                    // $playercompat <player class> <gender> <compat class> <compat gender>
                    if (tokens.Length >= 5)
                    {
                        info.PlayerCompatibility.Add(new PlayerCompatDefinition
                        {
                            PlayerClass = tokens[1],
                            Gender = tokens[2],
                            CompatibleClass = tokens[3],
                            CompatibleGender = tokens[4]
                        });
                    }
                    break;

                case "$musicalias":
                    if (tokens.Length >= 3)
                        info.MusicAliases[tokens[1].ToLowerInvariant()] = tokens[2].ToLowerInvariant();
                    break;

                case "$ambient":
                    ParseAmbient(tokens, info);
                    break;

                case "$environment":
                    if (tokens.Length >= 3 && int.TryParse(tokens[1], out int envId))
                    {
                        info.Environments.Add(new EnvironmentDefinition
                        {
                            Id = envId,
                            Name = tokens[2]
                        });
                    }
                    break;

                case "$ifdoom":
                case "$ifheretic":
                case "$ifhexen":
                case "$ifstrife":
                case "$ifchex":
                case "$endif":
                    // Conditional compilation - skip (we'd need to know the game type)
                    break;

                case "$map":
                case "$registered":
                case "$archivepath":
                    // Various configuration directives - skip
                    break;

                // Ignore unknown directives
                default:
                    break;
            }
        }

        private void ParseRandom(string line, SndInfo info)
        {
            // Format: $random <name> { sound1 sound2 ... }
            var match = Regex.Match(line, @"\$random\s+(\S+)\s*\{([^}]+)\}", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var name = match.Groups[1].Value.ToLowerInvariant();
                var soundsText = match.Groups[2].Value;
                var sounds = SplitTokens(soundsText);

                for (int i = 0; i < sounds.Length; i++)
                    sounds[i] = sounds[i].ToLowerInvariant();

                info.RandomSounds[name] = sounds;
            }
        }

        private void ParseAmbient(string[] tokens, SndInfo info)
        {
            // Format: $ambient <index> <sound> [type] [mode] [volume]
            if (tokens.Length < 3)
                return;

            if (!int.TryParse(tokens[1], out int index))
                return;

            var ambient = new AmbientSoundDefinition
            {
                Index = index,
                SoundName = tokens[2].ToLowerInvariant(),
                Type = AmbientType.Point,
                PlayMode = AmbientPlayMode.Continuous,
                Volume = 1.0f
            };

            for (int i = 3; i < tokens.Length; i++)
            {
                var token = tokens[i].ToLowerInvariant();

                switch (token)
                {
                    case "point":
                        ambient.Type = AmbientType.Point;
                        break;
                    case "surround":
                        ambient.Type = AmbientType.Surround;
                        break;
                    case "world":
                        ambient.Type = AmbientType.World;
                        break;
                    case "continuous":
                        ambient.PlayMode = AmbientPlayMode.Continuous;
                        break;
                    case "random":
                        ambient.PlayMode = AmbientPlayMode.Random;
                        // Next two tokens should be min and max time
                        if (i + 2 < tokens.Length)
                        {
                            if (float.TryParse(tokens[i + 1], out float minTime))
                                ambient.MinTime = minTime;
                            if (float.TryParse(tokens[i + 2], out float maxTime))
                                ambient.MaxTime = maxTime;
                            i += 2;
                        }
                        break;
                    case "periodic":
                        ambient.PlayMode = AmbientPlayMode.Periodic;
                        if (i + 1 < tokens.Length && float.TryParse(tokens[i + 1], out float period))
                        {
                            ambient.MinTime = period;
                            ambient.MaxTime = period;
                            i++;
                        }
                        break;
                    default:
                        // Try to parse as volume
                        if (float.TryParse(token, out float vol))
                            ambient.Volume = vol;
                        break;
                }
            }

            info.AmbientSounds[index] = ambient;
        }

        private string[] SplitTokens(string line)
        {
            var tokens = new List<string>();
            var matches = Regex.Matches(line, @"""([^""]*)""|(\S+)");

            foreach (Match match in matches)
            {
                if (match.Groups[1].Success)
                    tokens.Add(match.Groups[1].Value);
                else if (match.Groups[2].Success)
                    tokens.Add(match.Groups[2].Value);
            }

            return tokens.ToArray();
        }
    }
}
