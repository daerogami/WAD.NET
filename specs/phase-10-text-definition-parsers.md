# Phase 10: Text Definition Parsers (MAPINFO, SNDINFO, DEHACKED)

## Overview

This phase replaces the regex-based and string-splitting parsers for MAPINFO, SNDINFO, and DEHACKED with proper lexer/parser implementations following the same Roslyn-style patterns established in Phase 8.

### Current Problems

| Parser | Issues |
|--------|--------|
| **MapInfoParser** | Regex tokenizer, hardcoded keywords, magic loop limits |
| **SndInfoParser** | Split on newlines, multiple regex patterns, array index assumptions |
| **DehackedParser** | Split on `=`, regex for section headers, magic text block lengths |

All three share common anti-patterns:
- Comment removal via regex instead of lexical handling
- String splitting instead of proper tokenization
- No error recovery or diagnostic reporting
- Fragile assumptions about formatting

### Design Principles

Apply the same architecture from Phase 8:
1. **Lexer** produces tokens with trivia (whitespace, comments)
2. **Parser** builds an AST from tokens
3. **Semantic model** provides lookup and validation
4. **Full fidelity** enables round-trip preservation

## Priority: MEDIUM

These parsers are less frequently used than DECORATE/ZScript but are important for mod compatibility detection.

---

## Task 10.1: Shared Lexer Infrastructure

All three formats share common lexical elements. Create a reusable base lexer.

```csharp
namespace WAD.NET.Parsing;

/// <summary>
/// Base lexer with common functionality for text definition formats.
/// </summary>
public abstract class DefinitionLexer
{
    protected readonly string Text;
    protected int Position;
    protected int Start;

    private readonly List<SyntaxTrivia> _leadingTrivia = new();
    private readonly List<SyntaxTrivia> _trailingTrivia = new();

    protected DefinitionLexer(string text)
    {
        Text = text;
    }

    protected char Current => Peek(0);
    protected char Lookahead => Peek(1);

    protected char Peek(int offset)
    {
        var index = Position + offset;
        return index >= Text.Length ? '\0' : Text[index];
    }

    protected void ReadTrivia(bool isLeading, List<SyntaxTrivia> trivia)
    {
        while (true)
        {
            Start = Position;

            switch (Current)
            {
                case '\0':
                    return;

                case '\r':
                case '\n':
                    if (!isLeading) return;
                    ReadEndOfLine(trivia);
                    break;

                case ' ':
                case '\t':
                    ReadWhitespace(trivia);
                    break;

                case '/':
                    if (Lookahead == '/')
                        ReadSingleLineComment(trivia);
                    else if (Lookahead == '*')
                        ReadMultiLineComment(trivia);
                    else
                        return;
                    break;

                case ';':
                    // Some formats use ; for comments
                    if (UsesSemicolonComments)
                        ReadSingleLineComment(trivia);
                    else
                        return;
                    break;

                case '#':
                    // Some formats use # for comments or directives
                    if (UsesHashComments)
                        ReadSingleLineComment(trivia);
                    else
                        return;
                    break;

                default:
                    return;
            }
        }
    }

    protected virtual bool UsesSemicolonComments => false;
    protected virtual bool UsesHashComments => false;

    protected void ReadWhitespace(List<SyntaxTrivia> trivia)
    {
        while (Current == ' ' || Current == '\t')
            Position++;

        trivia.Add(new SyntaxTrivia(
            SyntaxTriviaKind.Whitespace,
            Text[Start..Position],
            new TextSpan(Start, Position - Start)));
    }

    protected void ReadEndOfLine(List<SyntaxTrivia> trivia)
    {
        if (Current == '\r' && Lookahead == '\n')
            Position += 2;
        else
            Position++;

        trivia.Add(new SyntaxTrivia(
            SyntaxTriviaKind.EndOfLine,
            Text[Start..Position],
            new TextSpan(Start, Position - Start)));
    }

    protected void ReadSingleLineComment(List<SyntaxTrivia> trivia)
    {
        while (Current != '\r' && Current != '\n' && Current != '\0')
            Position++;

        trivia.Add(new SyntaxTrivia(
            SyntaxTriviaKind.SingleLineComment,
            Text[Start..Position],
            new TextSpan(Start, Position - Start)));
    }

    protected void ReadMultiLineComment(List<SyntaxTrivia> trivia)
    {
        Position += 2;  // Skip /*
        while (!(Current == '*' && Lookahead == '/') && Current != '\0')
            Position++;
        if (Current != '\0')
            Position += 2;  // Skip */

        trivia.Add(new SyntaxTrivia(
            SyntaxTriviaKind.MultiLineComment,
            Text[Start..Position],
            new TextSpan(Start, Position - Start)));
    }

    protected string ReadQuotedString()
    {
        var quote = Current;  // " or '
        Position++;
        var builder = new StringBuilder();

        while (Current != quote && Current != '\0')
        {
            if (Current == '\\' && (Lookahead == quote || Lookahead == '\\'))
            {
                Position++;
                builder.Append(Current);
            }
            else
            {
                builder.Append(Current);
            }
            Position++;
        }

        if (Current == quote)
            Position++;

        return builder.ToString();
    }

    protected string ReadUnquotedWord()
    {
        while (Current != '\0' && !char.IsWhiteSpace(Current) && !IsOperator(Current))
            Position++;

        return Text[Start..Position];
    }

    protected virtual bool IsOperator(char c) =>
        c == '=' || c == '{' || c == '}' || c == '(' || c == ')' ||
        c == '[' || c == ']' || c == ',' || c == ';';

    protected int ReadInteger()
    {
        bool negative = Current == '-';
        if (negative) Position++;

        Start = Position;
        while (char.IsDigit(Current))
            Position++;

        var value = int.Parse(Text[Start..Position]);
        return negative ? -value : value;
    }
}
```

---

## Task 10.2: MAPINFO Parser

### Reference
- https://zdoom.org/wiki/MAPINFO
- https://zdoom.org/wiki/ZMAPINFO

### Lexer

