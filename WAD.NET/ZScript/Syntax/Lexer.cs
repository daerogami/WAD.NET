using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;

namespace WAD.NET.ZScript.Syntax;

/// <summary>
/// Lexer for ZScript and DECORATE source text. Produces <see cref="SyntaxToken"/> values
/// one at a time via successive calls to <see cref="Lex"/>.
/// </summary>
public sealed class Lexer
{
    private readonly string _text;
    private int _position;
    private int _start;
    private SyntaxTokenKind _kind;
    private object? _value;

    private readonly List<SyntaxTrivia> _leadingTrivia = new List<SyntaxTrivia>();
    private readonly List<SyntaxTrivia> _trailingTrivia = new List<SyntaxTrivia>();

    /// <summary>
    /// Creates a new lexer for the given source text.
    /// </summary>
    /// <param name="text">The source text to tokenize.</param>
    public Lexer(string text)
    {
        _text = text ?? throw new ArgumentNullException(nameof(text));
    }

    /// <summary>
    /// The character at the current position, or '\0' if past the end.
    /// </summary>
    private char Current => Peek(0);

    /// <summary>
    /// The character one position ahead, or '\0' if past the end.
    /// </summary>
    private char Lookahead => Peek(1);

    /// <summary>
    /// Returns the character at the given offset from the current position,
    /// or '\0' if the resulting index is out of range.
    /// </summary>
    private char Peek(int offset)
    {
        var index = _position + offset;
        if (index >= _text.Length)
            return '\0';
        return _text[index];
    }

    /// <summary>
    /// Advances the position by one character.
    /// </summary>
    private void Advance() => _position++;

    /// <summary>
    /// Produces the next token from the source text, including leading and trailing trivia.
    /// </summary>
    /// <returns>The next <see cref="SyntaxToken"/>.</returns>
    public SyntaxToken Lex()
    {
        // Read leading trivia (whitespace, newlines, comments before the token)
        _leadingTrivia.Clear();
        ReadTrivia(isLeading: true);
        var leadingTrivia = new SyntaxTriviaList(_leadingTrivia.ToImmutableArray());

        // Read the actual token
        _start = _position;
        _kind = SyntaxTokenKind.None;
        _value = null;
        ReadToken();

        var tokenText = _text[_start.._position];
        var tokenSpan = new TextSpan(_start, _position - _start);

        // Read trailing trivia (whitespace and comments up to but not including newline)
        _trailingTrivia.Clear();
        ReadTrivia(isLeading: false);
        var trailingTrivia = new SyntaxTriviaList(_trailingTrivia.ToImmutableArray());

        return new SyntaxToken(_kind, tokenText, tokenSpan, leadingTrivia, trailingTrivia, _value);
    }

    /// <summary>
    /// Reads trivia (whitespace, comments, newlines) into the appropriate trivia list.
    /// Leading trivia consumes everything before the token. Trailing trivia consumes
    /// whitespace and single-line comments until a newline is encountered.
    /// </summary>
    private void ReadTrivia(bool isLeading)
    {
        var list = isLeading ? _leadingTrivia : _trailingTrivia;

        while (true)
        {
            var triviaStart = _position;
            SyntaxTriviaKind triviaKind;

            switch (Current)
            {
                case '\0':
                    return;

                case '\r':
                case '\n':
                    if (!isLeading)
                        return; // Newline ends trailing trivia
                    ReadEndOfLine();
                    triviaKind = SyntaxTriviaKind.EndOfLine;
                    break;

                case ' ':
                case '\t':
                    ReadWhitespace();
                    triviaKind = SyntaxTriviaKind.Whitespace;
                    break;

                case '/':
                    if (Lookahead == '/')
                    {
                        ReadSingleLineComment();
                        triviaKind = SyntaxTriviaKind.SingleLineComment;
                    }
                    else if (Lookahead == '*')
                    {
                        ReadMultiLineComment();
                        triviaKind = SyntaxTriviaKind.MultiLineComment;
                    }
                    else
                    {
                        return; // Not trivia - it's a slash operator
                    }
                    break;

                case '#':
                    if (isLeading && IsPreprocessorDirective())
                    {
                        ReadPreprocessorDirective();
                        triviaKind = SyntaxTriviaKind.PreprocessorDirective;
                    }
                    else
                    {
                        return; // Not trivia
                    }
                    break;

                default:
                    return; // Not trivia
            }

            var triviaText = _text[triviaStart.._position];
            var triviaSpan = new TextSpan(triviaStart, _position - triviaStart);
            list.Add(new SyntaxTrivia(triviaKind, triviaText, triviaSpan));
        }
    }

