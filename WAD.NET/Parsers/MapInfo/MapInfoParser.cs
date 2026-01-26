using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace WAD.NET.Parsers.MapInfo
{
    /// <summary>
    /// Parser for MAPINFO lumps (ZDoom/GZDoom format).
    /// </summary>
    public class MapInfoParser
    {
        private readonly Queue<string> _tokens;
        private int _episodeCounter;

        /// <summary>
        /// Creates a new MAPINFO parser.
        /// </summary>
        /// <param name="content">The MAPINFO content.</param>
        public MapInfoParser(string content)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            _tokens = Tokenize(content);
        }

        /// <summary>
        /// Parses the MAPINFO content.
        /// </summary>
        /// <returns>A parsed MapInfo object.</returns>
        public MapInfo Parse()
        {
            var info = new MapInfo();
            _episodeCounter = 1;

            while (_tokens.Count > 0)
            {
                var token = _tokens.Dequeue();
                var lowerToken = token.ToLowerInvariant();

                switch (lowerToken)
                {
                    case "map":
                        var mapDef = ParseMapDefinition();
                        info.Maps[mapDef.MapLump.ToUpperInvariant()] = mapDef;
                        break;

                    case "defaultmap":
                        // Skip defaultmap block, just consume it
                        if (_tokens.Count > 0 && PeekToken() == "{")
                            SkipBlock();
                        break;

                    case "adddefaultmap":
                        if (_tokens.Count > 0 && PeekToken() == "{")
                            SkipBlock();
                        break;

                    case "cluster":
                    case "clusterdef":
                        var clusterDef = ParseClusterDefinition();
                        info.Clusters[clusterDef.Id] = clusterDef;
                        break;

                    case "episode":
                        var episodeDef = ParseEpisodeDefinition();
                        info.Episodes[episodeDef.Number] = episodeDef;
                        break;

                    case "clearepisodes":
                        info.ClearEpisodes = true;
                        info.Episodes.Clear();
                        _episodeCounter = 1;
                        break;

                    case "skill":
                        info.Skills.Add(ParseSkillDefinition());
                        break;

                    case "clearskills":
                        info.Skills.Clear();
                        break;

                    case "gameinfo":
                        info.GameInfo = ParseGameInfo();
                        break;

                    case "doomednums":
                    case "spawnnums":
                    case "conversationids":
                        // Skip these blocks
                        if (_tokens.Count > 0 && PeekToken() == "{")
                            SkipBlock();
                        break;

                    case "include":
                        // Just consume the include path, we can't actually include
                        if (_tokens.Count > 0)
                            _tokens.Dequeue();
                        break;
                }
            }

            return info;
        }

        private Queue<string> Tokenize(string content)
        {
            var tokens = new Queue<string>();

            // Remove comments
            content = Regex.Replace(content, @"//[^\n]*", "");
            content = Regex.Replace(content, @"/\*[\s\S]*?\*/", "");

            // Tokenize
            var pattern = new Regex(@"
                ""([^""\\]*(?:\\.[^""\\]*)*)""  |  # Quoted string
                '([^'\\]*(?:\\.[^'\\]*)*)'      |  # Single-quoted string
                (\{|\}|=|,)                     |  # Punctuation
                ([^\s\{\}=,""\',]+)                # Words
            ", RegexOptions.IgnorePatternWhitespace);

            foreach (Match match in pattern.Matches(content))
            {
                if (match.Groups[1].Success)
                    tokens.Enqueue(match.Groups[1].Value); // Quoted string (without quotes)
                else if (match.Groups[2].Success)
                    tokens.Enqueue(match.Groups[2].Value); // Single-quoted string
                else if (match.Groups[3].Success)
                    tokens.Enqueue(match.Groups[3].Value); // Punctuation
                else if (match.Groups[4].Success)
                    tokens.Enqueue(match.Groups[4].Value); // Word
            }

            return tokens;
        }

        private string PeekToken()
        {
            return _tokens.Count > 0 ? _tokens.Peek() : null;
        }

        private void Expect(string expected)
        {
            if (_tokens.Count == 0)
                throw new FormatException($"Expected '{expected}' but reached end of input");

            var token = _tokens.Dequeue();
            if (!token.Equals(expected, StringComparison.OrdinalIgnoreCase))
                throw new FormatException($"Expected '{expected}' but got '{token}'");
        }

        private bool TryConsume(string expected)
        {
            if (_tokens.Count > 0 && _tokens.Peek().Equals(expected, StringComparison.OrdinalIgnoreCase))
            {
                _tokens.Dequeue();
                return true;
            }
            return false;
        }

        private void SkipBlock()
        {
            Expect("{");
            int depth = 1;
            while (depth > 0 && _tokens.Count > 0)
            {
                var token = _tokens.Dequeue();
                if (token == "{") depth++;
                else if (token == "}") depth--;
            }
        }

        private string ParseString()
        {
            if (_tokens.Count == 0)
                return "";
            return _tokens.Dequeue();
        }

        private int ParseInt()
        {
            var token = ParseString();
            if (int.TryParse(token, out int result))
                return result;
            return 0;
        }

        private float ParseFloat()
        {
            var token = ParseString();
            if (float.TryParse(token, out float result))
                return result;
            return 0;
        }

        private MapDefinition ParseMapDefinition()
        {
            var def = new MapDefinition
            {
                Flags = new MapFlags()
            };

            def.MapLump = ParseString();

            // Optional lookup or nice name
            if (_tokens.Count > 0)
            {
                var next = PeekToken();
                if (next != "{" && next != "lookup")
                {
                    def.NiceName = ParseString();
                }
                else if (next == "lookup")
                {
                    _tokens.Dequeue(); // consume "lookup"
                    def.LookupName = ParseString();
                }
            }

            if (!TryConsume("{"))
                return def;

            while (_tokens.Count > 0 && PeekToken() != "}")
            {
                var key = _tokens.Dequeue().ToLowerInvariant();
                TryConsume("="); // Optional equals sign

                switch (key)
                {
                    case "levelnum": def.LevelNum = ParseInt(); break;
                    case "titlepatch": def.TitlePatch = ParseString(); break;
                    case "next": def.Next = ParseString(); break;
                    case "secretnext": def.SecretNext = ParseString(); break;
                    case "sky1": def.Sky1 = ParseString(); TryConsume(","); if (_tokens.Count > 0 && char.IsDigit(PeekToken()[0])) ParseFloat(); break;
                    case "sky2": def.Sky2 = ParseString(); TryConsume(","); if (_tokens.Count > 0 && char.IsDigit(PeekToken()[0])) ParseFloat(); break;
                    case "music": def.Music = ParseString(); break;
                    case "intermusic": def.InterMusic = ParseString(); break;
                    case "cluster": def.Cluster = ParseInt(); break;
                    case "par": def.Par = ParseInt(); break;
                    case "gravity": def.Gravity = ParseFloat(); break;
                    case "aircontrol": def.AirControl = ParseFloat(); break;
                    case "fade": def.Fade = ParseString(); break;
                    case "outsidefog": def.OutsideFog = ParseString(); break;
                    case "bordertexture": def.BorderTexture = ParseString(); break;
                    case "enterpic": def.EnterPic = ParseString(); break;
                    case "exitpic": def.ExitPic = ParseString(); break;
                    case "sndseq": def.SndSeq = ParseString(); break;
                    case "sndinfo": def.SndInfo = ParseString(); break;
                    case "cdtrack": def.CdTrack = ParseInt(); break;
                    case "author": def.Author = ParseString(); break;

                    // Flags
                    case "nointermission": def.NoIntermission = true; break;
                    case "allowmonstertelefrags": def.AllowMonsterTelefrags = true; break;
                    case "allowrespawn": def.AllowRespawn = true; break;
                    case "secretlevel": def.SecretLevel = true; break;
                    case "lightning": def.Lightning = true; break;
                    case "evenlighting": def.EvenLighting = true; break;
                    case "smoothlighting": def.SmoothLighting = true; break;
                    case "nosoundclipping": def.Flags.NoSoundClipping = true; break;
                    case "allowcrouch": def.Flags.AllowCrouch = true; break;
                    case "allowjump": def.Flags.AllowJump = true; break;
                    case "nocrouch": def.Flags.NoCrouch = true; break;
                    case "nojump": def.Flags.NoJump = true; break;
                    case "nofreelook": def.Flags.NoFreeLook = true; break;
                    case "allowfreelook": def.Flags.AllowFreeLook = true; break;
                    case "noinfighting": def.Flags.NoInfighting = true; break;
                    case "normalinfighting": def.Flags.NormalInfighting = true; break;
                    case "totalinfighting": def.Flags.TotalInfighting = true; break;
                    case "infiniteflightpowerup": def.Flags.InfiniteFlightPowerup = true; break;
                    case "noautosequences": def.Flags.NoAutosequences = true; break;
                    case "forcenoskystretch": def.Flags.ForceNoSkyStretch = true; break;
                    case "allowmonsterrespawn": def.Flags.AllowMonsterRespawn = true; break;
                    case "noinventorybar": def.Flags.NoInventoryBar = true; break;
                    case "activateowndeathspecials": def.Flags.ActivateOwnDeathSpecials = true; break;
                    case "killeractivatesdeathspecials": def.Flags.KillerActivatesDeathSpecials = true; break;
                    case "missilesactivateimpactlines": def.Flags.MissilesActivateImpactLines = true; break;
                    case "filterstarts": def.Flags.FilterStarts = true; break;
                    case "teamplayon": def.Flags.TeamplayOn = true; break;
                    case "teamplayoff": def.Flags.TeamplayOff = true; break;
                    case "checkswitchrange": def.Flags.CheckSwitchRange = true; break;
                    case "nocheckswitchrange": def.Flags.NoCheckSwitchRange = true; break;
                    case "resethealth": def.Flags.ResetHealth = true; break;
                    case "resetinventory": def.Flags.ResetInventory = true; break;
                    case "resetitems": def.Flags.ResetItems = true; break;
                    case "fallingdamage": def.Flags.FallingDamage = true; break;
                    case "nofallingdamage": def.Flags.NoFallingDamage = true; break;
                    case "oldfallingdamage": def.Flags.OldFallingDamage = true; break;
                    case "strifefallingdamage": def.Flags.StrifeFallingDamage = true; break;
                    case "noautosavehint": def.Flags.NoAutoSaveHint = true; break;

                    case "specialaction":
                        def.SpecialActions.Add(ParseSpecialAction());
                        break;
                }
            }

            TryConsume("}");
            return def;
        }

        private SpecialAction ParseSpecialAction()
        {
            var action = new SpecialAction();
            TryConsume("=");

            action.ActorClass = ParseString();
            TryConsume(",");
            action.Special = ParseInt();

            for (int i = 0; i < 5 && _tokens.Count > 0 && PeekToken() != "}"; i++)
            {
                if (!TryConsume(","))
                    break;
                action.Args[i] = ParseInt();
            }

            return action;
        }

        private ClusterDefinition ParseClusterDefinition()
        {
            var def = new ClusterDefinition
            {
                Id = ParseInt()
            };

            if (!TryConsume("{"))
                return def;

            while (_tokens.Count > 0 && PeekToken() != "}")
            {
                var key = _tokens.Dequeue().ToLowerInvariant();
                TryConsume("=");

                switch (key)
                {
                    case "flat": def.Flat = ParseString(); break;
                    case "music": def.Music = ParseString(); break;
                    case "pic": def.Pic = ParseString(); break;
                    case "hub": def.Hub = true; break;
                    case "entertext":
                        if (TryConsume("lookup"))
                            def.EnterTextLookup = ParseString();
                        else
                            def.EnterText = ParseMultilineText();
                        break;
                    case "exittext":
                        if (TryConsume("lookup"))
                            def.ExitTextLookup = ParseString();
                        else
                            def.ExitText = ParseMultilineText();
                        break;
                    case "exittextislump": def.ExitTextIsLump = true; break;
                    case "entertextislump": def.EnterTextIsLump = true; break;
                }
            }

            TryConsume("}");
            return def;
        }

        private string ParseMultilineText()
        {
            var lines = new List<string>();

            // First line or opening brace
            var firstToken = ParseString();
            if (firstToken == "{")
            {
                // Multi-line format
                while (_tokens.Count > 0 && PeekToken() != "}")
                {
                    lines.Add(ParseString());
                    TryConsume(",");
                }
                TryConsume("}");
            }
            else
            {
                // Single line
                lines.Add(firstToken);
            }

            return string.Join("\n", lines);
        }

        private EpisodeDefinition ParseEpisodeDefinition()
        {
            var def = new EpisodeDefinition
            {
                Number = _episodeCounter++
            };

            def.StartMap = ParseString();

            // Check for optional properties before the block
            while (_tokens.Count > 0 && PeekToken() != "{")
            {
                var next = PeekToken().ToLowerInvariant();
                if (next == "teaser")
                {
                    _tokens.Dequeue();
                    def.Optional = true;
                    def.StartMap = ParseString();
                }
                else if (next == "name" || next == "picname" || next == "key" || next == "lookup")
                {
                    break; // These go in the block
                }
                else
                {
                    def.Name = ParseString();
                    break;
                }
            }

            if (!TryConsume("{"))
                return def;

            while (_tokens.Count > 0 && PeekToken() != "}")
            {
                var key = _tokens.Dequeue().ToLowerInvariant();
                TryConsume("=");

                switch (key)
                {
                    case "name": def.Name = ParseString(); break;
                    case "lookup": def.LookupName = ParseString(); break;
                    case "picname": def.PicName = ParseString(); break;
                    case "key": def.Key = ParseString()[0]; break;
                    case "noskillmenu": def.NoSkillMenu = true; break;
                    case "optional": def.Optional = true; break;
                }
            }

            TryConsume("}");
            return def;
        }

        private SkillDefinition ParseSkillDefinition()
        {
            var def = new SkillDefinition();

            def.Name = ParseString();

            if (!TryConsume("{"))
                return def;

            while (_tokens.Count > 0 && PeekToken() != "}")
            {
                var key = _tokens.Dequeue().ToLowerInvariant();
                TryConsume("=");

                switch (key)
                {
                    case "ammofactor": def.AmmoFactor = ParseFloat(); break;
                    case "damagefactor": def.DamageFactor = ParseFloat(); break;
                    case "respawntime": def.RespawnTime = ParseInt(); break;
                    case "aggressiveness": def.AggressiveFactor = ParseFloat(); break;
                    case "healthfactor": def.HealthFactor = ParseFloat(); break;
                    case "key": def.Key = ParseString()[0]; break;
                    case "fastmonsters": def.FastMonsters = true; break;
                    case "disablecheats": def.DisableCheats = true; break;
                    case "respawn": def.MonstersRespawn = true; break;
                    case "spawnfilter": def.SpawnFilter = ParseInt(); break;
                    case "acsreturn": def.AcsReturn = ParseInt(); break;
                    case "mustconfirm": def.MustConfirm = true; if (_tokens.Count > 0 && PeekToken() != "}") def.MustConfirmMessage = ParseString(); break;
                    case "picname": def.PicName = ParseString(); break;
                    case "textcolor": def.TextColor = ParseString(); break;
                    case "name": def.Name = ParseString(); break;
                }
            }

            TryConsume("}");
            return def;
        }

        private GameInfo ParseGameInfo()
        {
            var info = new GameInfo();

            if (!TryConsume("{"))
                return info;

            while (_tokens.Count > 0 && PeekToken() != "}")
            {
                var key = _tokens.Dequeue().ToLowerInvariant();
                TryConsume("=");

                switch (key)
                {
                    case "titlepage": info.TitlePage = ParseString(); break;
                    case "creditpage": info.CreditPage = ParseString(); break;
                    case "finaleflat": info.FinaleFlat = ParseString(); break;
                    case "finalemusic": info.FinaleMusic = ParseString(); break;
                    case "infopage": info.InfoPage = ParseString(); break;
                    case "borderflat": info.BorderFlat = ParseString(); break;
                    case "quitmessages":
                        if (TryConsume("{"))
                        {
                            while (_tokens.Count > 0 && PeekToken() != "}")
                            {
                                info.QuitMessages.Add(ParseString());
                                TryConsume(",");
                            }
                            TryConsume("}");
                        }
                        break;
                    case "playerclasses":
                        if (TryConsume("{"))
                        {
                            while (_tokens.Count > 0 && PeekToken() != "}")
                            {
                                info.PlayerClasses.Add(ParseString());
                                TryConsume(",");
                            }
                            TryConsume("}");
                        }
                        break;
                    default:
                        // Skip unknown properties
                        if (PeekToken() == "{")
                            SkipBlock();
                        else
                            ParseString();
                        break;
                }
            }

            TryConsume("}");
            return info;
        }
    }
}