```csharp
namespace WAD.NET.Parsing.MapInfo;

public enum MapInfoTokenKind
{
    None,
    EndOfFile,

    // Literals
    IntegerLiteral,
    FloatLiteral,
    StringLiteral,
    Identifier,

    // Keywords - Top level
    MapKeyword,
    DefaultMapKeyword,
    AddDefaultMapKeyword,
    GameDefKeyword,
    ClusterKeyword,
    EpisodeKeyword,
    ClearEpisodesKeyword,
    SkillKeyword,
    GameInfoKeyword,

    // Map properties
    LevelNumKeyword,
    NextKeyword,
    SecretNextKeyword,
    Sky1Keyword,
    Sky2Keyword,
    MusicKeyword,
    ClusterKeyword_Prop,  // cluster = X (property, not block)
    ParKeyword,
    SuckTimeKeyword,
    TitlePatchKeyword,

    // Flags
    NoIntermissionKeyword,
    DoubleSkySkyword,
    LightningKeyword,
    Map07SpecialKeyword,
    BaronSpecialKeyword,
    CyberdemonSpecialKeyword,
    SpiderMastermindSpecialKeyword,
    SpecialAction_LowerFloorKeyword,
    SpecialAction_ExitLevelKeyword,

    // Cluster properties
    EnterTextKeyword,
    ExitTextKeyword,
    FlatKeyword,
    PicKeyword,
    HubKeyword,

    // Punctuation
    OpenBrace,
    CloseBrace,
    Equals,
    Comma,

    // Error
    BadToken,
}

public sealed class MapInfoLexer : DefinitionLexer
{
    public MapInfoLexer(string text) : base(text) { }

    protected override bool UsesSemicolonComments => false;

    public MapInfoToken Lex()
    {
        var leadingTrivia = new List<SyntaxTrivia>();
        ReadTrivia(isLeading: true, leadingTrivia);

        Start = Position;
        var kind = ReadToken(out var value);

        var trailingTrivia = new List<SyntaxTrivia>();
        ReadTrivia(isLeading: false, trailingTrivia);

        return new MapInfoToken(
            kind,
            Text[Start..Position],
            new TextSpan(Start, Position - Start),
            leadingTrivia.ToImmutableArray(),
            trailingTrivia.ToImmutableArray(),
            value);
    }

    private MapInfoTokenKind ReadToken(out object? value)
    {
        value = null;

        switch (Current)
        {
            case '\0':
                return MapInfoTokenKind.EndOfFile;

            case '{':
                Position++;
                return MapInfoTokenKind.OpenBrace;

            case '}':
                Position++;
                return MapInfoTokenKind.CloseBrace;

            case '=':
                Position++;
                return MapInfoTokenKind.Equals;

            case ',':
                Position++;
                return MapInfoTokenKind.Comma;

            case '"':
                value = ReadQuotedString();
                return MapInfoTokenKind.StringLiteral;

            case var c when char.IsDigit(c) || (c == '-' && char.IsDigit(Lookahead)):
                return ReadNumber(out value);

            case var c when char.IsLetter(c) || c == '_':
                return ReadIdentifierOrKeyword(out value);

            default:
                Position++;
                return MapInfoTokenKind.BadToken;
        }
    }

    private MapInfoTokenKind ReadNumber(out object? value)
    {
        bool negative = Current == '-';
        if (negative) Position++;

        while (char.IsDigit(Current))
            Position++;

        if (Current == '.' && char.IsDigit(Lookahead))
        {
            Position++;
            while (char.IsDigit(Current))
                Position++;
            value = double.Parse(Text[Start..Position], CultureInfo.InvariantCulture);
            return MapInfoTokenKind.FloatLiteral;
        }

        value = int.Parse(Text[Start..Position]);
        return MapInfoTokenKind.IntegerLiteral;
    }

    private MapInfoTokenKind ReadIdentifierOrKeyword(out object? value)
    {
        while (char.IsLetterOrDigit(Current) || Current == '_')
            Position++;

        var text = Text[Start..Position];
        value = text;

        return text.ToLowerInvariant() switch
        {
            "map" => MapInfoTokenKind.MapKeyword,
            "defaultmap" => MapInfoTokenKind.DefaultMapKeyword,
            "adddefaultmap" => MapInfoTokenKind.AddDefaultMapKeyword,
            "gamedef" => MapInfoTokenKind.GameDefKeyword,
            "cluster" => MapInfoTokenKind.ClusterKeyword,
            "episode" => MapInfoTokenKind.EpisodeKeyword,
            "clearepisodes" => MapInfoTokenKind.ClearEpisodesKeyword,
            "skill" => MapInfoTokenKind.SkillKeyword,
            "gameinfo" => MapInfoTokenKind.GameInfoKeyword,
            "levelnum" => MapInfoTokenKind.LevelNumKeyword,
            "next" => MapInfoTokenKind.NextKeyword,
            "secretnext" => MapInfoTokenKind.SecretNextKeyword,
            "sky1" => MapInfoTokenKind.Sky1Keyword,
            "sky2" => MapInfoTokenKind.Sky2Keyword,
            "music" => MapInfoTokenKind.MusicKeyword,
            "par" => MapInfoTokenKind.ParKeyword,
            "sucktime" => MapInfoTokenKind.SuckTimeKeyword,
            "titlepatch" => MapInfoTokenKind.TitlePatchKeyword,
            "nointermission" => MapInfoTokenKind.NoIntermissionKeyword,
            "doublesky" => MapInfoTokenKind.DoubleSkySkyword,
            "lightning" => MapInfoTokenKind.LightningKeyword,
            "map07special" => MapInfoTokenKind.Map07SpecialKeyword,
            "baronspecial" => MapInfoTokenKind.BaronSpecialKeyword,
            "cyberdemonspecial" => MapInfoTokenKind.CyberdemonSpecialKeyword,
            "spidermastermindspecial" => MapInfoTokenKind.SpiderMastermindSpecialKeyword,
            "entertext" => MapInfoTokenKind.EnterTextKeyword,
            "exittext" => MapInfoTokenKind.ExitTextKeyword,
            "flat" => MapInfoTokenKind.FlatKeyword,
            "pic" => MapInfoTokenKind.PicKeyword,
            "hub" => MapInfoTokenKind.HubKeyword,
            _ => MapInfoTokenKind.Identifier
        };
    }
}
```

### AST Nodes

