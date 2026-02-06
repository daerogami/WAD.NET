using System.Collections.Generic;
using System.Text;

namespace WAD.NET.Parsers
{
    /// <summary>
    /// Tokenizes text definition formats (MAPINFO, SNDINFO) with proper comment
    /// handling that respects quoted strings. Comments inside quotes are preserved.
    /// </summary>
    internal static class DefinitionTokenizer
    {
        /// <summary>
        /// Tokenizes input into a queue of string tokens.
        /// Handles: double-quoted strings, single-quoted strings,
        /// // line comments, /* */ block comments, ; line comments (optional),
        /// punctuation ({, }, =, ,), and unquoted words.
        /// </summary>
        /// <param name="content">The text to tokenize.</param>
        /// <param name="semicolonComments">If true, ; starts a line comment.</param>
        /// <returns>Queue of tokens (quoted strings have quotes stripped).</returns>
        public static Queue<string> Tokenize(string content, bool semicolonComments = false)
        {
            var tokens = new Queue<string>();
            int i = 0;
            int len = content.Length;

            while (i < len)
            {
                char c = content[i];

                // Skip whitespace
                if (char.IsWhiteSpace(c))
                {
                    i++;
                    continue;
                }

                // Line comment: //
                if (c == '/' && i + 1 < len && content[i + 1] == '/')
                {
                    while (i < len && content[i] != '\n')
                        i++;
                    continue;
                }

                // Block comment: /* */
                if (c == '/' && i + 1 < len && content[i + 1] == '*')
                {
                    i += 2;
                    while (i + 1 < len && !(content[i] == '*' && content[i + 1] == '/'))
                        i++;
                    if (i + 1 < len) i += 2; // skip */
                    continue;
                }

                // Semicolon line comment
                if (semicolonComments && c == ';')
                {
                    while (i < len && content[i] != '\n')
                        i++;
                    continue;
                }

                // Double-quoted string
                if (c == '"')
                {
                    i++;
                    var sb = new StringBuilder();
                    while (i < len && content[i] != '"')
                    {
                        if (content[i] == '\\' && i + 1 < len)
                        {
                            sb.Append(content[i + 1]);
                            i += 2;
                        }
                        else
                        {
                            sb.Append(content[i]);
                            i++;
                        }
                    }
                    if (i < len) i++; // skip closing quote
                    tokens.Enqueue(sb.ToString());
                    continue;
                }

                // Single-quoted string
                if (c == '\'')
                {
                    i++;
                    var sb = new StringBuilder();
                    while (i < len && content[i] != '\'')
                    {
                        if (content[i] == '\\' && i + 1 < len)
                        {
                            sb.Append(content[i + 1]);
                            i += 2;
                        }
                        else
                        {
                            sb.Append(content[i]);
                            i++;
                        }
                    }
                    if (i < len) i++; // skip closing quote
                    tokens.Enqueue(sb.ToString());
                    continue;
                }

                // Punctuation: single-char tokens
                if (c == '{' || c == '}' || c == '=' || c == ',')
                {
                    tokens.Enqueue(c.ToString());
                    i++;
                    continue;
                }

                // Unquoted word: collect until whitespace or punctuation
                var word = new StringBuilder();
                while (i < len)
                {
                    char w = content[i];
                    if (char.IsWhiteSpace(w) || w == '{' || w == '}' || w == '=' || w == ',')
                        break;
                    // Stop at comment starts
                    if (w == '/' && i + 1 < len && (content[i + 1] == '/' || content[i + 1] == '*'))
                        break;
                    if (semicolonComments && w == ';')
                        break;
                    word.Append(w);
                    i++;
                }
                if (word.Length > 0)
                    tokens.Enqueue(word.ToString());
            }

            return tokens;
        }
    }
}