    /// <summary>
    /// Reads the main token starting at the current position and sets <see cref="_kind"/> and <see cref="_value"/>.
    /// </summary>
    private void ReadToken()
    {
        switch (Current)
        {
            case '\0':
                _kind = SyntaxTokenKind.EndOfFile;
                break;

            case '+':
                Advance();
                if (Current == '+')
                {
                    Advance();
                    _kind = SyntaxTokenKind.PlusPlus;
                }
                else if (Current == '=')
                {
                    Advance();
                    _kind = SyntaxTokenKind.PlusEquals;
                }
                else
                {
                    _kind = SyntaxTokenKind.Plus;
                }
                break;

            case '-':
                Advance();
                if (Current == '-')
                {
                    Advance();
                    _kind = SyntaxTokenKind.MinusMinus;
                }
                else if (Current == '=')
                {
                    Advance();
                    _kind = SyntaxTokenKind.MinusEquals;
                }
                else if (Current == '>')
                {
                    Advance();
                    _kind = SyntaxTokenKind.Arrow;
                }
                else
                {
                    _kind = SyntaxTokenKind.Minus;
                }
                break;

            case '*':
                Advance();
                if (Current == '=')
                {
                    Advance();
                    _kind = SyntaxTokenKind.AsteriskEquals;
                }
                else
                {
                    _kind = SyntaxTokenKind.Asterisk;
                }
                break;

            case '/':
                Advance();
                if (Current == '=')
                {
                    Advance();
                    _kind = SyntaxTokenKind.SlashEquals;
                }
                else
                {
                    _kind = SyntaxTokenKind.Slash;
                }
                break;

            case '%':
                Advance();
                if (Current == '=')
                {
                    Advance();
                    _kind = SyntaxTokenKind.PercentEquals;
                }
                else
                {
                    _kind = SyntaxTokenKind.Percent;
                }
                break;

            case '&':
                Advance();
                if (Current == '&')
                {
                    Advance();
                    _kind = SyntaxTokenKind.AmpersandAmpersand;
                }
                else if (Current == '=')
                {
                    Advance();
                    _kind = SyntaxTokenKind.AmpersandEquals;
                }
                else
                {
                    _kind = SyntaxTokenKind.Ampersand;
                }
                break;

            case '|':
                Advance();
                if (Current == '|')
                {
                    Advance();
                    _kind = SyntaxTokenKind.PipePipe;
                }
                else if (Current == '=')
                {
                    Advance();
                    _kind = SyntaxTokenKind.PipeEquals;
                }
                else
                {
                    _kind = SyntaxTokenKind.Pipe;
                }
                break;

            case '^':
                Advance();
                if (Current == '=')
                {
                    Advance();
                    _kind = SyntaxTokenKind.CaretEquals;
                }
                else
                {
                    _kind = SyntaxTokenKind.Caret;
                }
                break;

            case '~':
                Advance();
                if (Current == '=' && Lookahead == '=')
                {
                    Advance();
                    Advance();
                    _kind = SyntaxTokenKind.ApproxEquals;
                }
                else
                {
                    _kind = SyntaxTokenKind.Tilde;
                }
                break;

            case '!':
                Advance();
                if (Current == '=')
                {
                    Advance();
                    _kind = SyntaxTokenKind.ExclamationEquals;
                }
                else
                {
                    _kind = SyntaxTokenKind.Exclamation;
                }
                break;

            case '?':
                Advance();
                _kind = SyntaxTokenKind.Question;
                break;

            case '=':
                Advance();
                if (Current == '=')
                {
                    Advance();
                    _kind = SyntaxTokenKind.EqualsEquals;
                }
                else
                {
                    _kind = SyntaxTokenKind.Equals;
                }
                break;

            case '<':
                Advance();
                if (Current == '=')
                {
                    Advance();
                    _kind = SyntaxTokenKind.LessThanEquals;
                }
                else if (Current == '<')
                {
                    Advance();
                    _kind = SyntaxTokenKind.LessThanLessThan;
                }
                else
                {
                    _kind = SyntaxTokenKind.LessThan;
                }
                break;

            case '>':
                Advance();
                if (Current == '=')
                {
                    Advance();
                    _kind = SyntaxTokenKind.GreaterThanEquals;
                }
                else if (Current == '>')
                {
                    Advance();
                    if (Current == '>')
                    {
                        Advance();
                        _kind = SyntaxTokenKind.GreaterThanGreaterThanGreaterThan;
                    }
                    else
                    {
                        _kind = SyntaxTokenKind.GreaterThanGreaterThan;
                    }
                }
                else
                {
                    _kind = SyntaxTokenKind.GreaterThan;
                }
                break;

            case '(':
                Advance();
                _kind = SyntaxTokenKind.OpenParen;
                break;

            case ')':
                Advance();
                _kind = SyntaxTokenKind.CloseParen;
                break;

            case '{':
                Advance();
                _kind = SyntaxTokenKind.OpenBrace;
                break;

            case '}':
                Advance();
                _kind = SyntaxTokenKind.CloseBrace;
                break;

            case '[':
                Advance();
                _kind = SyntaxTokenKind.OpenBracket;
                break;

            case ']':
                Advance();
                _kind = SyntaxTokenKind.CloseBracket;
                break;

            case ';':
                Advance();
                _kind = SyntaxTokenKind.Semicolon;
                break;

            case ':':
                Advance();
                if (Current == ':')
                {
                    Advance();
                    _kind = SyntaxTokenKind.ColonColon;
                }
                else
                {
                    _kind = SyntaxTokenKind.Colon;
                }
                break;

            case ',':
                Advance();
                _kind = SyntaxTokenKind.Comma;
                break;

            case '.':
                if (Lookahead == '.')
                {
                    Advance();
                    Advance();
                    _kind = SyntaxTokenKind.DotDot;
                }
                else if (Lookahead >= '0' && Lookahead <= '9')
                {
                    ReadNumber();
                }
                else
                {
                    Advance();
                    _kind = SyntaxTokenKind.Dot;
                }
                break;

            case '#':
                Advance();
                _kind = SyntaxTokenKind.Hash;
                break;

            case '"':
                ReadStringLiteral();
                break;

            case '\'':
                ReadNameLiteral();
                break;

            default:
                if (Current >= '0' && Current <= '9')
                {
                    ReadNumber();
                }
                else if (IsIdentifierStart(Current))
                {
                    ReadIdentifierOrKeyword();
                }
                else
                {
                    // Unrecognized character
                    Advance();
                    _kind = SyntaxTokenKind.BadToken;
                }
                break;
        }
    }