```csharp
namespace WAD.NET.Parsing.MapInfo;

public abstract class MapInfoSyntax { }

public sealed class MapInfoFileSyntax : MapInfoSyntax
{
    public ImmutableArray<MapInfoDefinitionSyntax> Definitions { get; init; }
}

public abstract class MapInfoDefinitionSyntax : MapInfoSyntax { }

public sealed class MapDefinitionSyntax : MapInfoDefinitionSyntax
{
    public MapInfoToken MapKeyword { get; init; }
    public MapInfoToken MapLump { get; init; }
    public MapInfoToken? LookupKeyword { get; init; }  // "lookup" for LANGUAGE
    public MapInfoToken? NiceName { get; init; }
    public MapInfoToken OpenBrace { get; init; }
    public ImmutableArray<MapPropertySyntax> Properties { get; init; }
    public MapInfoToken CloseBrace { get; init; }

    public string MapName => MapLump.Text;
    public string? DisplayName => NiceName?.Value as string;
}

public abstract class MapPropertySyntax : MapInfoSyntax { }

public sealed class MapPropertyAssignmentSyntax : MapPropertySyntax
{
    public MapInfoToken PropertyName { get; init; }
    public MapInfoToken? EqualsToken { get; init; }
    public ImmutableArray<MapInfoToken> Values { get; init; }
}

public sealed class MapFlagSyntax : MapPropertySyntax
{
    public MapInfoToken FlagKeyword { get; init; }
}

public sealed class ClusterDefinitionSyntax : MapInfoDefinitionSyntax
{
    public MapInfoToken ClusterKeyword { get; init; }
    public MapInfoToken ClusterId { get; init; }
    public MapInfoToken OpenBrace { get; init; }
    public ImmutableArray<ClusterPropertySyntax> Properties { get; init; }
    public MapInfoToken CloseBrace { get; init; }

    public int Id => (int)(ClusterId.Value ?? 0);
}

public abstract class ClusterPropertySyntax : MapInfoSyntax { }

public sealed class ClusterTextPropertySyntax : ClusterPropertySyntax
{
    public MapInfoToken PropertyKeyword { get; init; }  // entertext, exittext
    public MapInfoToken? EqualsToken { get; init; }
    public ImmutableArray<MapInfoToken> TextLines { get; init; }
}

public sealed class EpisodeDefinitionSyntax : MapInfoDefinitionSyntax
{
    public MapInfoToken EpisodeKeyword { get; init; }
    public MapInfoToken MapLump { get; init; }
    public MapInfoToken OpenBrace { get; init; }
    public ImmutableArray<EpisodePropertySyntax> Properties { get; init; }
    public MapInfoToken CloseBrace { get; init; }
}
```

### Parser

```csharp
namespace WAD.NET.Parsing.MapInfo;

public sealed class MapInfoParser
{
    private readonly MapInfoLexer _lexer;
    private MapInfoToken _current;
    private readonly List<Diagnostic> _diagnostics = new();

    public MapInfoParser(string text)
    {
        _lexer = new MapInfoLexer(text);
        _current = _lexer.Lex();
    }

    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics;

    public MapInfoFileSyntax Parse()
    {
        var definitions = ImmutableArray.CreateBuilder<MapInfoDefinitionSyntax>();

        while (_current.Kind != MapInfoTokenKind.EndOfFile)
        {
            var definition = ParseDefinition();
            if (definition != null)
                definitions.Add(definition);
        }

        return new MapInfoFileSyntax { Definitions = definitions.ToImmutable() };
    }

    private MapInfoDefinitionSyntax? ParseDefinition()
    {
        return _current.Kind switch
        {
            MapInfoTokenKind.MapKeyword => ParseMapDefinition(),
            MapInfoTokenKind.DefaultMapKeyword => ParseDefaultMapDefinition(),
            MapInfoTokenKind.ClusterKeyword => ParseClusterDefinition(),
            MapInfoTokenKind.EpisodeKeyword => ParseEpisodeDefinition(),
            MapInfoTokenKind.ClearEpisodesKeyword => ParseClearEpisodes(),
            MapInfoTokenKind.GameInfoKeyword => ParseGameInfo(),
            MapInfoTokenKind.SkillKeyword => ParseSkillDefinition(),
            _ => SkipUnknown()
        };
    }

    private MapDefinitionSyntax ParseMapDefinition()
    {
        var mapKeyword = Match(MapInfoTokenKind.MapKeyword);
        var mapLump = MatchAny(MapInfoTokenKind.Identifier, MapInfoTokenKind.StringLiteral);

        MapInfoToken? lookupKeyword = null;
        MapInfoToken? niceName = null;

        if (_current.Kind == MapInfoTokenKind.Identifier &&
            _current.Text.Equals("lookup", StringComparison.OrdinalIgnoreCase))
        {
            lookupKeyword = NextToken();
            niceName = Match(MapInfoTokenKind.StringLiteral);
        }
        else if (_current.Kind == MapInfoTokenKind.StringLiteral)
        {
            niceName = NextToken();
        }

        var openBrace = Match(MapInfoTokenKind.OpenBrace);
        var properties = ParseMapProperties();
        var closeBrace = Match(MapInfoTokenKind.CloseBrace);

        return new MapDefinitionSyntax
        {
            MapKeyword = mapKeyword,
            MapLump = mapLump,
            LookupKeyword = lookupKeyword,
            NiceName = niceName,
            OpenBrace = openBrace,
            Properties = properties,
            CloseBrace = closeBrace
        };
    }

    private ImmutableArray<MapPropertySyntax> ParseMapProperties()
    {
        var properties = ImmutableArray.CreateBuilder<MapPropertySyntax>();

        while (_current.Kind != MapInfoTokenKind.CloseBrace &&
               _current.Kind != MapInfoTokenKind.EndOfFile)
        {
            if (IsMapFlag(_current.Kind))
            {
                properties.Add(new MapFlagSyntax { FlagKeyword = NextToken() });
            }
            else if (_current.Kind == MapInfoTokenKind.Identifier ||
                     IsMapPropertyKeyword(_current.Kind))
            {
                properties.Add(ParseMapPropertyAssignment());
            }
            else
            {
                NextToken();  // Skip unknown
            }
        }

        return properties.ToImmutable();
    }

    private MapPropertyAssignmentSyntax ParseMapPropertyAssignment()
    {
        var propertyName = NextToken();
        var equalsToken = MatchOptional(MapInfoTokenKind.Equals);

        var values = ImmutableArray.CreateBuilder<MapInfoToken>();

        // Read values until end of line or next property
        while (_current.Kind != MapInfoTokenKind.CloseBrace &&
               _current.Kind != MapInfoTokenKind.EndOfFile &&
               !IsMapPropertyStart(_current))
        {
            if (_current.Kind == MapInfoTokenKind.Comma)
            {
                NextToken();  // Skip comma
                continue;
            }

            values.Add(NextToken());
        }

        return new MapPropertyAssignmentSyntax
        {
            PropertyName = propertyName,
            EqualsToken = equalsToken,
            Values = values.ToImmutable()
        };
    }

    private static bool IsMapFlag(MapInfoTokenKind kind) => kind is
        MapInfoTokenKind.NoIntermissionKeyword or
        MapInfoTokenKind.DoubleSkySkyword or
        MapInfoTokenKind.LightningKeyword or
        MapInfoTokenKind.Map07SpecialKeyword or
        MapInfoTokenKind.BaronSpecialKeyword or
        MapInfoTokenKind.CyberdemonSpecialKeyword or
        MapInfoTokenKind.SpiderMastermindSpecialKeyword;

    private static bool IsMapPropertyKeyword(MapInfoTokenKind kind) => kind is
        MapInfoTokenKind.LevelNumKeyword or
        MapInfoTokenKind.NextKeyword or
        MapInfoTokenKind.SecretNextKeyword or
        MapInfoTokenKind.Sky1Keyword or
        MapInfoTokenKind.Sky2Keyword or
        MapInfoTokenKind.MusicKeyword or
        MapInfoTokenKind.ParKeyword or
        MapInfoTokenKind.TitlePatchKeyword;

    private bool IsMapPropertyStart(MapInfoToken token)
    {
        return IsMapPropertyKeyword(token.Kind) || IsMapFlag(token.Kind) ||
               (token.Kind == MapInfoTokenKind.Identifier && token.LeadingTrivia.Any(t => t.Kind == SyntaxTriviaKind.EndOfLine));
    }

    // ... additional parsing methods for clusters, episodes, etc.
}
```

