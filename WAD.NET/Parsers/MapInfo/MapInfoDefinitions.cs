using System.Collections.Generic;

namespace WAD.NET.Parsers.MapInfo
{
    /// <summary>
    /// Represents a parsed MAPINFO lump.
    /// </summary>
    public class MapInfo
    {
        /// <summary>
        /// Map definitions keyed by map lump name.
        /// </summary>
        public Dictionary<string, MapDefinition> Maps { get; } = new Dictionary<string, MapDefinition>();

        /// <summary>
        /// Cluster definitions keyed by cluster ID.
        /// </summary>
        public Dictionary<int, ClusterDefinition> Clusters { get; } = new Dictionary<int, ClusterDefinition>();

        /// <summary>
        /// Episode definitions keyed by episode number.
        /// </summary>
        public Dictionary<int, EpisodeDefinition> Episodes { get; } = new Dictionary<int, EpisodeDefinition>();

        /// <summary>
        /// Skill definitions.
        /// </summary>
        public List<SkillDefinition> Skills { get; } = new List<SkillDefinition>();

        /// <summary>
        /// Game-specific settings.
        /// </summary>
        public GameInfo GameInfo { get; set; }

        /// <summary>
        /// Whether episodes should be cleared before adding new ones.
        /// </summary>
        public bool ClearEpisodes { get; set; }
    }

    /// <summary>
    /// Defines a single map.
    /// </summary>
    public class MapDefinition
    {
        /// <summary>Map lump name (e.g., E1M1, MAP01).</summary>
        public string MapLump { get; set; }

        /// <summary>Display name for the map.</summary>
        public string NiceName { get; set; }

        /// <summary>Level number for sorting/display.</summary>
        public int LevelNum { get; set; }

        /// <summary>Titlepic lump for intermission.</summary>
        public string TitlePatch { get; set; }

        /// <summary>Next map after normal exit.</summary>
        public string Next { get; set; }

        /// <summary>Next map after secret exit.</summary>
        public string SecretNext { get; set; }

        /// <summary>Sky texture name.</summary>
        public string Sky1 { get; set; }

        /// <summary>Secondary sky texture name (for double-sky effects).</summary>
        public string Sky2 { get; set; }

        /// <summary>Music lump name.</summary>
        public string Music { get; set; }

        /// <summary>Cluster this map belongs to.</summary>
        public int Cluster { get; set; }

        /// <summary>Par time in seconds.</summary>
        public int Par { get; set; }

        /// <summary>Skip intermission screen.</summary>
        public bool NoIntermission { get; set; }

        /// <summary>Allow monsters to telefrag.</summary>
        public bool AllowMonsterTelefrags { get; set; }

        /// <summary>Allow respawn of items.</summary>
        public bool AllowRespawn { get; set; }

        /// <summary>Map is a secret map.</summary>
        public bool SecretLevel { get; set; }

        /// <summary>Map has lightning effects.</summary>
        public bool Lightning { get; set; }

        /// <summary>Fog color.</summary>
        public string Fade { get; set; }

        /// <summary>Outside fog color.</summary>
        public string OutsideFog { get; set; }

        /// <summary>Gravity multiplier (1.0 = normal).</summary>
        public float? Gravity { get; set; }

        /// <summary>Air control (for jumping/flight).</summary>
        public float? AirControl { get; set; }

        /// <summary>Border flat for status bar.</summary>
        public string BorderTexture { get; set; }

        /// <summary>Entering message.</summary>
        public string EnterPic { get; set; }

        /// <summary>Exiting message.</summary>
        public string ExitPic { get; set; }

        /// <summary>Lookup string for level name.</summary>
        public string LookupName { get; set; }

        /// <summary>SNDSEQ environment.</summary>
        public string SndSeq { get; set; }

        /// <summary>SNDINFO environment.</summary>
        public string SndInfo { get; set; }

        /// <summary>CD track number.</summary>
        public int? CdTrack { get; set; }

        /// <summary>Intermission music.</summary>
        public string InterMusic { get; set; }

        /// <summary>Whether map uses even lighting.</summary>
        public bool EvenLighting { get; set; }

        /// <summary>Whether map uses smooth lighting.</summary>
        public bool SmoothLighting { get; set; }

        /// <summary>Author of the map.</summary>
        public string Author { get; set; }

        /// <summary>Special actions to run on this map.</summary>
        public List<SpecialAction> SpecialActions { get; } = new List<SpecialAction>();

        /// <summary>Map-specific flags.</summary>
        public MapFlags Flags { get; set; }
    }

    /// <summary>
    /// Special action triggered on map events.
    /// </summary>
    public class SpecialAction
    {
        /// <summary>Actor class name.</summary>
        public string ActorClass { get; set; }

        /// <summary>Special type to execute.</summary>
        public int Special { get; set; }

        /// <summary>Arguments for the special.</summary>
        public int[] Args { get; set; } = new int[5];
    }

