using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using WAD.NET.LaunchConfig;

namespace WAD.NET.SourcePorts.Zandronum
{
    /// <summary>
    /// Serializes and deserializes <see cref="WadLaunchConfiguration"/> to and from
    /// the Zandronum/Doomseeker INI-style configuration format. Also generates
    /// Zandronum command-line arguments.
    /// </summary>
    public class ZandronumConfigSerializer : ILaunchConfigSerializer
    {
        private const int DefaultPort = 10666;

        /// <summary>
        /// Maps <see cref="GameMode"/> values to the Zandronum gamemode integer used in config files.
        /// </summary>
        private static readonly Dictionary<GameMode, int> GameModeToInt = new Dictionary<GameMode, int>
        {
            { GameMode.SinglePlayer, 0 },
            { GameMode.Cooperative, 1 },
            { GameMode.Deathmatch, 2 },
            { GameMode.TeamDeathmatch, 3 },
            { GameMode.CaptureTheFlag, 4 },
            { GameMode.LastManStanding, 5 },
            { GameMode.Survival, 6 }
        };

        private static readonly Dictionary<int, GameMode> IntToGameMode =
            GameModeToInt.ToDictionary(kv => kv.Value, kv => kv.Key);

        /// <inheritdoc />
        public string Serialize(WadLaunchConfiguration config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            var executablePath = config.ExtraParameters.ContainsKey("executable")
                ? config.ExtraParameters["executable"]
                : "";

            var configName = config.ExtraParameters.ContainsKey("configName")
                ? config.ExtraParameters["configName"]
                : GenerateConfigName();

            var port = config.Port ?? DefaultPort;
            var gamemodeInt = config.Mode.HasValue && GameModeToInt.ContainsKey(config.Mode.Value)
                ? GameModeToInt[config.Mode.Value]
                : 0;

            var map = config.Map ?? "";
            var difficulty = config.Skill ?? 3;
            var maxClients = config.Players ?? 1;
            var maxPlayers = config.Players ?? 1;

            var pwadsList = string.Join(";", config.Files);
            var pwadsOptionalList = string.Join(";", config.Files.Select(_ => "0"));

            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("[%General]");
            sb.AppendLine("engine=Zandronum");
            sb.AppendLine($"executable={executablePath}");
            sb.AppendLine($"name={configName}");
            sb.AppendLine($"port={port}");
            sb.AppendLine($"gamemode={gamemodeInt}");
            sb.AppendLine($"map={map}");
            sb.AppendLine($"iwad={config.Iwad ?? ""}");
            sb.AppendLine($"pwads=\"{pwadsList}\"");
            sb.AppendLine($"pwadsOptional = \"{pwadsOptionalList}\"");
            sb.AppendLine("broadcastToLAN = 0");
            sb.AppendLine("broadcastToMaster = 0");
            sb.AppendLine("upnp = 0");
            sb.AppendLine("upnpPort = 0");
            sb.AppendLine();
            sb.AppendLine("[Rules]");
            sb.AppendLine($"difficulty={difficulty}");
            sb.AppendLine($"modifier=0");
            sb.AppendLine($"maxClients={maxClients}");
            sb.AppendLine($"maxPlayers={maxPlayers}");
            sb.AppendLine("%2Bsv_maxlives=0");
            sb.AppendLine("maplist=");
            sb.AppendLine("randomMapRotation=0");
            sb.AppendLine();
            sb.AppendLine("[Misc]");
            sb.AppendLine("URL=");
            sb.AppendLine("eMail=");
            sb.AppendLine("connectPassword=");
            sb.AppendLine("joinPassword=");
            sb.AppendLine("RConPassword=");
            sb.AppendLine("MOTD=");
            sb.AppendLine("CustomParams=");
            sb.AppendLine();
            sb.AppendLine("[dmflags]");

            // Write dmflags from ExtraParameters or defaults
            foreach (var dmflag in GetDefaultDmflags())
            {
                var value = config.ExtraParameters.ContainsKey(dmflag.Key)
                    ? config.ExtraParameters[dmflag.Key]
                    : dmflag.Value;
                sb.AppendLine($"{dmflag.Key}={value}");
            }

            sb.AppendLine();
            sb.AppendLine("[voting]");
            foreach (var vote in GetDefaultVotingSettings())
            {
                var value = config.ExtraParameters.ContainsKey(vote.Key)
                    ? config.ExtraParameters[vote.Key]
                    : vote.Value;
                sb.AppendLine($"{vote.Key}={value}");
            }

            return sb.ToString().TrimEnd('\r', '\n');
        }