---

## Task 10.3: SNDINFO Parser

### Reference
- https://zdoom.org/wiki/SNDINFO

### Lexer

```csharp
namespace WAD.NET.Parsing.SndInfo;

public enum SndInfoTokenKind
{
    None,
    EndOfFile,

    // Literals
    StringLiteral,
    Identifier,
    IntegerLiteral,
    FloatLiteral,

    // Directives ($ commands)
    AliasDirective,           // $alias
    RandomDirective,          // $random
    LimitDirective,           // $limit
    SingularDirective,        // $singular
    PitchShiftDirective,      // $pitchshift
    PitchShiftRangeDirective, // $pitchshiftrange
    VolumeDirective,          // $volume
    RolloffDirective,         // $rolloff
    MusicVolumeDirective,     // $musicvolume
    IfDoomDirective,          // $ifdoom
    IfHereticDirective,       // $ifheretic
    IfHexenDirective,         // $ifhexen
    IfStrifeDirective,        // $ifstrife
    EndIfDirective,           // $endif
    ArchivePathDirective,     // $archivepath
    MapDirective,             // $map
    RegisteredDirective,      // $registered
    PlayerReserveDirective,   // $playerreserve
    PlayerSoundDirective,     // $playersound
    PlayerAliasDirective,     // $playeralias
    PlayerCompatDirective,    // $playercompat
    AmbientDirective,         // $ambient
    MusicAliasDirective,      // $musicalias
    EdfOverrideDirective,     // $edfoverride
    AttenDirective,           // $attenuation

    // Punctuation
    OpenBrace,
    CloseBrace,

    // Error
    BadToken,
}

public sealed class SndInfoLexer : DefinitionLexer
{
    public SndInfoLexer(string text) : base(text) { }

    protected override bool UsesSemicolonComments => true;

    public SndInfoToken Lex()
    {
        var leadingTrivia = new List<SyntaxTrivia>();
        ReadTrivia(isLeading: true, leadingTrivia);

        Start = Position;
        var kind = ReadToken(out var value);

        var trailingTrivia = new List<SyntaxTrivia>();
        ReadTrivia(isLeading: false, trailingTrivia);

        return new SndInfoToken(
            kind,
            Text[Start..Position],
            new TextSpan(Start, Position - Start),
            leadingTrivia.ToImmutableArray(),
            trailingTrivia.ToImmutableArray(),
            value);
    }

    private SndInfoTokenKind ReadToken(out object? value)
    {
        value = null;

        switch (Current)
        {
            case '\0':
                return SndInfoTokenKind.EndOfFile;

            case '{':
                Position++;
                return SndInfoTokenKind.OpenBrace;

            case '}':
                Position++;
                return SndInfoTokenKind.CloseBrace;

            case '"':
                value = ReadQuotedString();
                return SndInfoTokenKind.StringLiteral;

            case '$':
                return ReadDirective();

            case var c when char.IsDigit(c) || (c == '-' && char.IsDigit(Lookahead)):
                return ReadNumber(out value);

            case var c when char.IsLetter(c) || c == '_' || c == '/':
                value = ReadSoundName();
                return SndInfoTokenKind.Identifier;

            default:
                Position++;
                return SndInfoTokenKind.BadToken;
        }
    }

    private SndInfoTokenKind ReadDirective()
    {
        Position++;  // Skip $
        Start = Position;

        while (char.IsLetter(Current))
            Position++;

        var directive = Text[Start..Position].ToLowerInvariant();

        return directive switch
        {
            "alias" => SndInfoTokenKind.AliasDirective,
            "random" => SndInfoTokenKind.RandomDirective,
            "limit" => SndInfoTokenKind.LimitDirective,
            "singular" => SndInfoTokenKind.SingularDirective,
            "pitchshift" => SndInfoTokenKind.PitchShiftDirective,
            "pitchshiftrange" => SndInfoTokenKind.PitchShiftRangeDirective,
            "volume" => SndInfoTokenKind.VolumeDirective,
            "rolloff" => SndInfoTokenKind.RolloffDirective,
            "musicvolume" => SndInfoTokenKind.MusicVolumeDirective,
            "ifdoom" => SndInfoTokenKind.IfDoomDirective,
            "ifheretic" => SndInfoTokenKind.IfHereticDirective,
            "ifhexen" => SndInfoTokenKind.IfHexenDirective,
            "ifstrife" => SndInfoTokenKind.IfStrifeDirective,
            "endif" => SndInfoTokenKind.EndIfDirective,
            "archivepath" => SndInfoTokenKind.ArchivePathDirective,
            "map" => SndInfoTokenKind.MapDirective,
            "registered" => SndInfoTokenKind.RegisteredDirective,
            "playerreserve" => SndInfoTokenKind.PlayerReserveDirective,
            "playersound" => SndInfoTokenKind.PlayerSoundDirective,
            "playeralias" => SndInfoTokenKind.PlayerAliasDirective,
            "playercompat" => SndInfoTokenKind.PlayerCompatDirective,
            "ambient" => SndInfoTokenKind.AmbientDirective,
            "musicalias" => SndInfoTokenKind.MusicAliasDirective,
            "edfoverride" => SndInfoTokenKind.EdfOverrideDirective,
            "attenuation" => SndInfoTokenKind.AttenDirective,
            _ => SndInfoTokenKind.BadToken
        };
    }

    private string ReadSoundName()
    {
        // Sound names can contain letters, digits, underscores, and slashes
        while (char.IsLetterOrDigit(Current) || Current == '_' || Current == '/' || Current == '-')
            Position++;

        return Text[Start..Position];
    }

    private SndInfoTokenKind ReadNumber(out object? value)
    {
        bool negative = Current == '-';
        if (negative) Position++;

        while (char.IsDigit(Current))
            Position++;

        if (Current == '.' && char.IsDigit(Lookahead))
        {
            Position++;
            while (char.IsDigit(Current))
                Position++;
            value = double.Parse(Text[Start..Position], CultureInfo.InvariantCulture);
            return SndInfoTokenKind.FloatLiteral;
        }

        value = int.Parse(Text[Start..Position]);
        return SndInfoTokenKind.IntegerLiteral;
    }
}
```

