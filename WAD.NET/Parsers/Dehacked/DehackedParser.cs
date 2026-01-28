using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace WAD.NET.Parsers.Dehacked
{
    /// <summary>
    /// Parser for DEHACKED patch files.
    /// Supports standard DEH format, BEX extensions, and MBF21 extensions.
    /// </summary>
    public class DehackedParser
    {
        private readonly string[] _lines;
        private int _lineIndex;

        /// <summary>
        /// Creates a new DEHACKED parser.
        /// </summary>
        /// <param name="content">The DEHACKED file content.</param>
        public DehackedParser(string content)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            _lines = content.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        }

        /// <summary>
        /// Parses the DEHACKED content.
        /// </summary>
        /// <returns>A parsed DehackedPatch object.</returns>
        public DehackedPatch Parse()
        {
            var patch = new DehackedPatch();
            _lineIndex = 0;

            while (_lineIndex < _lines.Length)
            {
                var line = _lines[_lineIndex].Trim();

                // Skip empty lines and comments
                if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                {
                    _lineIndex++;
                    continue;
                }

                if (line.StartsWith("Patch File for DeHackEd", StringComparison.OrdinalIgnoreCase))
                {
                    // Skip header line
                }
                else if (line.StartsWith("Doom version", StringComparison.OrdinalIgnoreCase))
                {
                    patch.DoomVersion = ParseIntValue(line);
                }
                else if (line.StartsWith("Patch format", StringComparison.OrdinalIgnoreCase))
                {
                    patch.PatchFormat = ParseIntValue(line);
                }
                else if (line.StartsWith("Thing ", StringComparison.OrdinalIgnoreCase))
                {
                    patch.Things.Add(ParseThing());
                    continue; // ParseThing advances _lineIndex
                }
                else if (line.StartsWith("Frame ", StringComparison.OrdinalIgnoreCase))
                {
                    patch.Frames.Add(ParseFrame());
                    continue;
                }
                else if (line.StartsWith("Weapon ", StringComparison.OrdinalIgnoreCase))
                {
                    patch.Weapons.Add(ParseWeapon());
                    continue;
                }
                else if (line.StartsWith("Ammo ", StringComparison.OrdinalIgnoreCase))
                {
                    patch.Ammo.Add(ParseAmmo());
                    continue;
                }
                else if (line.StartsWith("Sound ", StringComparison.OrdinalIgnoreCase))
                {
                    patch.Sounds.Add(ParseSound());
                    continue;
                }
                else if (line.StartsWith("Sprite ", StringComparison.OrdinalIgnoreCase))
                {
                    patch.Sprites.Add(ParseSprite());
                    continue;
                }
                else if (line.StartsWith("Text ", StringComparison.OrdinalIgnoreCase))
                {
                    patch.Texts.Add(ParseText(line));
                    continue;
                }
                else if (line.StartsWith("[CODEPTR]", StringComparison.OrdinalIgnoreCase))
                {
                    ParseCodePointers(patch);
                    continue;
                }
                else if (line.StartsWith("[STRINGS]", StringComparison.OrdinalIgnoreCase))
                {
                    ParseStrings(patch);
                    continue;
                }
                else if (line.StartsWith("[PARS]", StringComparison.OrdinalIgnoreCase))
                {
                    ParsePars(patch);
                    continue;
                }
                else if (line.StartsWith("[CHEATS]", StringComparison.OrdinalIgnoreCase))
                {
                    ParseCheats(patch);
                    continue;
                }
                else if (line.StartsWith("Cheat ", StringComparison.OrdinalIgnoreCase))
                {
                    // Old-style cheat definition
                    patch.Cheats.Add(ParseCheatLine(line));
                }
                else if (line.StartsWith("Misc ", StringComparison.OrdinalIgnoreCase) ||
                         line.Equals("Misc", StringComparison.OrdinalIgnoreCase))
                {
                    patch.Misc = ParseMisc();
                    continue;
                }

                _lineIndex++;
            }

            return patch;
        }

        private int ParseIntValue(string line)
        {
            var match = Regex.Match(line, @"=\s*(-?\d+)");
            if (match.Success)
                return int.Parse(match.Groups[1].Value);
            return 0;
        }

        private DehThing ParseThing()
        {
            var header = _lines[_lineIndex];
            var match = Regex.Match(header, @"Thing\s+(\d+)(?:\s*\(([^)]+)\))?", RegexOptions.IgnoreCase);

            var thing = new DehThing
            {
                Index = match.Success ? int.Parse(match.Groups[1].Value) : 0,
                Name = match.Groups[2].Success ? match.Groups[2].Value : null
            };

            _lineIndex++;

            while (_lineIndex < _lines.Length)
            {
                var line = _lines[_lineIndex].Trim();
                if (string.IsNullOrEmpty(line) || !line.Contains("="))
                    break;

                var parts = line.Split(new[] { '=' }, 2);
                var key = parts[0].Trim();
                var value = ParseIntFromValue(parts[1].Trim());

                switch (key.ToLowerInvariant())
                {
                    case "hit points": thing.HitPoints = value; break;
                    case "speed": thing.Speed = value; break;
                    case "reaction time": thing.ReactionTime = value; break;
                    case "pain chance": thing.PainChance = value; break;
                    case "damage": thing.Damage = value; break;
                    case "mass": thing.Mass = value; break;
                    case "width": thing.Width = value; break;
                    case "height": thing.Height = value; break;
                    case "bits": thing.Flags = (uint)value; break;
                    case "mbf21 bits": thing.MBF21Flags = (uint)value; break;
                    case "id #": /* Editor number, not stored */ break;
                    case "initial frame": thing.SpawnState = value; break;
                    case "first moving frame": thing.SeeState = value; break;
                    case "injury frame": thing.PainState = value; break;
                    case "close attack frame": thing.MeleeState = value; break;
                    case "far attack frame": thing.MissileState = value; break;
                    case "death frame": thing.DeathState = value; break;
                    case "exploding frame": thing.XDeathState = value; break;
                    case "respawn frame": thing.RaiseState = value; break;
                    case "alert sound": thing.AlertSound = value; break;
                    case "attack sound": thing.AttackSound = value; break;
                    case "pain sound": thing.PainSound = value; break;
                    case "death sound": thing.DeathSound = value; break;
                    case "action sound": thing.ActionSound = value; break;
                    case "dropped item": thing.DropItem = value; break;
                    case "infighting group": thing.InfightingGroup = value; break;
                    case "projectile group": thing.ProjectileGroup = value; break;
                    case "splash group": thing.SplashGroup = value; break;
                    case "melee range": thing.MeleeRange = value; break;
                    case "rip sound": thing.RipSound = value; break;
                }

                _lineIndex++;
            }

            return thing;
        }

        private DehFrame ParseFrame()
        {
            var header = _lines[_lineIndex];
            var match = Regex.Match(header, @"Frame\s+(\d+)", RegexOptions.IgnoreCase);

            var frame = new DehFrame
            {
                Index = match.Success ? int.Parse(match.Groups[1].Value) : 0
            };

            _lineIndex++;

            while (_lineIndex < _lines.Length)
            {
                var line = _lines[_lineIndex].Trim();
                if (string.IsNullOrEmpty(line) || !line.Contains("="))
                    break;

                var parts = line.Split(new[] { '=' }, 2);
                var key = parts[0].Trim();
                var value = ParseIntFromValue(parts[1].Trim());

                switch (key.ToLowerInvariant())
                {
                    case "sprite number": frame.SpriteNumber = value; break;
                    case "sprite subnumber": frame.SpriteSubnumber = value; break;
                    case "duration": frame.Duration = value; break;
                    case "next frame": frame.NextState = value; break;
                    case "unknown 1":
                    case "misc1": frame.Misc1 = value; break;
                    case "unknown 2":
                    case "misc2": frame.Misc2 = value; break;
                    default:
                        // Handle MBF21 Args1-8
                        var argMatch = Regex.Match(key, @"Args(\d+)", RegexOptions.IgnoreCase);
                        if (argMatch.Success)
                        {
                            int argIndex = int.Parse(argMatch.Groups[1].Value) - 1;
                            if (frame.Args == null)
                                frame.Args = new int[8];
                            if (argIndex >= 0 && argIndex < 8)
                                frame.Args[argIndex] = value;
                        }
                        break;
                }

                _lineIndex++;
            }

            return frame;
        }

        private DehWeapon ParseWeapon()
        {
            var header = _lines[_lineIndex];
            var match = Regex.Match(header, @"Weapon\s+(\d+)(?:\s*\(([^)]+)\))?", RegexOptions.IgnoreCase);

            var weapon = new DehWeapon
            {
                Index = match.Success ? int.Parse(match.Groups[1].Value) : 0,
                Name = match.Groups[2].Success ? match.Groups[2].Value : null
            };

            _lineIndex++;

            while (_lineIndex < _lines.Length)
            {
                var line = _lines[_lineIndex].Trim();
                if (string.IsNullOrEmpty(line) || !line.Contains("="))
                    break;

                var parts = line.Split(new[] { '=' }, 2);
                var key = parts[0].Trim();
                var value = ParseIntFromValue(parts[1].Trim());

                switch (key.ToLowerInvariant())
                {
                    case "ammo type": weapon.AmmoType = value; break;
                    case "deselect frame": weapon.DeselectState = value; break;
                    case "select frame": weapon.SelectState = value; break;
                    case "bobbing frame": weapon.ReadyState = value; break;
                    case "shooting frame": weapon.AttackState = value; break;
                    case "firing frame": weapon.FlashState = value; break;
                    case "ammo per shot": weapon.AmmoPerShot = value; break;
                    case "mbf21 bits": weapon.MBF21Flags = (uint)value; break;
                }

                _lineIndex++;
            }

            return weapon;
        }

        private DehAmmo ParseAmmo()
        {
            var header = _lines[_lineIndex];
            var match = Regex.Match(header, @"Ammo\s+(\d+)", RegexOptions.IgnoreCase);

            var ammo = new DehAmmo
            {
                Index = match.Success ? int.Parse(match.Groups[1].Value) : 0
            };

            _lineIndex++;

            while (_lineIndex < _lines.Length)
            {
                var line = _lines[_lineIndex].Trim();
                if (string.IsNullOrEmpty(line) || !line.Contains("="))
                    break;

                var parts = line.Split(new[] { '=' }, 2);
                var key = parts[0].Trim();
                var value = ParseIntFromValue(parts[1].Trim());

                switch (key.ToLowerInvariant())
                {
                    case "max ammo": ammo.MaxAmmo = value; break;
                    case "per ammo": ammo.PerAmmo = value; break;
                }

                _lineIndex++;
            }

            return ammo;
        }

        private DehSound ParseSound()
        {
            var header = _lines[_lineIndex];
            var match = Regex.Match(header, @"Sound\s+(\d+)", RegexOptions.IgnoreCase);

            var sound = new DehSound
            {
                Index = match.Success ? int.Parse(match.Groups[1].Value) : 0
            };

            _lineIndex++;

            while (_lineIndex < _lines.Length)
            {
                var line = _lines[_lineIndex].Trim();
                if (string.IsNullOrEmpty(line) || !line.Contains("="))
                    break;

                var parts = line.Split(new[] { '=' }, 2);
                var key = parts[0].Trim();
                var value = ParseIntFromValue(parts[1].Trim());

                switch (key.ToLowerInvariant())
                {
                    case "zero/one":
                    case "zero 1": sound.ZeroOne = value; break;
                    case "value":
                    case "priority": sound.Priority = value; break;
                    case "zero 2":
                    case "link": sound.Link = value; break;
                    case "zero 3":
                    case "pitch": sound.Pitch = value; break;
                    case "zero 4":
                    case "volume": sound.Volume = value; break;
                }

                _lineIndex++;
            }

            return sound;
        }

        private DehSprite ParseSprite()
        {
            var header = _lines[_lineIndex];
            var match = Regex.Match(header, @"Sprite\s+(\d+)", RegexOptions.IgnoreCase);

            var sprite = new DehSprite
            {
                Index = match.Success ? int.Parse(match.Groups[1].Value) : 0
            };

            _lineIndex++;

            while (_lineIndex < _lines.Length)
            {
                var line = _lines[_lineIndex].Trim();
                if (string.IsNullOrEmpty(line) || !line.Contains("="))
                    break;

                var parts = line.Split(new[] { '=' }, 2);
                var key = parts[0].Trim();
                var value = ParseIntFromValue(parts[1].Trim());

                if (key.Equals("Offset", StringComparison.OrdinalIgnoreCase))
                {
                    sprite.Offset = value;
                }

                _lineIndex++;
            }

            return sprite;
        }

        private DehText ParseText(string headerLine)
        {
            var match = Regex.Match(headerLine, @"Text\s+(\d+)\s+(\d+)", RegexOptions.IgnoreCase);

            int oldLen = match.Success ? int.Parse(match.Groups[1].Value) : 0;
            int newLen = match.Success ? int.Parse(match.Groups[2].Value) : 0;

            _lineIndex++;

            // Read the text block
            var textBuilder = new System.Text.StringBuilder();
            int charsRead = 0;
            int totalChars = oldLen + newLen;

            while (_lineIndex < _lines.Length && charsRead < totalChars)
            {
                var line = _lines[_lineIndex];
                textBuilder.AppendLine(line);
                charsRead += line.Length + 1; // +1 for newline
                _lineIndex++;
            }

            var fullText = textBuilder.ToString();

            // Split into old and new text
            string oldText = oldLen > 0 && fullText.Length >= oldLen
                ? fullText.Substring(0, oldLen)
                : fullText;
            string newText = fullText.Length > oldLen
                ? fullText.Substring(oldLen, Math.Min(newLen, fullText.Length - oldLen))
                : "";

            return new DehText
            {
                OldLength = oldLen,
                NewLength = newLen,
                OldText = oldText.TrimEnd('\r', '\n'),
                NewText = newText.TrimEnd('\r', '\n')
            };
        }

        private void ParseCodePointers(DehackedPatch patch)
        {
            _lineIndex++;

            while (_lineIndex < _lines.Length)
            {
                var line = _lines[_lineIndex].Trim();

                if (string.IsNullOrEmpty(line))
                {
                    _lineIndex++;
                    continue;
                }

                // Stop at next section (but not "Frame X = " which is valid in CODEPTR)
                if (line.StartsWith("[") || line.StartsWith("Thing ") ||
                    line.StartsWith("Weapon ") || line.StartsWith("Ammo ") ||
                    line.StartsWith("Sound ") || line.StartsWith("Sprite ") ||
                    line.StartsWith("Text ") || line.StartsWith("Misc") ||
                    (line.StartsWith("Frame ") && !line.Contains("=")))
                    break;

                // Parse: Frame <num> = <pointer>
                var match = Regex.Match(line, @"Frame\s+(\d+)\s*=\s*(\w+)", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    patch.CodePointers.Add(new DehCodePointer
                    {
                        FrameIndex = int.Parse(match.Groups[1].Value),
                        CodePointerName = match.Groups[2].Value
                    });
                }

                _lineIndex++;
            }
        }

        private void ParseStrings(DehackedPatch patch)
        {
            _lineIndex++;

            while (_lineIndex < _lines.Length)
            {
                var line = _lines[_lineIndex].Trim();

                if (string.IsNullOrEmpty(line))
                {
                    _lineIndex++;
                    continue;
                }

                // Stop at next section
                if (line.StartsWith("[") || line.StartsWith("Thing ") ||
                    line.StartsWith("Frame ") || line.StartsWith("Weapon "))
                    break;

                // Parse: MNEMONIC = value
                var match = Regex.Match(line, @"^(\w+)\s*=\s*(.*)$");
                if (match.Success)
                {
                    var value = match.Groups[2].Value;

                    // Handle multi-line strings (BEX format uses backslash continuation)
                    while (value.EndsWith("\\") && _lineIndex + 1 < _lines.Length)
                    {
                        value = value.Substring(0, value.Length - 1);
                        _lineIndex++;
                        value += "\n" + _lines[_lineIndex].TrimEnd();
                    }

                    patch.Strings.Add(new DehString
                    {
                        Mnemonic = match.Groups[1].Value,
                        Value = value
                    });
                }

                _lineIndex++;
            }
        }

        private void ParsePars(DehackedPatch patch)
        {
            _lineIndex++;

            while (_lineIndex < _lines.Length)
            {
                var line = _lines[_lineIndex].Trim();

                if (string.IsNullOrEmpty(line))
                {
                    _lineIndex++;
                    continue;
                }

                // Stop at next section
                if (line.StartsWith("[") || line.StartsWith("Thing ") ||
                    line.StartsWith("Frame ") || line.StartsWith("Weapon "))
                    break;

                // Parse: par <episode> <map> <seconds>  (DOOM)
                // or:    par <map> <seconds>            (DOOM II)
                var match = Regex.Match(line, @"par\s+(\d+)\s+(\d+)(?:\s+(\d+))?", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    if (match.Groups[3].Success)
                    {
                        // DOOM format: episode map seconds
                        patch.Pars.Add(new DehPar
                        {
                            Episode = int.Parse(match.Groups[1].Value),
                            Map = int.Parse(match.Groups[2].Value),
                            Seconds = int.Parse(match.Groups[3].Value)
                        });
                    }
                    else
                    {
                        // DOOM II format: map seconds
                        patch.Pars.Add(new DehPar
                        {
                            Episode = 0,
                            Map = int.Parse(match.Groups[1].Value),
                            Seconds = int.Parse(match.Groups[2].Value)
                        });
                    }
                }

                _lineIndex++;
            }
        }

        private void ParseCheats(DehackedPatch patch)
        {
            _lineIndex++;

            while (_lineIndex < _lines.Length)
            {
                var line = _lines[_lineIndex].Trim();

                if (string.IsNullOrEmpty(line))
                {
                    _lineIndex++;
                    continue;
                }

                // Stop at next section
                if (line.StartsWith("[") || line.StartsWith("Thing ") ||
                    line.StartsWith("Frame ") || line.StartsWith("Weapon "))
                    break;

                var cheat = ParseCheatLine(line);
                if (cheat != null)
                    patch.Cheats.Add(cheat);

                _lineIndex++;
            }
        }

        private DehCheat ParseCheatLine(string line)
        {
            // Format: "Cheat <name> = <sequence>" or just "<name> = <sequence>" in [CHEATS] section
            var match = Regex.Match(line, @"(?:Cheat\s+)?(\w+)\s*=\s*(.+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return new DehCheat
                {
                    Name = match.Groups[1].Value,
                    Sequence = match.Groups[2].Value.Trim()
                };
            }
            return null!;
        }

        private DehMisc ParseMisc()
        {
            var misc = new DehMisc();

            _lineIndex++;

            while (_lineIndex < _lines.Length)
            {
                var line = _lines[_lineIndex].Trim();
                if (string.IsNullOrEmpty(line) || !line.Contains("="))
                    break;

                var parts = line.Split(new[] { '=' }, 2);
                var key = parts[0].Trim();
                var value = ParseIntFromValue(parts[1].Trim());

                switch (key.ToLowerInvariant())
                {
                    case "initial health": misc.InitialHealth = value; break;
                    case "initial bullets": misc.InitialBullets = value; break;
                    case "max health": misc.MaxHealth = value; break;
                    case "max armor": misc.MaxArmor = value; break;
                    case "green armor class": misc.GreenArmorClass = value; break;
                    case "blue armor class": misc.BlueArmorClass = value; break;
                    case "max soulsphere": misc.MaxSoulsphere = value; break;
                    case "soulsphere health": misc.SoulsphereHealth = value; break;
                    case "megasphere health": misc.MegasphereHealth = value; break;
                    case "god mode health": misc.GodModeHealth = value; break;
                    case "idfa armor": misc.IdfaArmor = value; break;
                    case "idfa armor class": misc.IdfaArmorClass = value; break;
                    case "idkfa armor": misc.IdkfaArmor = value; break;
                    case "idkfa armor class": misc.IdkfaArmorClass = value; break;
                    case "bfg cells/shot": misc.BfgCellsPerShot = value; break;
                    case "monsters infight": misc.MonstersInfight = value; break;
                }

                _lineIndex++;
            }

            return misc;
        }

        private int ParseIntFromValue(string value)
        {
            // Handle hex values (0x...) and decimal
            value = value.Trim();
            if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return Convert.ToInt32(value, 16);
            }

            // Extract just the number part (ignore trailing comments)
            var match = Regex.Match(value, @"^(-?\d+)");
            if (match.Success)
            {
                return int.Parse(match.Groups[1].Value);
            }

            return 0;
        }
    }
}
