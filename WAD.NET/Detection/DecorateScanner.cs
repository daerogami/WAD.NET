using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace WAD.NET.Detection
{
    /// <summary>
    /// Scans DECORATE lumps to extract actor information.
    /// </summary>
    public static class DecorateScanner
    {
        /// <summary>
        /// Scans DECORATE content for actor definitions.
        /// </summary>
        /// <param name="content">The DECORATE content.</param>
        /// <returns>Information about actors defined in the DECORATE.</returns>
        public static DecorateInfo Scan(string content)
        {
            var info = new DecorateInfo();

            if (string.IsNullOrEmpty(content))
                return info;

            // Remove comments
            content = Regex.Replace(content, @"//[^\n]*", "");
            content = Regex.Replace(content, @"/\*[\s\S]*?\*/", "");

            // Find actor definitions
            // Format: actor <name> [: <parent>] [replaces <replacement>] [<doomednum>]
            var actorPattern = new Regex(
                @"actor\s+(\w+)(?:\s*:\s*(\w+))?(?:\s+replaces\s+(\w+))?(?:\s+(\d+))?",
                RegexOptions.IgnoreCase | RegexOptions.Multiline);

            foreach (Match match in actorPattern.Matches(content))
            {
                var actor = new ActorDefinition
                {
                    Name = match.Groups[1].Value,
                    Parent = match.Groups[2].Success ? match.Groups[2].Value : null,
                    Replaces = match.Groups[3].Success ? match.Groups[3].Value : null,
                    EditorNumber = match.Groups[4].Success
                        ? int.Parse(match.Groups[4].Value)
                        : (int?)null
                };

                // Try to extract some properties from the actor block
                int blockStart = match.Index + match.Length;
                int braceStart = content.IndexOf('{', blockStart);
                if (braceStart != -1 && braceStart - blockStart < 100) // Within reasonable distance
                {
                    string block = ExtractBlock(content, braceStart);
                    if (block != null)
                    {
                        ExtractActorProperties(block, actor);
                    }
                }

                info.Actors.Add(actor);
            }

            // Find #include directives
            var includePattern = new Regex(@"#include\s+""([^""]+)""", RegexOptions.IgnoreCase);
            foreach (Match match in includePattern.Matches(content))
            {
                info.Includes.Add(match.Groups[1].Value);
            }

            return info;
        }

        private static string ExtractBlock(string content, int braceStart)
        {
            int depth = 0;
            int start = braceStart;

            for (int i = braceStart; i < content.Length; i++)
            {
                if (content[i] == '{')
                    depth++;
                else if (content[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                        return content.Substring(start + 1, i - start - 1);
                }
            }

            return null;
        }

        private static void ExtractActorProperties(string block, ActorDefinition actor)
        {
            // Extract health
            var healthMatch = Regex.Match(block, @"\bhealth\s+(\d+)", RegexOptions.IgnoreCase);
            if (healthMatch.Success)
                actor.SpawnHealth = int.Parse(healthMatch.Groups[1].Value);

            // Extract radius
            var radiusMatch = Regex.Match(block, @"\bradius\s+(\d+)", RegexOptions.IgnoreCase);
            if (radiusMatch.Success)
                actor.Radius = int.Parse(radiusMatch.Groups[1].Value);

            // Extract height
            var heightMatch = Regex.Match(block, @"\bheight\s+(\d+)", RegexOptions.IgnoreCase);
            if (heightMatch.Success)
                actor.Height = int.Parse(heightMatch.Groups[1].Value);

            // Extract speed
            var speedMatch = Regex.Match(block, @"\bspeed\s+(\d+)", RegexOptions.IgnoreCase);
            if (speedMatch.Success)
                actor.Speed = int.Parse(speedMatch.Groups[1].Value);

            // Extract damage
            var damageMatch = Regex.Match(block, @"\bdamage\s+(\d+)", RegexOptions.IgnoreCase);
            if (damageMatch.Success)
                actor.Damage = int.Parse(damageMatch.Groups[1].Value);

            // Check for MONSTER flag
            if (Regex.IsMatch(block, @"\+ISMONSTER|\+COUNTKILL|monster\b", RegexOptions.IgnoreCase))
                actor.IsMonster = true;

            // Check for PROJECTILE flag
            if (Regex.IsMatch(block, @"projectile\b|\+MISSILE", RegexOptions.IgnoreCase))
                actor.IsProjectile = true;

            // Extract states
            var statesMatch = Regex.Match(block, @"states\s*\{([\s\S]*?)\}", RegexOptions.IgnoreCase);
            if (statesMatch.Success)
            {
                var stateLabels = Regex.Matches(statesMatch.Groups[1].Value, @"^\s*(\w+):", RegexOptions.Multiline);
                var labels = new List<string>();
                foreach (Match labelMatch in stateLabels)
                    labels.Add(labelMatch.Groups[1].Value);
                actor.StateLabels = labels.ToArray();
            }
        }

        /// <summary>
        /// Checks if content appears to be DECORATE.
        /// </summary>
        public static bool IsDecorate(string content)
        {
            if (string.IsNullOrEmpty(content))
                return false;

            return Regex.IsMatch(content, @"\bactor\s+\w+", RegexOptions.IgnoreCase);
        }
    }

    /// <summary>
    /// Information extracted from DECORATE lumps.
    /// </summary>
    public class DecorateInfo
    {
        /// <summary>
        /// Actor definitions found.
        /// </summary>
        public List<ActorDefinition> Actors { get; } = new List<ActorDefinition>();

        /// <summary>
        /// Include paths referenced.
        /// </summary>
        public List<string> Includes { get; } = new List<string>();

        /// <summary>
        /// Whether any actors were found.
        /// </summary>
        public bool HasActors => Actors.Count > 0;
    }

    /// <summary>
    /// Basic actor definition extracted from DECORATE.
    /// </summary>
    public class ActorDefinition
    {
        /// <summary>Actor class name.</summary>
        public string Name { get; set; }

        /// <summary>Parent class name.</summary>
        public string Parent { get; set; }

        /// <summary>Actor this replaces.</summary>
        public string Replaces { get; set; }

        /// <summary>Editor number (DoomEdNum).</summary>
        public int? EditorNumber { get; set; }

        /// <summary>Spawn health.</summary>
        public int? SpawnHealth { get; set; }

        /// <summary>Collision radius.</summary>
        public int? Radius { get; set; }

        /// <summary>Height.</summary>
        public int? Height { get; set; }

        /// <summary>Movement speed.</summary>
        public int? Speed { get; set; }

        /// <summary>Damage amount.</summary>
        public int? Damage { get; set; }

        /// <summary>Whether this is a monster.</summary>
        public bool IsMonster { get; set; }

        /// <summary>Whether this is a projectile.</summary>
        public bool IsProjectile { get; set; }

        /// <summary>State labels defined.</summary>
        public string[] StateLabels { get; set; }
    }
}