### AST Nodes

```csharp
namespace WAD.NET.Parsing.SndInfo;

public sealed class SndInfoFileSyntax
{
    public ImmutableArray<SndInfoEntrySyntax> Entries { get; init; }
}

public abstract class SndInfoEntrySyntax { }

/// <summary>
/// Sound definition: logical_name lump_name
/// </summary>
public sealed class SoundDefinitionSyntax : SndInfoEntrySyntax
{
    public SndInfoToken LogicalName { get; init; }
    public SndInfoToken LumpName { get; init; }

    public string SoundName => LogicalName.Text;
    public string LumpReference => LumpName.Text;
}

/// <summary>
/// $alias new_name existing_name
/// </summary>
public sealed class AliasDirectiveSyntax : SndInfoEntrySyntax
{
    public SndInfoToken DirectiveToken { get; init; }
    public SndInfoToken NewName { get; init; }
    public SndInfoToken ExistingName { get; init; }
}

/// <summary>
/// $random name { sound1 sound2 ... }
/// </summary>
public sealed class RandomDirectiveSyntax : SndInfoEntrySyntax
{
    public SndInfoToken DirectiveToken { get; init; }
    public SndInfoToken Name { get; init; }
    public SndInfoToken OpenBrace { get; init; }
    public ImmutableArray<SndInfoToken> Sounds { get; init; }
    public SndInfoToken CloseBrace { get; init; }
}

/// <summary>
/// $limit name count
/// </summary>
public sealed class LimitDirectiveSyntax : SndInfoEntrySyntax
{
    public SndInfoToken DirectiveToken { get; init; }
    public SndInfoToken Name { get; init; }
    public SndInfoToken Count { get; init; }

    public int LimitCount => (int)(Count.Value ?? 0);
}

/// <summary>
/// $pitchshiftrange value
/// </summary>
public sealed class PitchShiftRangeDirectiveSyntax : SndInfoEntrySyntax
{
    public SndInfoToken DirectiveToken { get; init; }
    public SndInfoToken Value { get; init; }

    public int Range => (int)(Value.Value ?? 0);
}

/// <summary>
/// $volume name volume
/// </summary>
public sealed class VolumeDirectiveSyntax : SndInfoEntrySyntax
{
    public SndInfoToken DirectiveToken { get; init; }
    public SndInfoToken Name { get; init; }
    public SndInfoToken Volume { get; init; }

    public double VolumeLevel => (double)(Volume.Value ?? 1.0);
}

/// <summary>
/// $ambient index name type [args]
/// </summary>
public sealed class AmbientDirectiveSyntax : SndInfoEntrySyntax
{
    public SndInfoToken DirectiveToken { get; init; }
    public SndInfoToken Index { get; init; }
    public SndInfoToken Name { get; init; }
    public SndInfoToken Type { get; init; }
    public ImmutableArray<SndInfoToken> Arguments { get; init; }
}

/// <summary>
/// $playersound class gender slot lump
/// </summary>
public sealed class PlayerSoundDirectiveSyntax : SndInfoEntrySyntax
{
    public SndInfoToken DirectiveToken { get; init; }
    public SndInfoToken PlayerClass { get; init; }
    public SndInfoToken Gender { get; init; }
    public SndInfoToken Slot { get; init; }
    public SndInfoToken Lump { get; init; }
}

// Conditional compilation
public sealed class ConditionalBlockSyntax : SndInfoEntrySyntax
{
    public SndInfoToken IfDirective { get; init; }
    public ImmutableArray<SndInfoEntrySyntax> Contents { get; init; }
    public SndInfoToken EndIfDirective { get; init; }
}
```

---

## Task 10.4: DEHACKED Parser

### Reference
- https://doomwiki.org/wiki/DeHackEd
- https://doomwiki.org/wiki/MBF21

### Lexer

