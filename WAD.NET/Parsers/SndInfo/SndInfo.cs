using System.Collections.Generic;

namespace WAD.NET.Parsers.SndInfo
{
    /// <summary>
    /// Represents a parsed SNDINFO lump.
    /// </summary>
    public class SndInfo
    {
        /// <summary>
        /// Sound definitions mapping logical names to lump names.
        /// </summary>
        public Dictionary<string, string> Sounds { get; } = new Dictionary<string, string>();

        /// <summary>
        /// Random sound definitions mapping names to arrays of possible sounds.
        /// </summary>
        public Dictionary<string, string[]> RandomSounds { get; } = new Dictionary<string, string[]>();

        /// <summary>
        /// Sound aliases mapping alias names to target sound names.
        /// </summary>
        public Dictionary<string, string> Aliases { get; } = new Dictionary<string, string>();

        /// <summary>
        /// Sound limits (max concurrent plays) for sound names.
        /// </summary>
        public Dictionary<string, int> Limits { get; } = new Dictionary<string, int>();

        /// <summary>
        /// Sound volumes for sound names.
        /// </summary>
        public Dictionary<string, float> Volumes { get; } = new Dictionary<string, float>();

        /// <summary>
        /// Sound rolloff distances (min, max).
        /// </summary>
        public Dictionary<string, (float min, float max)> Rolloffs { get; } = new Dictionary<string, (float, float)>();

        /// <summary>
        /// Pitch shift range (default 0).
        /// </summary>
        public int PitchShiftRange { get; set; } = 0;

        /// <summary>
        /// Sounds marked as singular (only one instance at a time).
        /// </summary>
        public HashSet<string> SingularSounds { get; } = new HashSet<string>();

        /// <summary>
        /// Sounds marked as playing at full volume regardless of distance.
        /// </summary>
        public HashSet<string> FullVolumeSounds { get; } = new HashSet<string>();

        /// <summary>
        /// Sounds marked as not affecting ambient sounds.
        /// </summary>
        public HashSet<string> NoAmbientSounds { get; } = new HashSet<string>();

        /// <summary>
        /// Music aliases.
        /// </summary>
        public Dictionary<string, string> MusicAliases { get; } = new Dictionary<string, string>();

        /// <summary>
        /// Environment definitions.
        /// </summary>
        public List<EnvironmentDefinition> Environments { get; } = new List<EnvironmentDefinition>();

        /// <summary>
        /// Ambient sound definitions.
        /// </summary>
        public Dictionary<int, AmbientSoundDefinition> AmbientSounds { get; } = new Dictionary<int, AmbientSoundDefinition>();

        /// <summary>
        /// Player sound definitions.
        /// </summary>
        public List<PlayerSoundDefinition> PlayerSounds { get; } = new List<PlayerSoundDefinition>();

        /// <summary>
        /// Player compatibility sound assignments.
        /// </summary>
        public List<PlayerCompatDefinition> PlayerCompatibility { get; } = new List<PlayerCompatDefinition>();
    }

    /// <summary>
    /// Reverb environment definition.
    /// </summary>
    public class EnvironmentDefinition
    {
        /// <summary>Environment ID.</summary>
        public int Id { get; set; }

        /// <summary>Environment name.</summary>
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Ambient sound definition.
    /// </summary>
    public class AmbientSoundDefinition
    {
        /// <summary>Ambient sound index.</summary>
        public int Index { get; set; }

        /// <summary>Sound name to play.</summary>
        public string SoundName { get; set; } = string.Empty;

        /// <summary>Ambient type (point, surround, world).</summary>
        public AmbientType Type { get; set; }

        /// <summary>Play mode (continuous, random, periodic).</summary>
        public AmbientPlayMode PlayMode { get; set; }

        /// <summary>Volume (0.0 to 1.0).</summary>
        public float Volume { get; set; } = 1.0f;

        /// <summary>Time interval for periodic/random sounds.</summary>
        public float MinTime { get; set; }

        /// <summary>Maximum time for random interval.</summary>
        public float MaxTime { get; set; }
    }

    /// <summary>
    /// Ambient sound type.
    /// </summary>
    public enum AmbientType
    {
        Point,
        Surround,
        World
    }

    /// <summary>
    /// Ambient sound play mode.
    /// </summary>
    public enum AmbientPlayMode
    {
        Continuous,
        Random,
        Periodic
    }

    /// <summary>
    /// Player sound definition.
    /// </summary>
    public class PlayerSoundDefinition
    {
        /// <summary>Player class name.</summary>
        public string PlayerClass { get; set; } = string.Empty;

        /// <summary>Gender (male, female, other).</summary>
        public string Gender { get; set; } = string.Empty;

        /// <summary>Sound slot name.</summary>
        public string SlotName { get; set; } = string.Empty;

        /// <summary>Sound to play.</summary>
        public string SoundName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Player sound compatibility definition.
    /// </summary>
    public class PlayerCompatDefinition
    {
        /// <summary>Player class name.</summary>
        public string PlayerClass { get; set; } = string.Empty;

        /// <summary>Gender.</summary>
        public string Gender { get; set; } = string.Empty;

        /// <summary>Compatible class.</summary>
        public string CompatibleClass { get; set; } = string.Empty;

        /// <summary>Compatible gender.</summary>
        public string CompatibleGender { get; set; } = string.Empty;
    }
}
