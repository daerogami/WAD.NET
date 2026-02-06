using System;
using System.Collections.Generic;

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
            var tokens = DefinitionTokenizer.Tokenize(content, semicolonComments: true);

            while (tokens.Count > 0)
            {
                var token = tokens.Dequeue();

                if (token.StartsWith("$"))
                {
                    ParseDirective(token, tokens, info);
                }
                else if (tokens.Count > 0)
                {
                    // Sound definition: logical_name lump_name
                    info.Sounds[token.ToLowerInvariant()] = tokens.Dequeue().ToUpperInvariant();
                }
            }

            return info;
        }

        private void ParseDirective(string directive, Queue<string> tokens, SndInfo info)
        {
            var dir = directive.ToLowerInvariant();

            switch (dir)
            {
                case "$pitchshiftrange":
                    if (tokens.Count > 0 && int.TryParse(tokens.Peek(), out int pitchRange))
                    {
                        tokens.Dequeue();
                        info.PitchShiftRange = pitchRange;
                    }
                    break;

                case "$random":
                    ParseRandom(tokens, info);
                    break;

                case "$alias":
                    if (tokens.Count >= 2)
                    {
                        var name = tokens.Dequeue().ToLowerInvariant();
                        var target = tokens.Dequeue().ToLowerInvariant();
                        info.Aliases[name] = target;
                    }
                    break;

                case "$limit":
                    if (tokens.Count >= 2)
                    {
                        var name = tokens.Dequeue().ToLowerInvariant();
                        if (int.TryParse(tokens.Peek(), out int limit))
                        {
                            tokens.Dequeue();
                            info.Limits[name] = limit;
                        }
                    }
                    break;

                case "$volume":
                    if (tokens.Count >= 2)
                    {
                        var name = tokens.Dequeue().ToLowerInvariant();
                        if (float.TryParse(tokens.Peek(), out float volume))
                        {
                            tokens.Dequeue();
                            info.Volumes[name] = volume;
                        }
                    }
                    break;

                case "$rolloff":
                    if (tokens.Count >= 3)
                    {
                        var name = tokens.Dequeue().ToLowerInvariant();
                        if (float.TryParse(tokens.Peek(), out float minDist))
                        {
                            tokens.Dequeue();
                            if (float.TryParse(tokens.Peek(), out float maxDist))
                            {
                                tokens.Dequeue();
                                info.Rolloffs[name] = (minDist, maxDist);
                            }
                        }
                    }
                    break;

                case "$singular":
                    if (tokens.Count > 0)
                        info.SingularSounds.Add(tokens.Dequeue().ToLowerInvariant());
                    break;

                case "$attenuation":
                    // $attenuation <name> <type> - skip
                    if (tokens.Count >= 2) { tokens.Dequeue(); tokens.Dequeue(); }
                    break;

                case "$playersound":
                    if (tokens.Count >= 4)
                    {
                        info.PlayerSounds.Add(new PlayerSoundDefinition
                        {
                            PlayerClass = tokens.Dequeue(),
                            Gender = tokens.Dequeue(),
                            SlotName = tokens.Dequeue(),
                            SoundName = tokens.Dequeue()
                        });
                    }
                    break;

                case "$playersounddup":
                    // $playersounddup <player class> <gender> <slot> <source slot> - skip
                    if (tokens.Count >= 4) { tokens.Dequeue(); tokens.Dequeue(); tokens.Dequeue(); tokens.Dequeue(); }
                    break;

                case "$playercompat":
                    if (tokens.Count >= 4)
                    {
                        info.PlayerCompatibility.Add(new PlayerCompatDefinition
                        {
                            PlayerClass = tokens.Dequeue(),
                            Gender = tokens.Dequeue(),
                            CompatibleClass = tokens.Dequeue(),
                            CompatibleGender = tokens.Dequeue()
                        });
                    }
                    break;

                case "$musicalias":
                    if (tokens.Count >= 2)
                    {
                        var name = tokens.Dequeue().ToLowerInvariant();
                        var target = tokens.Dequeue().ToLowerInvariant();
                        info.MusicAliases[name] = target;
                    }
                    break;

                case "$ambient":
                    ParseAmbient(tokens, info);
                    break;

                case "$environment":
                    if (tokens.Count >= 2 && int.TryParse(tokens.Peek(), out int envId))
                    {
                        tokens.Dequeue();
                        info.Environments.Add(new EnvironmentDefinition
                        {
                            Id = envId,
                            Name = tokens.Count > 0 ? tokens.Dequeue() : ""
                        });
                    }
                    break;

                case "$ifdoom":
                case "$ifheretic":
                case "$ifhexen":
                case "$ifstrife":
                case "$ifchex":
                case "$endif":
                case "$map":
                case "$registered":
                case "$archivepath":
                    // Skip configuration/conditional directives
                    break;
            }
        }

        private void ParseRandom(Queue<string> tokens, SndInfo info)
        {
            if (tokens.Count == 0)
                return;

            var name = tokens.Dequeue().ToLowerInvariant();

            if (tokens.Count > 0 && tokens.Peek() == "{")
                tokens.Dequeue(); // consume {

            var sounds = new List<string>();
            while (tokens.Count > 0 && tokens.Peek() != "}")
                sounds.Add(tokens.Dequeue().ToLowerInvariant());

            if (tokens.Count > 0)
                tokens.Dequeue(); // consume }

            info.RandomSounds[name] = sounds.ToArray();
        }

        private void ParseAmbient(Queue<string> tokens, SndInfo info)
        {
            if (tokens.Count < 2)
                return;

            if (!int.TryParse(tokens.Peek(), out int index))
                return;
            tokens.Dequeue();

            if (tokens.Count == 0)
                return;

            var ambient = new AmbientSoundDefinition
            {
                Index = index,
                SoundName = tokens.Dequeue().ToLowerInvariant(),
                Type = AmbientType.Point,
                PlayMode = AmbientPlayMode.Continuous,
                Volume = 1.0f
            };

            // Parse optional type/mode/volume tokens until we hit a directive or another sound def
            while (tokens.Count > 0)
            {
                var peek = tokens.Peek();
                if (peek.StartsWith("$") || peek == "{" || peek == "}")
                    break;

                var tok = tokens.Dequeue().ToLowerInvariant();

                switch (tok)
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
                        if (tokens.Count > 0 && float.TryParse(tokens.Peek(), out float minTime))
                        {
                            tokens.Dequeue();
                            ambient.MinTime = minTime;
                            if (tokens.Count > 0 && float.TryParse(tokens.Peek(), out float maxTime))
                            {
                                tokens.Dequeue();
                                ambient.MaxTime = maxTime;
                            }
                        }
                        break;
                    case "periodic":
                        ambient.PlayMode = AmbientPlayMode.Periodic;
                        if (tokens.Count > 0 && float.TryParse(tokens.Peek(), out float period))
                        {
                            tokens.Dequeue();
                            ambient.MinTime = period;
                            ambient.MaxTime = period;
                        }
                        break;
                    default:
                        if (float.TryParse(tok, out float vol))
                            ambient.Volume = vol;
                        break;
                }
            }

            info.AmbientSounds[index] = ambient;
        }
    }
}