```csharp
namespace WAD.NET.Parsing.Dehacked;

public enum DehTokenKind
{
    None,
    EndOfFile,

    // Literals
    IntegerLiteral,
    StringLiteral,
    Identifier,

    // Section headers
    PatchFileHeader,     // "Patch File for DeHackEd"
    DoomVersionHeader,   // "Doom version"
    PatchFormatHeader,   // "Patch format"
    ThingHeader,         // "Thing #"
    FrameHeader,         // "Frame #"
    SoundHeader,         // "Sound #"
    SpriteHeader,        // "Sprite #"
    WeaponHeader,        // "Weapon #"
    AmmoHeader,          // "Ammo #"
    PointerHeader,       // "Pointer #"
    TextHeader,          // "Text oldlen newlen"
    StringsHeader,       // "[STRINGS]"
    CodePtrHeader,       // "[CODEPTR]"
    ParsHeader,          // "[PARS]"
    MiscHeader,          // "[MISC]"
    CheatHeader,         // "Cheat"
    BexStringsHeader,    // "[STRINGS]" in BEX
    BexParsHeader,       // "[PARS]" in BEX

    // Property names (handled as identifiers)
    Equals,
    NewLine,

    BadToken,
}

public sealed class DehLexer : DefinitionLexer
{
    public DehLexer(string text) : base(text) { }

    protected override bool UsesHashComments => true;  // BEX format uses #

    public DehToken Lex()
    {
        var leadingTrivia = new List<SyntaxTrivia>();
        ReadTrivia(isLeading: true, leadingTrivia);

        Start = Position;
        var kind = ReadToken(out var value);

        var trailingTrivia = new List<SyntaxTrivia>();
        ReadTrivia(isLeading: false, trailingTrivia);

        return new DehToken(
            kind,
            Text[Start..Position],
            new TextSpan(Start, Position - Start),
            leadingTrivia.ToImmutableArray(),
            trailingTrivia.ToImmutableArray(),
            value);
    }

    private DehTokenKind ReadToken(out object? value)
    {
        value = null;

        switch (Current)
        {
            case '\0':
                return DehTokenKind.EndOfFile;

            case '\r':
            case '\n':
                ReadEndOfLine(new List<SyntaxTrivia>());
                return DehTokenKind.NewLine;

            case '=':
                Position++;
                return DehTokenKind.Equals;

            case '[':
                return ReadBracketSection();

            case var c when char.IsDigit(c) || (c == '-' && char.IsDigit(Lookahead)):
                value = ReadInteger();
                return DehTokenKind.IntegerLiteral;

            case var c when char.IsLetter(c):
                return ReadIdentifierOrHeader(out value);

            default:
                Position++;
                return DehTokenKind.BadToken;
        }
    }

    private DehTokenKind ReadBracketSection()
    {
        Position++;  // Skip [
        Start = Position;

        while (Current != ']' && Current != '\0')
            Position++;

        var section = Text[Start..Position].ToUpperInvariant();

        if (Current == ']')
            Position++;

        return section switch
        {
            "STRINGS" => DehTokenKind.StringsHeader,
            "CODEPTR" => DehTokenKind.CodePtrHeader,
            "PARS" => DehTokenKind.ParsHeader,
            "MISC" => DehTokenKind.MiscHeader,
            _ => DehTokenKind.BadToken
        };
    }

    private DehTokenKind ReadIdentifierOrHeader(out object? value)
    {
        // Read until end of line or = to determine if this is a header or property
        while (Current != '\0' && Current != '\r' && Current != '\n' && Current != '=')
            Position++;

        var text = Text[Start..Position].Trim();
        value = text;

        // Check for section headers
        if (text.StartsWith("Patch File", StringComparison.OrdinalIgnoreCase))
            return DehTokenKind.PatchFileHeader;
        if (text.StartsWith("Doom version", StringComparison.OrdinalIgnoreCase))
            return DehTokenKind.DoomVersionHeader;
        if (text.StartsWith("Patch format", StringComparison.OrdinalIgnoreCase))
            return DehTokenKind.PatchFormatHeader;
        if (text.StartsWith("Thing ", StringComparison.OrdinalIgnoreCase))
            return DehTokenKind.ThingHeader;
        if (text.StartsWith("Frame ", StringComparison.OrdinalIgnoreCase))
            return DehTokenKind.FrameHeader;
        if (text.StartsWith("Sound ", StringComparison.OrdinalIgnoreCase))
            return DehTokenKind.SoundHeader;
        if (text.StartsWith("Sprite ", StringComparison.OrdinalIgnoreCase))
            return DehTokenKind.SpriteHeader;
        if (text.StartsWith("Weapon ", StringComparison.OrdinalIgnoreCase))
            return DehTokenKind.WeaponHeader;
        if (text.StartsWith("Ammo ", StringComparison.OrdinalIgnoreCase))
            return DehTokenKind.AmmoHeader;
        if (text.StartsWith("Pointer ", StringComparison.OrdinalIgnoreCase))
            return DehTokenKind.PointerHeader;
        if (text.StartsWith("Text ", StringComparison.OrdinalIgnoreCase))
            return DehTokenKind.TextHeader;
        if (text.StartsWith("Cheat", StringComparison.OrdinalIgnoreCase))
            return DehTokenKind.CheatHeader;

        return DehTokenKind.Identifier;
    }
}
```

### AST Nodes