    /// <summary>
    /// Map-specific flags.
    /// </summary>
    public class MapFlags
    {
        public bool NoSoundClipping { get; set; }
        public bool AllowCrouch { get; set; }
        public bool AllowJump { get; set; }
        public bool NoCrouch { get; set; }
        public bool NoJump { get; set; }
        public bool NoFreeLook { get; set; }
        public bool AllowFreeLook { get; set; }
        public bool NoInfighting { get; set; }
        public bool NormalInfighting { get; set; }
        public bool TotalInfighting { get; set; }
        public bool InfiniteFlightPowerup { get; set; }
        public bool NoAutosequences { get; set; }
        public bool ForceNoSkyStretch { get; set; }
        public bool AllowMonsterRespawn { get; set; }
        public bool NoInventoryBar { get; set; }
        public bool ActivateOwnDeathSpecials { get; set; }
        public bool KillerActivatesDeathSpecials { get; set; }
        public bool MissilesActivateImpactLines { get; set; }
        public bool FilterStarts { get; set; }
        public bool TeamplayOn { get; set; }
        public bool TeamplayOff { get; set; }
        public bool CheckSwitchRange { get; set; }
        public bool NoCheckSwitchRange { get; set; }
        public bool ResetHealth { get; set; }
        public bool ResetInventory { get; set; }
        public bool ResetItems { get; set; }
        public bool FallingDamage { get; set; }
        public bool NoFallingDamage { get; set; }
        public bool OldFallingDamage { get; set; }
        public bool StrifeFallingDamage { get; set; }
        public bool NoAutoSaveHint { get; set; }
    }

    /// <summary>
    /// Cluster definition for grouping maps.
    /// </summary>
    public class ClusterDefinition
    {
        /// <summary>Cluster ID.</summary>
        public int Id { get; set; }

        /// <summary>Flat for text screen background.</summary>
        public string Flat { get; set; }

        /// <summary>Music for cluster screens.</summary>
        public string Music { get; set; }

        /// <summary>Text shown when entering cluster.</summary>
        public string EnterText { get; set; }

        /// <summary>Text shown when exiting cluster.</summary>
        public string ExitText { get; set; }

        /// <summary>Lookup key for enter text.</summary>
        public string EnterTextLookup { get; set; }

        /// <summary>Lookup key for exit text.</summary>
        public string ExitTextLookup { get; set; }

        /// <summary>Background picture.</summary>
        public string Pic { get; set; }

        /// <summary>Whether this is a hub cluster.</summary>
        public bool Hub { get; set; }

        /// <summary>Whether to show enter text as intermission.</summary>
        public bool EnterTextIsLump { get; set; }

        /// <summary>Whether to show exit text as intermission.</summary>
        public bool ExitTextIsLump { get; set; }
    }

    /// <summary>
    /// Episode definition.
    /// </summary>
    public class EpisodeDefinition
    {
        /// <summary>Episode number.</summary>
        public int Number { get; set; }

        /// <summary>Map to start the episode.</summary>
        public string StartMap { get; set; }

        /// <summary>Episode name.</summary>
        public string Name { get; set; }

        /// <summary>Episode key for selection.</summary>
        public char Key { get; set; }

        /// <summary>Lookup key for name.</summary>
        public string LookupName { get; set; }

        /// <summary>Patch graphic for episode selection.</summary>
        public string PicName { get; set; }

        /// <summary>Whether to skip episode in selection.</summary>
        public bool NoSkillMenu { get; set; }

        /// <summary>Whether episode is optional.</summary>
        public bool Optional { get; set; }
    }

    /// <summary>
    /// Skill level definition.
    /// </summary>
    public class SkillDefinition
    {
        /// <summary>Skill name.</summary>
        public string Name { get; set; }

        /// <summary>Ammo multiplier.</summary>
        public float? AmmoFactor { get; set; }

        /// <summary>Damage multiplier for player.</summary>
        public float? DamageFactor { get; set; }

        /// <summary>Respawn time for monsters.</summary>
        public int? RespawnTime { get; set; }

        /// <summary>Aggression multiplier for monsters.</summary>
        public float? AggressiveFactor { get; set; }

        /// <summary>Spawn multiplier for health.</summary>
        public float? HealthFactor { get; set; }

        /// <summary>Key for selection.</summary>
        public char Key { get; set; }

        /// <summary>Is fast monsters enabled.</summary>
        public bool FastMonsters { get; set; }

        /// <summary>Disable cheats on this skill.</summary>
        public bool DisableCheats { get; set; }

        /// <summary>Monsters respawn.</summary>
        public bool MonstersRespawn { get; set; }

        /// <summary>Spawn filter value.</summary>
        public int SpawnFilter { get; set; }

        /// <summary>ACS skill value.</summary>
        public int AcsReturn { get; set; }

        /// <summary>Must confirm new game.</summary>
        public bool MustConfirm { get; set; }

        /// <summary>Confirmation message.</summary>
        public string MustConfirmMessage { get; set; }

        /// <summary>Pic name for skill selection.</summary>
        public string PicName { get; set; }

        /// <summary>Text color for skill.</summary>
        public string TextColor { get; set; }
    }

    /// <summary>
    /// Global game info settings.
    /// </summary>
    public class GameInfo
    {
        /// <summary>Title screen background.</summary>
        public string TitlePage { get; set; }

        /// <summary>Credit screen background.</summary>
        public string CreditPage { get; set; }

        /// <summary>Finale background.</summary>
        public string FinaleFlat { get; set; }

        /// <summary>Finale music.</summary>
        public string FinaleMusic { get; set; }

        /// <summary>Info page.</summary>
        public string InfoPage { get; set; }

        /// <summary>Quit screen messages.</summary>
        public List<string> QuitMessages { get; } = new List<string>();

        /// <summary>Border flat.</summary>
        public string BorderFlat { get; set; }

        /// <summary>Player class names.</summary>
        public List<string> PlayerClasses { get; } = new List<string>();
    }
}
