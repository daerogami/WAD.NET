using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace WAD.NET.Detection
{
    /// <summary>
    /// Scans ZScript lumps to extract class information.
    /// </summary>
    public static class ZScriptScanner
    {
        /// <summary>
        /// Scans ZScript content for class definitions.
        /// </summary>
        /// <param name="content">The ZScript content.</param>
        /// <returns>Information about the ZScript content.</returns>
        public static ZScriptInfo Scan(string content)
        {
            var info = new ZScriptInfo();

            if (string.IsNullOrEmpty(content))
                return info;

            // Remove comments
            content = Regex.Replace(content, @"//[^\n]*", "");
            content = Regex.Replace(content, @"/\*[\s\S]*?\*/", "");

            // Detect version declaration
            var versionMatch = Regex.Match(content, @"version\s+""([^""]+)""", RegexOptions.IgnoreCase);
            if (versionMatch.Success)
                info.Version = versionMatch.Groups[1].Value;

            // Find class definitions
            // Format: class <name> [: <parent>] [replaces <replacement>]
            var classPattern = new Regex(
                @"class\s+(\w+)(?:\s*:\s*(\w+))?(?:\s+replaces\s+(\w+))?",
                RegexOptions.IgnoreCase);

            foreach (Match match in classPattern.Matches(content))
            {
                var classDef = new ZScriptClass
                {
                    Name = match.Groups[1].Value,
                    Parent = match.Groups[2].Success ? match.Groups[2].Value : string.Empty,
                    Replaces = match.Groups[3].Success ? match.Groups[3].Value : string.Empty
                };

                // Try to extract class type from parent
                if (classDef.Parent != null)
                {
                    var parentLower = classDef.Parent.ToLowerInvariant();
                    if (parentLower.Contains("actor") || parentLower.Contains("monster") ||
                        parentLower.Contains("weapon") || parentLower.Contains("inventory"))
                    {
                        classDef.IsActor = true;
                    }
                    else if (parentLower.Contains("handler") || parentLower.Contains("eventhandler"))
                    {
                        classDef.IsEventHandler = true;
                    }
                    else if (parentLower.Contains("statusbar") || parentLower.Contains("basesb"))
                    {
                        classDef.IsStatusBar = true;
                    }
                    else if (parentLower.Contains("menu"))
                    {
                        classDef.IsMenu = true;
                    }
                }

                // Check for Default block (indicates actor class)
                int blockStart = match.Index + match.Length;
                int braceStart = content.IndexOf('{', blockStart);
                if (braceStart != -1 && braceStart - blockStart < 100)
                {
                    string? block = ExtractBlock(content, braceStart);
                    if (block != null)
                    {
                        if (Regex.IsMatch(block, @"\bDefault\s*\{", RegexOptions.IgnoreCase))
                            classDef.IsActor = true;

                        // Extract methods
                        var methodPattern = new Regex(@"\b(action|override|virtual|static|clearscope|ui|play)?\s*\w+\s+(\w+)\s*\(", RegexOptions.IgnoreCase);
                        var methods = new List<string>();
                        foreach (Match methodMatch in methodPattern.Matches(block))
                        {
                            string methodName = methodMatch.Groups[2].Value;
                            if (!IsKeyword(methodName))
                                methods.Add(methodName);
                        }
                        classDef.Methods = methods.ToArray();
                    }
                }

                info.Classes.Add(classDef);
            }

            // Find struct definitions
            var structPattern = new Regex(@"struct\s+(\w+)", RegexOptions.IgnoreCase);
            foreach (Match match in structPattern.Matches(content))
            {
                info.Structs.Add(match.Groups[1].Value);
            }

            // Find enum definitions
            var enumPattern = new Regex(@"enum\s+(\w+)", RegexOptions.IgnoreCase);
            foreach (Match match in enumPattern.Matches(content))
            {
                info.Enums.Add(match.Groups[1].Value);
            }

            // Find #include directives
            var includePattern = new Regex(@"#include\s+""([^""]+)""", RegexOptions.IgnoreCase);
            foreach (Match match in includePattern.Matches(content))
            {
                info.Includes.Add(match.Groups[1].Value);
            }

            return info;
        }

        private static string? ExtractBlock(string content, int braceStart)
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

        private static bool IsKeyword(string name)
        {
            var keywords = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
            {
                "if", "else", "for", "while", "do", "switch", "case", "default",
                "return", "break", "continue", "new", "class", "struct", "enum",
                "void", "int", "double", "float", "string", "bool", "name", "state",
                "true", "false", "null", "self", "super", "let", "const", "static"
            };
            return keywords.Contains(name);
        }

        /// <summary>
        /// Checks if content appears to be ZScript.
        /// </summary>
        public static bool IsZScript(string content)
        {
            if (string.IsNullOrEmpty(content))
                return false;

            // ZScript typically has version declaration or specific ZScript syntax
            return Regex.IsMatch(content, @"\bversion\s+""[^""]+""", RegexOptions.IgnoreCase) ||
                   Regex.IsMatch(content, @"\bclass\s+\w+\s*:\s*\w+", RegexOptions.IgnoreCase) ||
                   Regex.IsMatch(content, @"\bDefault\s*\{", RegexOptions.IgnoreCase) ||
                   Regex.IsMatch(content, @"\bclearscope\b|\bui\b\s+\w+\s+\w+\s*\(|\bplay\b\s+\w+\s+\w+\s*\(", RegexOptions.IgnoreCase);
        }
    }

    /// <summary>
    /// Information extracted from ZScript lumps.
    /// </summary>
    public class ZScriptInfo
    {
        /// <summary>
        /// ZScript version requirement.
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Class definitions found.
        /// </summary>
        public List<ZScriptClass> Classes { get; } = new List<ZScriptClass>();

        /// <summary>
        /// Struct definitions found.
        /// </summary>
        public List<string> Structs { get; } = new List<string>();

        /// <summary>
        /// Enum definitions found.
        /// </summary>
        public List<string> Enums { get; } = new List<string>();

        /// <summary>
        /// Include paths referenced.
        /// </summary>
        public List<string> Includes { get; } = new List<string>();

        /// <summary>
        /// Whether any classes were found.
        /// </summary>
        public bool HasClasses => Classes.Count > 0;

        /// <summary>
        /// Gets a list of class names.
        /// </summary>
        public IEnumerable<string> ClassNames
        {
            get
            {
                foreach (var cls in Classes)
                    yield return cls.Name;
            }
        }
    }

    /// <summary>
    /// ZScript class definition.
    /// </summary>
    public class ZScriptClass
    {
        /// <summary>Class name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Parent class name.</summary>
        public string Parent { get; set; } = string.Empty;

        /// <summary>Class this replaces.</summary>
        public string Replaces { get; set; } = string.Empty;

        /// <summary>Whether this class derives from Actor.</summary>
        public bool IsActor { get; set; }

        /// <summary>Whether this is an event handler.</summary>
        public bool IsEventHandler { get; set; }

        /// <summary>Whether this is a status bar class.</summary>
        public bool IsStatusBar { get; set; }

        /// <summary>Whether this is a menu class.</summary>
        public bool IsMenu { get; set; }

        /// <summary>Method names found in the class.</summary>
        public string[] Methods { get; set; } = System.Array.Empty<string>();
    }
}