```csharp
namespace WAD.NET.Parsing.Dehacked;

public sealed class DehackedFileSyntax
{
    public DehToken? PatchFileHeader { get; init; }
    public int? DoomVersion { get; init; }
    public int? PatchFormat { get; init; }
    public ImmutableArray<DehSectionSyntax> Sections { get; init; }
}

public abstract class DehSectionSyntax { }

public sealed class ThingSectionSyntax : DehSectionSyntax
{
    public DehToken Header { get; init; }
    public int ThingIndex { get; init; }
    public string? ThingName { get; init; }
    public ImmutableArray<DehPropertySyntax> Properties { get; init; }

    public int? GetProperty(string name) =>
        Properties.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.IntValue;
}

public sealed class FrameSectionSyntax : DehSectionSyntax
{
    public DehToken Header { get; init; }
    public int FrameIndex { get; init; }
    public ImmutableArray<DehPropertySyntax> Properties { get; init; }
}

public sealed class WeaponSectionSyntax : DehSectionSyntax
{
    public DehToken Header { get; init; }
    public int WeaponIndex { get; init; }
    public string? WeaponName { get; init; }
    public ImmutableArray<DehPropertySyntax> Properties { get; init; }
}

public sealed class AmmoSectionSyntax : DehSectionSyntax
{
    public DehToken Header { get; init; }
    public int AmmoIndex { get; init; }
    public ImmutableArray<DehPropertySyntax> Properties { get; init; }
}

public sealed class SoundSectionSyntax : DehSectionSyntax
{
    public DehToken Header { get; init; }
    public int SoundIndex { get; init; }
    public ImmutableArray<DehPropertySyntax> Properties { get; init; }
}

public sealed class TextReplacementSyntax : DehSectionSyntax
{
    public DehToken Header { get; init; }
    public int OldLength { get; init; }
    public int NewLength { get; init; }
    public string OldText { get; init; } = "";
    public string NewText { get; init; } = "";
}

public sealed class CodePtrSectionSyntax : DehSectionSyntax
{
    public DehToken Header { get; init; }
    public ImmutableArray<CodePtrAssignmentSyntax> Assignments { get; init; }
}

public sealed class CodePtrAssignmentSyntax
{
    public DehToken FrameToken { get; init; }
    public int FrameIndex { get; init; }
    public DehToken ActionToken { get; init; }
}

public sealed class ParsSectionSyntax : DehSectionSyntax
{
    public DehToken Header { get; init; }
    public ImmutableArray<ParEntrySyntax> Entries { get; init; }
}

public sealed class ParEntrySyntax
{
    public int? Episode { get; init; }  // null for DOOM II
    public int Map { get; init; }
    public int ParTime { get; init; }
}

public sealed class StringsSectionSyntax : DehSectionSyntax
{
    public DehToken Header { get; init; }
    public ImmutableArray<StringReplacementSyntax> Strings { get; init; }
}

public sealed class StringReplacementSyntax
{
    public DehToken KeyToken { get; init; }
    public string Key { get; init; } = "";
    public string Value { get; init; } = "";
}

public sealed class DehPropertySyntax
{
    public DehToken NameToken { get; init; }
    public DehToken EqualsToken { get; init; }
    public DehToken ValueToken { get; init; }

    public string Name => NameToken.Text.Trim();
    public int? IntValue => ValueToken.Value as int?;
}
```

### Parser

```csharp
namespace WAD.NET.Parsing.Dehacked;

public sealed class DehackedParser
{
    private readonly DehLexer _lexer;
    private DehToken _current;
    private readonly List<Diagnostic> _diagnostics = new();
    private readonly string _text;

    public DehackedParser(string text)
    {
        _text = text;
        _lexer = new DehLexer(text);
        _current = _lexer.Lex();
    }

    public DehackedFileSyntax Parse()
    {
        DehToken? patchHeader = null;
        int? doomVersion = null;
        int? patchFormat = null;
        var sections = ImmutableArray.CreateBuilder<DehSectionSyntax>();

        // Parse header
        if (_current.Kind == DehTokenKind.PatchFileHeader)
        {
            patchHeader = NextToken();
            SkipNewLines();
        }

        if (_current.Kind == DehTokenKind.DoomVersionHeader)
        {
            NextToken();
            Match(DehTokenKind.Equals);
            doomVersion = ParseInt();
            SkipNewLines();
        }

        if (_current.Kind == DehTokenKind.PatchFormatHeader)
        {
            NextToken();
            Match(DehTokenKind.Equals);
            patchFormat = ParseInt();
            SkipNewLines();
        }

        // Parse sections
        while (_current.Kind != DehTokenKind.EndOfFile)
        {
            var section = ParseSection();
            if (section != null)
                sections.Add(section);
            SkipNewLines();
        }

        return new DehackedFileSyntax
        {
            PatchFileHeader = patchHeader,
            DoomVersion = doomVersion,
            PatchFormat = patchFormat,
            Sections = sections.ToImmutable()
        };
    }

    private DehSectionSyntax? ParseSection()
    {
        return _current.Kind switch
        {
            DehTokenKind.ThingHeader => ParseThingSection(),
            DehTokenKind.FrameHeader => ParseFrameSection(),
            DehTokenKind.WeaponHeader => ParseWeaponSection(),
            DehTokenKind.AmmoHeader => ParseAmmoSection(),
            DehTokenKind.SoundHeader => ParseSoundSection(),
            DehTokenKind.TextHeader => ParseTextSection(),
            DehTokenKind.CodePtrHeader => ParseCodePtrSection(),
            DehTokenKind.ParsHeader => ParseParsSection(),
            DehTokenKind.StringsHeader => ParseStringsSection(),
            _ => SkipUnknownSection()
        };
    }

    private ThingSectionSyntax ParseThingSection()
    {
        var header = NextToken();
        var (index, name) = ParseSectionIndex(header.Text);
        SkipNewLines();

        var properties = ParseProperties();

        return new ThingSectionSyntax
        {
            Header = header,
            ThingIndex = index,
            ThingName = name,
            Properties = properties
        };
    }

    private (int index, string? name) ParseSectionIndex(string headerText)
    {
        // Format: "Thing 1 (Zombieman)" or "Thing 1"
        var match = Regex.Match(headerText, @"(\w+)\s+(\d+)(?:\s+\(([^)]+)\))?");
        if (match.Success)
        {
            var index = int.Parse(match.Groups[2].Value);
            var name = match.Groups[3].Success ? match.Groups[3].Value : null;
            return (index, name);
        }
        return (0, null);
    }

    private ImmutableArray<DehPropertySyntax> ParseProperties()
    {
        var properties = ImmutableArray.CreateBuilder<DehPropertySyntax>();

        while (_current.Kind == DehTokenKind.Identifier)
        {
            var nameToken = NextToken();
            var equalsToken = Match(DehTokenKind.Equals);
            var valueToken = Match(DehTokenKind.IntegerLiteral);

            properties.Add(new DehPropertySyntax
            {
                NameToken = nameToken,
                EqualsToken = equalsToken,
                ValueToken = valueToken
            });

            SkipNewLines();
        }

        return properties.ToImmutable();
    }

    private TextReplacementSyntax ParseTextSection()
    {
        var header = NextToken();

        // Parse "Text oldlen newlen"
        var match = Regex.Match(header.Text, @"Text\s+(\d+)\s+(\d+)");
        var oldLen = int.Parse(match.Groups[1].Value);
        var newLen = int.Parse(match.Groups[2].Value);

        // Read exact number of characters for old and new text
        SkipNewLines();
        var startPos = GetCurrentPosition();
        var oldText = ReadExactChars(oldLen);
        var newText = ReadExactChars(newLen);

        return new TextReplacementSyntax
        {
            Header = header,
            OldLength = oldLen,
            NewLength = newLen,
            OldText = oldText,
            NewText = newText
        };
    }

    private CodePtrSectionSyntax ParseCodePtrSection()
    {
        var header = NextToken();
        SkipNewLines();

        var assignments = ImmutableArray.CreateBuilder<CodePtrAssignmentSyntax>();

        while (_current.Kind == DehTokenKind.Identifier &&
               _current.Text.StartsWith("Frame", StringComparison.OrdinalIgnoreCase))
        {
            var frameToken = NextToken();
            var frameMatch = Regex.Match(frameToken.Text, @"Frame\s+(\d+)");
            var frameIndex = int.Parse(frameMatch.Groups[1].Value);

            Match(DehTokenKind.Equals);
            var actionToken = Match(DehTokenKind.Identifier);

            assignments.Add(new CodePtrAssignmentSyntax
            {
                FrameToken = frameToken,
                FrameIndex = frameIndex,
                ActionToken = actionToken
            });

            SkipNewLines();
        }

        return new CodePtrSectionSyntax
        {
            Header = header,
            Assignments = assignments.ToImmutable()
        };
    }

    private ParsSectionSyntax ParseParsSection()
    {
        var header = NextToken();
        SkipNewLines();

        var entries = ImmutableArray.CreateBuilder<ParEntrySyntax>();

        while (_current.Kind == DehTokenKind.Identifier &&
               _current.Text.StartsWith("par", StringComparison.OrdinalIgnoreCase))
        {
            var parToken = NextToken();
            var match = Regex.Match(parToken.Text, @"par\s+(\d+)\s+(\d+)(?:\s+(\d+))?");

            if (match.Groups[3].Success)
            {
                // DOOM format: par episode map time
                entries.Add(new ParEntrySyntax
                {
                    Episode = int.Parse(match.Groups[1].Value),
                    Map = int.Parse(match.Groups[2].Value),
                    ParTime = int.Parse(match.Groups[3].Value)
                });
            }
            else
            {
                // DOOM II format: par map time
                entries.Add(new ParEntrySyntax
                {
                    Map = int.Parse(match.Groups[1].Value),
                    ParTime = int.Parse(match.Groups[2].Value)
                });
            }

            SkipNewLines();
        }

        return new ParsSectionSyntax
        {
            Header = header,
            Entries = entries.ToImmutable()
        };
    }

    // ... additional parsing methods
}
```

