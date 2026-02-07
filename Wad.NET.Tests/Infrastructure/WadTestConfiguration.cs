using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace WAD.NET.Tests.Infrastructure
{
    /// <summary>
    /// Configuration for real-world WAD file testing.
    /// Reads paths from user secrets (preferred) or testsettings.json (fallback).
    /// </summary>
    public static class WadTestConfiguration
    {
        private static readonly Lazy<IConfiguration> _configuration = new(BuildConfiguration);
        private static readonly Lazy<WadPaths> _wadPaths = new(LoadWadPaths);

        public static WadPaths Paths => _wadPaths.Value;

        private static IConfiguration BuildConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(GetBasePath())
                .AddJsonFile("testsettings.json", optional: true, reloadOnChange: false)
                .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true);

            return builder.Build();
        }

        private static string GetBasePath()
        {
            // Try to find testsettings.json relative to the test assembly
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var assemblyDir = Path.GetDirectoryName(assemblyLocation);
            return assemblyDir ?? Directory.GetCurrentDirectory();
        }

        private static WadPaths LoadWadPaths()
        {
            var config = _configuration.Value;
            var paths = new WadPaths();

            // Load IWAD paths
            paths.Doom = config["WadPaths:Doom"] ?? "";
            paths.Doom2 = config["WadPaths:Doom2"] ?? "";
            paths.Plutonia = config["WadPaths:Plutonia"] ?? "";
            paths.Tnt = config["WadPaths:Tnt"] ?? "";
            paths.Heretic = config["WadPaths:Heretic"] ?? "";
            paths.Hexen = config["WadPaths:Hexen"] ?? "";
            paths.Strife = config["WadPaths:Strife"] ?? "";
            paths.FreeDoom1 = config["WadPaths:FreeDoom1"] ?? "";
            paths.FreeDoom2 = config["WadPaths:FreeDoom2"] ?? "";

            // Load PWAD paths
            paths.Eviternity = config["WadPaths:Pwads:Eviternity"] ?? "";
            paths.AncientAliens = config["WadPaths:Pwads:AncientAliens"] ?? "";
            paths.Sunlust = config["WadPaths:Pwads:Sunlust"] ?? "";
            paths.Scythe2 = config["WadPaths:Pwads:Scythe2"] ?? "";

            // Load PK3 paths
            paths.BrutalDoom = config["WadPaths:Pk3s:BrutalDoom"] ?? "";
            paths.GzdoomPk3 = config["WadPaths:Pk3s:Gzdoom_Pk3"] ?? "";
            paths.ZandronumPk3 = config["WadPaths:Pk3s:Zandronum_Pk3"] ?? "";
            paths.CustomMod = config["WadPaths:Pk3s:CustomMod"] ?? "";

            return paths;
        }

        /// <summary>
        /// Checks if a WAD path is configured and the file exists.
        /// </summary>
        public static bool IsAvailable(string? path)
        {
            return !string.IsNullOrWhiteSpace(path) && File.Exists(path);
        }

        /// <summary>
        /// Gets the skip reason for a WAD that isn't available.
        /// </summary>
        public static string GetSkipReason(string wadName, string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return $"{wadName} path not configured in user secrets. " +
                       $"Run: dotnet user-secrets set \"WadPaths:{wadName}\" \"<path>\"";
            }

            if (!File.Exists(path))
            {
                return $"{wadName} file not found at: {path}";
            }

            return string.Empty;
        }
    }

    /// <summary>
    /// Paths to well-known WAD files for testing.
    /// </summary>
    public class WadPaths
    {
        // IWADs
        public string Doom { get; set; } = "";
        public string Doom2 { get; set; } = "";
        public string Plutonia { get; set; } = "";
        public string Tnt { get; set; } = "";
        public string Heretic { get; set; } = "";
        public string Hexen { get; set; } = "";
        public string Strife { get; set; } = "";
        public string FreeDoom1 { get; set; } = "";
        public string FreeDoom2 { get; set; } = "";

        // PWADs
        public string Eviternity { get; set; } = "";
        public string AncientAliens { get; set; } = "";
        public string Sunlust { get; set; } = "";
        public string Scythe2 { get; set; } = "";

        // PK3s
        public string BrutalDoom { get; set; } = "";
        public string GzdoomPk3 { get; set; } = "";
        public string ZandronumPk3 { get; set; } = "";
        public string CustomMod { get; set; } = "";

        /// <summary>
        /// Gets all configured IWAD paths as a dictionary.
        /// </summary>
        public IReadOnlyDictionary<string, string> GetIwads()
        {
            return new Dictionary<string, string>
            {
                ["DOOM"] = Doom,
                ["DOOM2"] = Doom2,
                ["PLUTONIA"] = Plutonia,
                ["TNT"] = Tnt,
                ["HERETIC"] = Heretic,
                ["HEXEN"] = Hexen,
                ["STRIFE"] = Strife,
                ["FREEDOOM1"] = FreeDoom1,
                ["FREEDOOM2"] = FreeDoom2
            };
        }

        /// <summary>
        /// Gets all configured PWAD paths as a dictionary.
        /// </summary>
        public IReadOnlyDictionary<string, string> GetPwads()
        {
            return new Dictionary<string, string>
            {
                ["Eviternity"] = Eviternity,
                ["AncientAliens"] = AncientAliens,
                ["Sunlust"] = Sunlust,
                ["Scythe2"] = Scythe2
            };
        }

        /// <summary>
        /// Gets all configured PK3 paths as a dictionary.
        /// </summary>
        public IReadOnlyDictionary<string, string> GetPk3s()
        {
            return new Dictionary<string, string>
            {
                ["BrutalDoom"] = BrutalDoom,
                ["GzdoomPk3"] = GzdoomPk3,
                ["ZandronumPk3"] = ZandronumPk3,
                ["CustomMod"] = CustomMod
            };
        }
    }
}