        /// <inheritdoc />
        public string[] ToCommandLineArgs(WadLaunchConfiguration config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            var args = new List<string>();

            if (!string.IsNullOrWhiteSpace(config.Iwad))
            {
                args.Add("-iwad");
                args.Add(config.Iwad);
            }

            foreach (var file in config.Files)
            {
                args.Add("-file");
                args.Add(file);
            }

            if (!string.IsNullOrWhiteSpace(config.Map))
            {
                args.Add("+map");
                args.Add(config.Map);
            }

            if (config.Skill.HasValue)
            {
                args.Add("-skill");
                args.Add(config.Skill.Value.ToString());
            }

            if (config.Players.HasValue && config.Players.Value > 1)
            {
                args.Add("-host");
                args.Add(config.Players.Value.ToString());
            }

            if (config.Port.HasValue)
            {
                args.Add("-port");
                args.Add(config.Port.Value.ToString());
            }

            return args.ToArray();
        }

        /// <inheritdoc />
        public WadLaunchConfiguration Deserialize(string content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));

            var config = new WadLaunchConfiguration();

            // Try parsing as Zandronum join string (command line)
            if (IsJoinString(content))
            {
                return DeserializeJoinString(content);
            }

            // Otherwise parse as INI-style config
            return DeserializeConfigFile(content);
        }

        /// <summary>
        /// Converts a Zandronum join/command string into a <see cref="WadLaunchConfiguration"/>.
        /// This is the migrated logic from the former <c>ConfigUtilities</c> class.
        /// </summary>
        private WadLaunchConfiguration DeserializeJoinString(string joinString)
        {
            var config = new WadLaunchConfiguration();

            // Extract executable path (everything before the first argument flag)
            var exeMatch = Regex.Match(joinString, @"^(.+?)\s");
            if (exeMatch.Success)
            {
                config.ExtraParameters["executable"] = exeMatch.Groups[0].Captures[0].Value;
            }

            // Extract IWAD
            var iwadMatch = Regex.Match(joinString, @"-iwad (.+?)\s");
            if (iwadMatch.Success)
            {
                config.Iwad = iwadMatch.Groups[1].Captures[0].Value;
            }
            else
            {
                config.Iwad = "";
            }

            // Extract files
            var fileMatches = Regex.Matches(joinString, @"-file (.+?)\s");
            if (fileMatches.Count == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(joinString),
                    "No -file arguments found in join string.");
            }
            // Access fileMatches[0] to preserve original behavior that throws on empty
            var _ = fileMatches[0];
            foreach (Match match in fileMatches)
            {
                config.Files.Add(match.Groups[1].Captures[0].Value);
            }

            // Extract connect info
            var connectMatch = Regex.Match(joinString, @"-connect (\S+)");
            if (connectMatch.Success)
            {
                config.ExtraParameters["connect"] = connectMatch.Groups[1].Value;
            }

            // Extract skill
            var skillMatch = Regex.Match(joinString, @"-skill (\d+)");
            if (skillMatch.Success)
            {
                config.Skill = int.Parse(skillMatch.Groups[1].Value);
            }

            // Extract map
            var mapMatch = Regex.Match(joinString, @"(?:-warp|\+map) (\S+)");
            if (mapMatch.Success)
            {
                config.Map = mapMatch.Groups[1].Value;
            }

            return config;
        }

        private WadLaunchConfiguration DeserializeConfigFile(string content)
        {
            var config = new WadLaunchConfiguration();
            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            string currentSection = "";

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    currentSection = trimmed;
                    continue;
                }

                var eqIndex = trimmed.IndexOf('=');
                if (eqIndex < 0) continue;

                var key = trimmed.Substring(0, eqIndex).Trim();
                var value = trimmed.Substring(eqIndex + 1).Trim();

                if (currentSection == "[%General]")
                {
                    switch (key)
                    {
                        case "iwad":
                            config.Iwad = value;
                            break;
                        case "pwads":
                            var pwads = value.Trim('"').Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                            config.Files.AddRange(pwads);
                            break;
                        case "map":
                            if (!string.IsNullOrWhiteSpace(value)) config.Map = value;
                            break;
                        case "port":
                            if (int.TryParse(value, out var port)) config.Port = port;
                            break;
                        case "gamemode":
                            if (int.TryParse(value, out var gm) && IntToGameMode.ContainsKey(gm))
                                config.Mode = IntToGameMode[gm];
                            break;
                        case "executable":
                            config.ExtraParameters["executable"] = value;
                            break;
                        case "name":
                            config.ExtraParameters["configName"] = value;
                            break;
                    }
                }
                else if (currentSection == "[Rules]")
                {
                    if (key == "difficulty" && int.TryParse(value, out var skill))
                        config.Skill = skill;
                    else if (key == "maxPlayers" && int.TryParse(value, out var players))
                        config.Players = players;
                }
                else if (currentSection == "[dmflags]" || currentSection == "[voting]")
                {
                    config.ExtraParameters[key] = value;
                }
            }

            return config;
        }

        private static bool IsJoinString(string content)
        {
            // A join string is a single-line command that typically contains executable path
            // and -iwad/-file flags, rather than INI section headers
            return !content.Contains("[") && (content.Contains("-iwad") || content.Contains("-file"));
        }

        private static string GenerateConfigName()
        {
            return $"NewConfig_{Guid.NewGuid().ToString().Substring(0, 4)}_{new DateTime().ToString("yyyy_MM_dd_hh_mm_ss")}";
        }

        /// <summary>
        /// Provides the legacy <c>ConfigUtilities.ConvertZandronumJoinStringToConfigFile</c> behavior
        /// as a convenience wrapper. Parses a Zandronum join string and produces a Doomseeker-style
        /// INI configuration file string.
        /// </summary>
        /// <param name="joinString">The Zandronum command-line join string.</param>
        /// <param name="configName">Optional config name; a random name is generated if null/empty.</param>
        /// <returns>The INI-style config file content.</returns>
        public string ConvertJoinStringToConfigFile(string joinString, string? configName)
        {
            var config = DeserializeJoinString(joinString);

            if (!string.IsNullOrWhiteSpace(configName))
            {
                config.ExtraParameters["configName"] = configName;
            }

            return Serialize(config);
        }

        private static List<KeyValuePair<string, string>> GetDefaultDmflags()
        {
            return new List<KeyValuePair<string, string>>
            {
                Kv("CompatUseOriginalMissileClippingHeight", "0"),
                Kv("CompatUseSectorBasedSoundTargetCode", "0"),
                Kv("CompatTraceIgnoreLinesWithoutSameSectorOnBothSides", "0"),
                Kv("CompatLimitDehMaxHealthToHealthBonus", "0"),
                Kv("CompatRavensScrollersUseOriginalSpeed", "0"),
                Kv("CompatAddNOGRAVITYFlagToSpheres", "0"),
                Kv("DontStopPlayerScriptsOnDisconnect", "0"),
                Kv("OldZDoomHorizontalThrust", "0"),
                Kv("OldZDoomBridgeDrops", "0"),
                Kv("OldZDoomJumpPhysics", "0"),
                Kv("CompatUseVanillaAutoaimTracerBehavior", "0"),
                Kv("CompatMaskedMidtex", "0"),
                Kv("CompatBadAngles", "0"),
                Kv("CompatFindShortestTexturesLikeDoom", "0"),
                Kv("CompatLimitPainElementals", "0"),
                Kv("CompatSpawnItemDropsOnTheFloor", "0"),
                Kv("CompatNETScriptsAreClientside", "0"),
                Kv("CompatActorsAreInfinitelyTall", "0"),
                Kv("CompatUseBuggierStairBuilding", "0"),
                Kv("CompatDisableBoomDoorLightEffect", "0"),
                Kv("CompatAllSpecialLinesCanDropUseLines", "0"),
                Kv("CompatOriginalSoundCurve", "0"),
                Kv("CompatFullWeaponLower", "0"),
                Kv("CompatWestSpawnsAreSilent", "0"),
                Kv("CompatFloorMove", "0"),
                Kv("CompatEnableWallRunning", "0"),
                Kv("CompatDontLetOthersHearPickups", "0"),
                Kv("CompatAllowInstantRespawn", "0"),
                Kv("CompatDisableStealthMonsters", "0"),
                Kv("CompatAllowSilentBFGTrick", "0"),
                Kv("CompatOriginalWeaponSwitch", "0"),
                Kv("CompatLimitedMovementInTheAir", "0"),
                Kv("CompatMonstersSeeSemiInvisiblePlayers", "0"),
                Kv("CompatPlasmaBumpBug", "0"),
                Kv("CompatAnyBossDeathActivatesMapSpecials", "0"),
                Kv("CompatFrictionPushersPullersAffectMonsters", "0"),
                Kv("CompatCrusherGibsByMorphingNotReplacement", "0"),
                Kv("CompatBlockMonsterLinesIgnoreFriendlyMonsters", "0"),
                Kv("CompatFindNeighboringLightLevelLikeDoom", "0"),
                Kv("CompatUseOldIntermissionScreensMusic", "0"),
                Kv("CompatScrollingSectorsAreAdditive", "0"),
                Kv("CompatSectorSoundsUseOriginalMethod", "0"),
                Kv("CompatNoMonstersDropoffMove", "0"),
                Kv("CompatInstantlyMovingFloorsArentSilent", "0"),
                Kv("CompatClientsSendFullButtonInfo", "0"),
                Kv("CompatOldRandomNumberGenerator", "0"),
                Kv("CompatMonstersCantBePushedOffCliffs", "0"),
                Kv("CompatOldDamageRadiusInfiniteHeight", "0"),
                Kv("CompatMinotaur", "0"),
                Kv("CompatOriginalVelocityCalcForMushroomInDehacked", "0"),
                Kv("CompatSpriteSortOrderInverted", "0"),
                Kv("CompatHitscansOriginalBlockmap", "0"),
                Kv("CompatDrawPolyobjectsOld", "0"),
                Kv("LMSChainsaw", "0"),
                Kv("LMSPistol", "0"),
                Kv("LMSShotgun", "0"),
                Kv("LMSSuperShotgun", "0"),
                Kv("LMSChaingun", "0"),
                Kv("LMSMinigun", "0"),
                Kv("LMSRocketLauncher", "0"),
                Kv("LMSGrenadeLauncher", "0"),
                Kv("LMSPlasmaRifle", "0"),
                Kv("LMSRailgun", "0"),
                Kv("LMSSpectatorsCanTalkToActivePlayers", "0"),
                Kv("LMSSpectatorsCanViewTheGame", "0"),
                Kv("RespawnAutomatically", "0"),
                Kv("DropWeaponOnDeath", "1"),
                Kv("RespawnFarthestAwayFromOthers", "0"),
                Kv("LoseAFragOnDeath", "0"),
                Kv("RespawnWithAShotgun", "0"),
                Kv("NoRespawnProtection", "0"),
                Kv("KeepFragsAfterMapChange", "0"),
                Kv("WeaponsStayAfterPickup", "1"),
                Kv("DoubleAmmo", "1"),
                Kv("DontSpawnHealth", "0"),
                Kv("DontSpawnArmor", "0"),
                Kv("DontSpawnRunes", "0"),
                Kv("ScoreDamageNotKills", "0"),
                Kv("DontSpawnDeathmatchWeapons", "1"),
                Kv("DontSpawnAnyMultiplayerItem", "0"),
                Kv("MonstersAreFast", "0"),
                Kv("MonstersRespawn", "0"),
                Kv("MonstersMustBeKilledToExit", "0"),
                Kv("KillBossMonsters", "1"),
                Kv("RespawnWhereDied", "0"),
                Kv("LoseAllInventory", "1"),
                Kv("LoseArmor", "0"),
                Kv("LoseKeys", "0"),
                Kv("LosePowerups", "0"),
                Kv("LoseWeapons", "1"),
                Kv("LoseAllAmmo", "0"),
                Kv("LoseHalfAmmo", "0"),
                Kv("ShareKeys", "1"),
                Kv("SurvivalNoMapResetOnDeath", "0"),
                Kv("NoSuicide", "1"),
                Kv("NoRespawn", "0"),
                Kv("NoRocketJump", "0"),
                Kv("NoTaunt", "0"),
                Kv("NoItemDrop", "0"),
                Kv("NoUseAutomap", "0"),
                Kv("NoTurnOffTranslucency", "0"),
                Kv("NoUseCrosshairs", "0"),
                Kv("NoUseCustomGLLightingSettings", "0"),
                Kv("NoTargetIdentify", "0"),
                Kv("NoDisplayCoopInfo", "0"),
                Kv("NoUseAutoaim", "0"),
                Kv("NoUseFOV", "0"),
                Kv("NoUseFreelook", "0"),
                Kv("NoUseLandConsoleCommand", "0"),
                Kv("NoMaxBloodScalar", "0"),
                Kv("InfiniteInventory", "0"),
                Kv("InfiniteAmmo", "0"),
                Kv("SlowlyLoseHealthWhenOver100", "0"),
                Kv("CanUseChasecam", "0"),
                Kv("AllowBFGFreeaiming", "1"),
                Kv("DontCheckAmmoWhenSwitchingWeapons", "0"),
                Kv("NoMonsters", "0"),
                Kv("ItemsRespawn", "0"),
                Kv("BarrelsRespawn", "0"),
                Kv("MegaPowerupsRespawn", "0"),
                Kv("ServerPicksTeams", "0"),
                Kv("PlayersCantSwitchTeams", "0"),
                Kv("KeepTeamsAfterAMapChange", "0"),
                Kv("HideAlliesOnTheAutomap", "0"),
                Kv("DontLetPlayersSpyOnAllies", "0"),
                Kv("InstantFlagSkullReturn", "0"),
                Kv("NoUnlagged", "0"),
                Kv("AlwaysApplyLMSSpectatorSettings", "0"),
                Kv("NoMedals", "0"),
                Kv("gameversion", "2"),
                Kv("defaultdmflags", "0"),
                Kv("falling_damage_type", "0"),
                Kv("jump_ability", "1"),
                Kv("crouch_ability", "1"),
                Kv("player_block", "2"),
                Kv("level_exit", "1"),
                Kv("killmonsters_percentage", "100"),
                Kv("force_inactive_players_spectating_mins", "0"),
                Kv("monsters_damage_factor", @"@Variant(\0\0\0\x87?\x80\0\0)")
            };
        }

        private static List<KeyValuePair<string, string>> GetDefaultVotingSettings()
        {
            return new List<KeyValuePair<string, string>>
            {
                Kv("UseThisPage", "0"),
                Kv("WhoCanVote", "0"),
                Kv("MinimumPlayersRequiredToVote", "1"),
                Kv("VoteFloodingProtection", "1"),
                Kv("KickVote", "1"),
                Kv("ChangeMapVote", "1"),
                Kv("MapVote", "1"),
                Kv("TimeLimitVote", "1"),
                Kv("FragLimitVote", "1"),
                Kv("DuelLimitVote", "1"),
                Kv("PointLimitVote", "1"),
                Kv("WinLimitVote", "1"),
                Kv("ForceSpectatorVote", "1")
            };
        }

        private static KeyValuePair<string, string> Kv(string key, string value)
            => new KeyValuePair<string, string>(key, value);
    }
}