---

## Acceptance Criteria

1. **MAPINFO Parser**
   - Correctly parses map, cluster, episode, skill, and gameinfo blocks
   - Handles both MAPINFO and ZMAPINFO syntax variations
   - Preserves comments and formatting for round-trip

2. **SNDINFO Parser**
   - Correctly parses all `$` directives
   - Handles conditional compilation (`$ifdoom`, `$endif`)
   - Parses `$random` sound groups correctly

3. **DEHACKED Parser**
   - Correctly parses all section types (Thing, Frame, Weapon, etc.)
   - Handles Text replacements with exact character counts
   - Parses BEX extensions ([CODEPTR], [STRINGS], [PARS])
   - Supports MBF21 extensions

4. **All Parsers**
   - Produce AST that can be traversed with visitors
   - Report diagnostics for syntax errors
   - Support round-trip (parse -> emit produces identical source)

---

## Test Cases

```csharp
// MAPINFO Tests
[Fact]
public void ShouldParseMapDefinition()
{
    var source = @"
map E1M1 ""Hangar""
{
    levelnum = 1
    next = ""E1M2""
    sky1 = ""SKY1""
    music = ""D_E1M1""
    par = 30
}";

    var parser = new MapInfoParser(source);
    var file = parser.Parse();

    Assert.Empty(parser.Diagnostics);
    Assert.Single(file.Definitions);

    var map = Assert.IsType<MapDefinitionSyntax>(file.Definitions[0]);
    Assert.Equal("E1M1", map.MapName);
    Assert.Equal("Hangar", map.DisplayName);
}

// SNDINFO Tests
[Fact]
public void ShouldParseSoundDefinitions()
{
    var source = @"
// Weapon sounds
weapons/pistol     DSPISTOL
weapons/shotgf     DSSHOTGN

$random grunt/death { grunt/death1 grunt/death2 grunt/death3 }
$alias menu/choose weapons/pistol
$limit weapons/pistol 4
";

    var parser = new SndInfoParser(source);
    var file = parser.Parse();

    Assert.Empty(parser.Diagnostics);

    var sounds = file.Entries.OfType<SoundDefinitionSyntax>().ToList();
    Assert.Equal(2, sounds.Count);
    Assert.Equal("weapons/pistol", sounds[0].SoundName);

    var random = file.Entries.OfType<RandomDirectiveSyntax>().Single();
    Assert.Equal("grunt/death", random.Name.Text);
    Assert.Equal(3, random.Sounds.Length);
}

// DEHACKED Tests
[Fact]
public void ShouldParseThingSection()
{
    var source = @"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

Thing 1 (Zombieman)
Hit points = 100
Reaction time = 8
Speed = 12
";

    var parser = new DehackedParser(source);
    var file = parser.Parse();

    Assert.Empty(parser.Diagnostics);
    Assert.Equal(19, file.DoomVersion);
    Assert.Single(file.Sections);

    var thing = Assert.IsType<ThingSectionSyntax>(file.Sections[0]);
    Assert.Equal(1, thing.ThingIndex);
    Assert.Equal("Zombieman", thing.ThingName);
    Assert.Equal(100, thing.GetProperty("Hit points"));
}

[Fact]
public void ShouldParseCodePointers()
{
    var source = @"
[CODEPTR]
Frame 10 = A_Chase
Frame 11 = A_FaceTarget
Frame 12 = A_CustomMissile
";

    var parser = new DehackedParser(source);
    var file = parser.Parse();

    var codeptr = Assert.IsType<CodePtrSectionSyntax>(file.Sections[0]);
    Assert.Equal(3, codeptr.Assignments.Length);
    Assert.Equal("A_Chase", codeptr.Assignments[0].ActionToken.Text);
}
```
