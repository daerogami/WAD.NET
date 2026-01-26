using System;
using System.Collections.Generic;
using System.Text;

namespace WAD.NET.UDMF
{
    /// <summary>
    /// Lexer for tokenizing UDMF text.
    /// </summary>
    public class UdmfLexer
    {
        private readonly string _text;
        private int _position;
        private int _line = 1;
        private int _column = 1;

        /// <summary>
        /// Creates a new UDMF lexer.
        /// </summary>
        /// <param name="text">The UDMF text to tokenize.</param>
        public UdmfLexer(string text)
        {
            _text = text ?? throw new ArgumentNullException(nameof(text));
        }

        /// <summary>
        /// Tokenizes the input text.
        /// </summary>
        /// <returns>Enumerable of tokens.</returns>
        public IEnumerable<UdmfToken> Tokenize()
        {
            while (_position < _text.Length)
            {
                SkipWhitespaceAndComments();
                if (_position >= _text.Length)
                    break;

                yield return ReadToken();
            }

            yield return new UdmfToken(UdmfTokenType.EndOfFile, string.Empty, _line, _column);
        }

        private void SkipWhitespaceAndComments()
        {
            while (_position < _text.Length)
            {
                char c = _text[_position];

                if (char.IsWhiteSpace(c))
                {
                    if (c == '\n')
                    {
                        _line++;
                        _column = 1;
                    }
                    else
                    {
                        _column++;
                    }
                    _position++;
                }
                else if (_position + 1 < _text.Length && c == '/' && _text[_position + 1] == '/')
                {
                    // Single-line comment
                    _position += 2;
                    _column += 2;
                    while (_position < _text.Length && _text[_position] != '\n')
                    {
                        _position++;
                        _column++;
                    }
                }
                else if (_position + 1 < _text.Length && c == '/' && _text[_position + 1] == '*')
                {
                    // Multi-line comment
                    _position += 2;
                    _column += 2;
                    while (_position + 1 < _text.Length)
                    {
                        if (_text[_position] == '*' && _text[_position + 1] == '/')
                        {
                            _position += 2;
                            _column += 2;
                            break;
                        }
                        if (_text[_position] == '\n')
                        {
                            _line++;
                            _column = 1;
                        }
                        else
                        {
                            _column++;
                        }
                        _position++;
                    }
                }
                else
                {
                    break;
                }
            }
        }

        private UdmfToken ReadToken()
        {
            char c = _text[_position];
            int startLine = _line;
            int startColumn = _column;

            // Single character tokens
            if (c == '{')
            {
                _position++;
                _column++;
                return new UdmfToken(UdmfTokenType.OpenBrace, "{", startLine, startColumn);
            }
            if (c == '}')
            {
                _position++;
                _column++;
                return new UdmfToken(UdmfTokenType.CloseBrace, "}", startLine, startColumn);
            }
            if (c == '=')
            {
                _position++;
                _column++;
                return new UdmfToken(UdmfTokenType.Equals, "=", startLine, startColumn);
            }
            if (c == ';')
            {
                _position++;
                _column++;
                return new UdmfToken(UdmfTokenType.Semicolon, ";", startLine, startColumn);
            }

            // String
            if (c == '"')
            {
                return ReadString();
            }

            // Number (integer or float)
            if (char.IsDigit(c) || c == '-' || c == '+' || c == '.')
            {
                return ReadNumber();
            }

            // Identifier or keyword
            if (char.IsLetter(c) || c == '_')
            {
                return ReadIdentifierOrKeyword();
            }

            throw new FormatException($"Unexpected character '{c}' at line {_line}, column {_column}");
        }

        private UdmfToken ReadString()
        {
            int startLine = _line;
            int startColumn = _column;
            var sb = new StringBuilder();

            // Skip opening quote
            _position++;
            _column++;

            while (_position < _text.Length)
            {
                char c = _text[_position];

                if (c == '"')
                {
                    _position++;
                    _column++;
                    return new UdmfToken(UdmfTokenType.String, sb.ToString(), startLine, startColumn);
                }

                if (c == '\\' && _position + 1 < _text.Length)
                {
                    _position++;
                    _column++;
                    char escaped = _text[_position];
                    switch (escaped)
                    {
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        default: sb.Append(escaped); break;
                    }
                }
                else
                {
                    sb.Append(c);
                }

                if (c == '\n')
                {
                    _line++;
                    _column = 1;
                }
                else
                {
                    _column++;
                }
                _position++;
            }

            throw new FormatException($"Unterminated string starting at line {startLine}, column {startColumn}");
        }

        private UdmfToken ReadNumber()
        {
            int startLine = _line;
            int startColumn = _column;
            var sb = new StringBuilder();
            bool hasDecimal = false;
            bool hasExponent = false;

            // Handle optional sign
            if (_text[_position] == '-' || _text[_position] == '+')
            {
                sb.Append(_text[_position]);
                _position++;
                _column++;
            }

            while (_position < _text.Length)
            {
                char c = _text[_position];

                if (char.IsDigit(c))
                {
                    sb.Append(c);
                    _position++;
                    _column++;
                }
                else if (c == '.' && !hasDecimal && !hasExponent)
                {
                    hasDecimal = true;
                    sb.Append(c);
                    _position++;
                    _column++;
                }
                else if ((c == 'e' || c == 'E') && !hasExponent)
                {
                    hasExponent = true;
                    hasDecimal = true; // e implies float
                    sb.Append(c);
                    _position++;
                    _column++;

                    // Handle optional exponent sign
                    if (_position < _text.Length && (_text[_position] == '-' || _text[_position] == '+'))
                    {
                        sb.Append(_text[_position]);
                        _position++;
                        _column++;
                    }
                }
                else
                {
                    break;
                }
            }

            var value = sb.ToString();
            var type = hasDecimal ? UdmfTokenType.Float : UdmfTokenType.Integer;
            return new UdmfToken(type, value, startLine, startColumn);
        }

        private UdmfToken ReadIdentifierOrKeyword()
        {
            int startLine = _line;
            int startColumn = _column;
            var sb = new StringBuilder();

            while (_position < _text.Length)
            {
                char c = _text[_position];
                if (char.IsLetterOrDigit(c) || c == '_')
                {
                    sb.Append(c);
                    _position++;
                    _column++;
                }
                else
                {
                    break;
                }
            }

            var value = sb.ToString();
            var lowerValue = value.ToLowerInvariant();

            // Check for boolean keywords
            if (lowerValue == "true" || lowerValue == "false")
            {
                return new UdmfToken(UdmfTokenType.Boolean, lowerValue, startLine, startColumn);
            }

            return new UdmfToken(UdmfTokenType.Identifier, value, startLine, startColumn);
        }
    }
}