    /// <summary>
    /// Reads an integer or floating-point number literal, including hex (0x...) and
    /// scientific notation (e.g. 1e10, 2.5E-3).
    /// </summary>
    private void ReadNumber()
    {
        // Check for hex literal: 0x or 0X
        if (Current == '0' && (Lookahead == 'x' || Lookahead == 'X'))
        {
            Advance(); // '0'
            Advance(); // 'x'

            if (!IsHexDigit(Current))
            {
                _kind = SyntaxTokenKind.BadToken;
                return;
            }

            while (IsHexDigit(Current))
                Advance();

            var hexText = _text[(_start + 2).._position];
            if (long.TryParse(hexText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hexValue))
            {
                _value = hexValue;
            }
            _kind = SyntaxTokenKind.IntegerLiteral;
            return;
        }

        bool isFloat = false;

        // Consume leading digits (or handle leading dot)
        if (Current == '.')
        {
            isFloat = true;
            Advance(); // consume '.'
        }
        else
        {
            while (Current >= '0' && Current <= '9')
                Advance();
        }

        // Check for decimal point (only if we haven't already consumed one)
        if (!isFloat && Current == '.' && Lookahead != '.')
        {
            isFloat = true;
            Advance(); // consume '.'
        }

        // Consume fractional digits
        if (isFloat)
        {
            while (Current >= '0' && Current <= '9')
                Advance();
        }

        // Check for scientific notation
        if (Current == 'e' || Current == 'E')
        {
            isFloat = true;
            Advance(); // consume 'e'/'E'
            if (Current == '+' || Current == '-')
                Advance(); // consume sign
            while (Current >= '0' && Current <= '9')
                Advance();
        }

        var numText = _text[_start.._position];

        if (isFloat)
        {
            if (double.TryParse(numText, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatValue))
            {
                _value = floatValue;
            }
            _kind = SyntaxTokenKind.FloatLiteral;
        }
        else
        {
            if (long.TryParse(numText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
            {
                _value = intValue;
            }
            _kind = SyntaxTokenKind.IntegerLiteral;
        }
    }

    /// <summary>
    /// Reads a double-quoted string literal, handling escape sequences (\n, \r, \t, \\, \", \0).
    /// </summary>
    private void ReadStringLiteral()
    {
        Advance(); // consume opening '"'

        var sb = new StringBuilder();

        while (true)
        {
            if (Current == '\0' || Current == '\r' || Current == '\n')
            {
                // Unterminated string
                _kind = SyntaxTokenKind.BadToken;
                _value = sb.ToString();
                return;
            }

            if (Current == '"')
            {
                Advance(); // consume closing '"'
                break;
            }

            if (Current == '\\')
            {
                Advance(); // consume '\'
                switch (Current)
                {
                    case 'n':
                        sb.Append('\n');
                        Advance();
                        break;
                    case 'r':
                        sb.Append('\r');
                        Advance();
                        break;
                    case 't':
                        sb.Append('\t');
                        Advance();
                        break;
                    case '\\':
                        sb.Append('\\');
                        Advance();
                        break;
                    case '"':
                        sb.Append('"');
                        Advance();
                        break;
                    case '0':
                        sb.Append('\0');
                        Advance();
                        break;
                    default:
                        // Unknown escape - keep the character as-is
                        sb.Append(Current);
                        Advance();
                        break;
                }
            }
            else
            {
                sb.Append(Current);
                Advance();
            }
        }

        _kind = SyntaxTokenKind.StringLiteral;
        _value = sb.ToString();
    }

    /// <summary>
    /// Reads a single-quoted name literal (e.g. 'MyName').
    /// </summary>
    private void ReadNameLiteral()
    {
        Advance(); // consume opening '\''

        var sb = new StringBuilder();

        while (true)
        {
            if (Current == '\0' || Current == '\r' || Current == '\n')
            {
                // Unterminated name
                _kind = SyntaxTokenKind.BadToken;
                _value = sb.ToString();
                return;
            }

            if (Current == '\'')
            {
                Advance(); // consume closing '\''
                break;
            }

            sb.Append(Current);
            Advance();
        }

        _kind = SyntaxTokenKind.NameLiteral;
        _value = sb.ToString();
    }

    /// <summary>
    /// Reads an identifier or keyword token. If the identifier text matches a keyword,
    /// the token kind is set to the appropriate keyword kind.
    /// </summary>
    private void ReadIdentifierOrKeyword()
    {
        while (IsIdentifierPart(Current))
            Advance();

        var text = _text[_start.._position];
        _kind = GetKeywordKind(text);
    }

    /// <summary>
    /// Returns the keyword token kind for the given text using case-insensitive matching,
    /// or <see cref="SyntaxTokenKind.Identifier"/> if the text is not a keyword.
    /// </summary>
    private static SyntaxTokenKind GetKeywordKind(string text) => text.ToLowerInvariant() switch
    {
        "class" => SyntaxTokenKind.ClassKeyword,
        "struct" => SyntaxTokenKind.StructKeyword,
        "enum" => SyntaxTokenKind.EnumKeyword,
        "const" => SyntaxTokenKind.ConstKeyword,
        "static" => SyntaxTokenKind.StaticKeyword,
        "private" => SyntaxTokenKind.PrivateKeyword,
        "protected" => SyntaxTokenKind.ProtectedKeyword,
        "virtual" => SyntaxTokenKind.VirtualKeyword,
        "override" => SyntaxTokenKind.OverrideKeyword,
        "final" => SyntaxTokenKind.FinalKeyword,
        "native" => SyntaxTokenKind.NativeKeyword,
        "actor" => SyntaxTokenKind.ActorKeyword,
        "replaces" => SyntaxTokenKind.ReplacesKeyword,
        "version" => SyntaxTokenKind.VersionKeyword,
        "extend" => SyntaxTokenKind.ExtendKeyword,
        "mixin" => SyntaxTokenKind.MixinKeyword,
        "abstract" => SyntaxTokenKind.AbstractKeyword,
        "deprecated" => SyntaxTokenKind.DeprecatedKeyword,
        "readonly" => SyntaxTokenKind.ReadOnlyKeyword,
        "default" => SyntaxTokenKind.DefaultKeyword,
        "states" => SyntaxTokenKind.StatesKeyword,
        "if" => SyntaxTokenKind.IfKeyword,
        "else" => SyntaxTokenKind.ElseKeyword,
        "while" => SyntaxTokenKind.WhileKeyword,
        "do" => SyntaxTokenKind.DoKeyword,
        "for" => SyntaxTokenKind.ForKeyword,
        "foreach" => SyntaxTokenKind.ForEachKeyword,
        "switch" => SyntaxTokenKind.SwitchKeyword,
        "case" => SyntaxTokenKind.CaseKeyword,
        "break" => SyntaxTokenKind.BreakKeyword,
        "continue" => SyntaxTokenKind.ContinueKeyword,
        "return" => SyntaxTokenKind.ReturnKeyword,
        "goto" => SyntaxTokenKind.GotoKeyword,
        "void" => SyntaxTokenKind.VoidKeyword,
        "int" => SyntaxTokenKind.IntKeyword,
        "uint" => SyntaxTokenKind.UIntKeyword,
        "float" => SyntaxTokenKind.FloatKeyword,
        "double" => SyntaxTokenKind.DoubleKeyword,
        "bool" => SyntaxTokenKind.BoolKeyword,
        "string" => SyntaxTokenKind.StringKeyword,
        "vector2" => SyntaxTokenKind.VectorKeyword,
        "vector3" => SyntaxTokenKind.VectorKeyword,
        "name" => SyntaxTokenKind.NameKeyword,
        "state" => SyntaxTokenKind.StateKeyword,
        "color" => SyntaxTokenKind.ColorKeyword,
        "sound" => SyntaxTokenKind.SoundKeyword,
        "array" => SyntaxTokenKind.ArrayKeyword,
        "map" => SyntaxTokenKind.MapKeyword,
        "true" => SyntaxTokenKind.TrueKeyword,
        "false" => SyntaxTokenKind.FalseKeyword,
        "null" => SyntaxTokenKind.NullKeyword,
        "stop" => SyntaxTokenKind.StopKeyword,
        "wait" => SyntaxTokenKind.WaitKeyword,
        "fail" => SyntaxTokenKind.FailKeyword,
        "loop" => SyntaxTokenKind.LoopKeyword,
        "let" => SyntaxTokenKind.LetKeyword,
        "out" => SyntaxTokenKind.OutKeyword,
        "in" => SyntaxTokenKind.InKeyword,
        _ => SyntaxTokenKind.Identifier,
    };

    /// <summary>
    /// Reads whitespace characters (spaces and tabs).
    /// </summary>
    private void ReadWhitespace()
    {
        while (Current == ' ' || Current == '\t')
            Advance();
    }

    /// <summary>
    /// Reads an end-of-line sequence (\r\n, \r, or \n).
    /// </summary>
    private void ReadEndOfLine()
    {
        if (Current == '\r')
        {
            Advance();
            if (Current == '\n')
                Advance();
        }
        else if (Current == '\n')
        {
            Advance();
        }
    }

    /// <summary>
    /// Reads a single-line comment (from // to end of line).
    /// </summary>
    private void ReadSingleLineComment()
    {
        // Consume '//'
        Advance();
        Advance();

        while (Current != '\0' && Current != '\r' && Current != '\n')
            Advance();
    }

    /// <summary>
    /// Reads a multi-line comment (from /* to */).
    /// </summary>
    private void ReadMultiLineComment()
    {
        // Consume '/*'
        Advance();
        Advance();

        while (true)
        {
            if (Current == '\0')
                break; // Unterminated comment

            if (Current == '*' && Lookahead == '/')
            {
                Advance(); // '*'
                Advance(); // '/'
                break;
            }

            Advance();
        }
    }

    /// <summary>
    /// Determines whether the current position starts a preprocessor directive
    /// (# followed by a known directive keyword such as include, define, etc.).
    /// </summary>
    private bool IsPreprocessorDirective()
    {
        if (Current != '#')
            return false;

        // Peek ahead past optional whitespace to see if there's a known directive word
        var i = _position + 1;
        while (i < _text.Length && (_text[i] == ' ' || _text[i] == '\t'))
            i++;

        // Read the directive word
        var wordStart = i;
        while (i < _text.Length && IsIdentifierPart(_text[i]))
            i++;

        if (wordStart == i)
            return false;

        var word = _text[wordStart..i].ToLowerInvariant();
        return word == "include" || word == "define" || word == "ifdef"
            || word == "ifndef" || word == "else" || word == "endif"
            || word == "region" || word == "endregion" || word == "pragma";
    }

    /// <summary>
    /// Reads a preprocessor directive from '#' to end of line.
    /// </summary>
    private void ReadPreprocessorDirective()
    {
        // Consume everything up to end of line
        while (Current != '\0' && Current != '\r' && Current != '\n')
            Advance();
    }

    /// <summary>
    /// Returns true if the character can start an identifier (letter or underscore).
    /// </summary>
    private static bool IsIdentifierStart(char c) =>
        (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_';

    /// <summary>
    /// Returns true if the character can continue an identifier (letter, digit, or underscore).
    /// </summary>
    private static bool IsIdentifierPart(char c) =>
        (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_';

    /// <summary>
    /// Returns true if the character is a valid hexadecimal digit.
    /// </summary>
    private static bool IsHexDigit(char c) =>
        (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
}
