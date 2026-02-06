using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using WAD.NET.ZScript.Diagnostics;

namespace WAD.NET.ZScript.Syntax;

/// <summary>
/// Recursive descent parser for ZScript and DECORATE source text.
/// Consumes tokens produced by <see cref="Lexer"/> and builds a syntax tree.
/// </summary>
public sealed class Parser
{
    private readonly ImmutableArray<SyntaxToken> _tokens;
    private readonly ScriptLanguage _language;
    private readonly List<Diagnostic> _diagnostics = new List<Diagnostic>();
    private int _position;

    /// <summary>
    /// Creates a new parser for the given source text.
    /// </summary>
    /// <param name="text">The source text to parse.</param>
    /// <param name="language">The script language (ZScript or DECORATE).</param>
    public Parser(string text, ScriptLanguage language = ScriptLanguage.ZScript)
    {
        var lexer = new Lexer(text);
        var tokens = ImmutableArray.CreateBuilder<SyntaxToken>();
        SyntaxToken token;
        do
        {
            token = lexer.Lex();
            tokens.Add(token);
        } while (token.Kind != SyntaxTokenKind.EndOfFile);

        _tokens = tokens.ToImmutable();
        _language = language;
    }

    /// <summary>
    /// The diagnostics reported during parsing.
    /// </summary>
    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics;

    // ------------------------------------------------------------------
    // Token access helpers
    // ------------------------------------------------------------------

    private SyntaxToken Current => Peek(0);

    private SyntaxToken Peek(int offset)
    {
        var index = _position + offset;
        if (index >= _tokens.Length)
            return _tokens[_tokens.Length - 1]; // EOF
        return _tokens[index];
    }

    private SyntaxToken NextToken()
    {
        var current = Current;
        if (_position < _tokens.Length - 1)
            _position++;
        return current;
    }

    private SyntaxToken Match(SyntaxTokenKind kind)
    {
        if (Current.Kind == kind)
            return NextToken();

        _diagnostics.Add(new Diagnostic(
            DiagnosticSeverity.Error,
            $"Expected '{kind}', got '{Current.Kind}'",
            Current.Span));

        // Return a missing token at the current position with the current token's leading trivia
        return new SyntaxToken(kind, string.Empty, Current.Span,
            isMissing: true);
    }

    private SyntaxToken? MatchOptional(SyntaxTokenKind kind)
    {
        if (Current.Kind == kind)
            return NextToken();
        return null;
    }

    private bool IsAtEnd => Current.Kind == SyntaxTokenKind.EndOfFile;

    // ------------------------------------------------------------------
    // Top-level: CompilationUnit
    // ------------------------------------------------------------------

    /// <summary>
    /// Parses a compilation unit (the top-level construct in a ZScript or DECORATE file).
    /// </summary>
    public CompilationUnitSyntax ParseCompilationUnit()
    {
        var versionDirective = ParseVersionDirective();
        var includes = ParseIncludes();
        var members = ParseMembers();
        var eof = Match(SyntaxTokenKind.EndOfFile);

        return new CompilationUnitSyntax(versionDirective, includes, members, eof);
    }

    private VersionDirectiveSyntax? ParseVersionDirective()
    {
        if (Current.Kind != SyntaxTokenKind.VersionKeyword)
            return null;

        var keyword = NextToken();
        var versionString = Match(SyntaxTokenKind.StringLiteral);
        return new VersionDirectiveSyntax(keyword, versionString);
    }

    private ImmutableArray<IncludeDirectiveSyntax> ParseIncludes()
    {
        var includes = ImmutableArray.CreateBuilder<IncludeDirectiveSyntax>();

        // Try token-based approach: # include "path"
        while (Current.Kind == SyntaxTokenKind.Hash &&
               Peek(1).Kind == SyntaxTokenKind.Identifier &&
               string.Equals(Peek(1).Text, "include", StringComparison.OrdinalIgnoreCase))
        {
            var hash = NextToken();
            var includeKeyword = NextToken();
            var path = Match(SyntaxTokenKind.StringLiteral);
            includes.Add(new IncludeDirectiveSyntax(hash, includeKeyword, path));
        }

        // Also check leading trivia of the current token for preprocessor #include directives
        // (the lexer treats #include lines as trivia, so we extract them here)
        if (includes.Count == 0)
        {
            var triviaList = Current.LeadingTrivia;
            for (int i = 0; i < triviaList.Count; i++)
            {
                var trivia = triviaList[i];
                if (trivia.Kind == SyntaxTriviaKind.PreprocessorDirective)
                {
                    var text = trivia.Text.Trim();
                    if (text.StartsWith("#") &&
                        text.IndexOf("include", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        // Extract the path from the directive
                        var quoteStart = text.IndexOf('"');
                        var quoteEnd = text.LastIndexOf('"');
                        if (quoteStart >= 0 && quoteEnd > quoteStart)
                        {
                            var span = trivia.Span;
                            var hashToken = new SyntaxToken(SyntaxTokenKind.Hash, "#",
                                new TextSpan(span.Start, 1));
                            // Find "include" within the text
                            var includeIdx = text.IndexOf("include", StringComparison.OrdinalIgnoreCase);
                            var includeKw = new SyntaxToken(SyntaxTokenKind.Identifier, text.Substring(includeIdx, 7),
                                new TextSpan(span.Start + includeIdx, 7));
                            var pathText = text.Substring(quoteStart, quoteEnd - quoteStart + 1);
                            var pathToken = new SyntaxToken(SyntaxTokenKind.StringLiteral, pathText,
                                new TextSpan(span.Start + quoteStart, pathText.Length));
                            includes.Add(new IncludeDirectiveSyntax(hashToken, includeKw, pathToken));
                        }
                    }
                }
            }
        }

        return includes.ToImmutable();
    }

    private ImmutableArray<MemberDeclarationSyntax> ParseMembers()
    {
        var members = ImmutableArray.CreateBuilder<MemberDeclarationSyntax>();

        while (!IsAtEnd)
        {
            var startPos = _position;
            var member = ParseMemberDeclaration();
            if (member != null)
            {
                members.Add(member);
            }
            else
            {
                // Error recovery: skip one token to avoid infinite loop
                if (_position == startPos)
                {
                    _diagnostics.Add(new Diagnostic(
                        DiagnosticSeverity.Error,
                        $"Unexpected token '{Current.Kind}'",
                        Current.Span));
                    NextToken();
                }
            }
        }

        return members.ToImmutable();
    }

    private MemberDeclarationSyntax? ParseMemberDeclaration()
    {
        if (_language == ScriptLanguage.Decorate && Current.Kind == SyntaxTokenKind.ActorKeyword)
        {
            return ParseActorDeclaration();
        }

        var modifiers = ParseModifiers();

        switch (Current.Kind)
        {
            case SyntaxTokenKind.ClassKeyword:
                return ParseClassDeclaration(modifiers);
            case SyntaxTokenKind.StructKeyword:
                return ParseStructDeclaration(modifiers);
            case SyntaxTokenKind.EnumKeyword:
                return ParseEnumDeclaration(modifiers);
            case SyntaxTokenKind.ConstKeyword:
                return ParseConstDeclaration(modifiers);
            case SyntaxTokenKind.ActorKeyword:
                return ParseActorDeclaration();
            default:
                // Could be a field or method if modifiers were consumed, or if we see a type-like token
                if (modifiers.Length > 0 || IsTypeStart())
                {
                    return ParseMethodOrField(modifiers);
                }
                return null;
        }
    }

    // ------------------------------------------------------------------
    // Modifiers
    // ------------------------------------------------------------------

    private ImmutableArray<SyntaxToken> ParseModifiers()
    {
        var modifiers = ImmutableArray.CreateBuilder<SyntaxToken>();

        while (true)
        {
            switch (Current.Kind)
            {
                case SyntaxTokenKind.AbstractKeyword:
                case SyntaxTokenKind.VirtualKeyword:
                case SyntaxTokenKind.OverrideKeyword:
                case SyntaxTokenKind.FinalKeyword:
                case SyntaxTokenKind.NativeKeyword:
                case SyntaxTokenKind.PrivateKeyword:
                case SyntaxTokenKind.ProtectedKeyword:
                case SyntaxTokenKind.StaticKeyword:
                case SyntaxTokenKind.ReadOnlyKeyword:
                case SyntaxTokenKind.DeprecatedKeyword:
                    modifiers.Add(NextToken());
                    // Handle deprecated("message") with optional string arg
                    if (modifiers[modifiers.Count - 1].Kind == SyntaxTokenKind.DeprecatedKeyword &&
                        Current.Kind == SyntaxTokenKind.OpenParen)
                    {
                        NextToken(); // (
                        if (Current.Kind == SyntaxTokenKind.StringLiteral)
                            NextToken(); // "message"
                        MatchOptional(SyntaxTokenKind.CloseParen);
                    }
                    break;
                default:
                    // Also treat "action" as a modifier (parsed as an identifier)
                    if (Current.Kind == SyntaxTokenKind.Identifier &&
                        string.Equals(Current.Text, "action", StringComparison.OrdinalIgnoreCase))
                    {
                        modifiers.Add(NextToken());
                        break;
                    }
                    return modifiers.ToImmutable();
            }
        }
    }

    // ------------------------------------------------------------------
    // Declarations
    // ------------------------------------------------------------------

    private ClassDeclarationSyntax ParseClassDeclaration(ImmutableArray<SyntaxToken> modifiers)
    {
        var classKeyword = Match(SyntaxTokenKind.ClassKeyword);
        var identifier = Match(SyntaxTokenKind.Identifier);

        var baseList = ParseBaseList();

        SyntaxToken? replacesKeyword = null;
        SyntaxToken? replacesIdentifier = null;
        if (Current.Kind == SyntaxTokenKind.ReplacesKeyword)
        {
            replacesKeyword = NextToken();
            replacesIdentifier = Match(SyntaxTokenKind.Identifier);
        }

        SyntaxToken? editorNumber = null;
        if (Current.Kind == SyntaxTokenKind.IntegerLiteral)
        {
            editorNumber = NextToken();
        }

        var openBrace = Match(SyntaxTokenKind.OpenBrace);
        var members = ParseClassMembers();
        var closeBrace = Match(SyntaxTokenKind.CloseBrace);

        return new ClassDeclarationSyntax(
            modifiers, classKeyword, identifier, baseList,
            replacesKeyword, replacesIdentifier, editorNumber,
            openBrace, members, closeBrace);
    }

    private ActorDeclarationSyntax ParseActorDeclaration()
    {
        var actorKeyword = Match(SyntaxTokenKind.ActorKeyword);
        var identifier = Match(SyntaxTokenKind.Identifier);

        SyntaxToken? colonToken = null;
        SyntaxToken? baseIdentifier = null;
        if (Current.Kind == SyntaxTokenKind.Colon)
        {
            colonToken = NextToken();
            baseIdentifier = MatchIdentifierOrKeywordAsIdentifier();
        }

        SyntaxToken? replacesKeyword = null;
        SyntaxToken? replacesIdentifier = null;
        if (Current.Kind == SyntaxTokenKind.ReplacesKeyword)
        {
            replacesKeyword = NextToken();
            replacesIdentifier = MatchIdentifierOrKeywordAsIdentifier();
        }

        SyntaxToken? editorNumber = null;
        if (Current.Kind == SyntaxTokenKind.IntegerLiteral)
        {
            editorNumber = NextToken();
        }

        var openBrace = Match(SyntaxTokenKind.OpenBrace);
        var body = ParseActorBody();
        var closeBrace = Match(SyntaxTokenKind.CloseBrace);

        return new ActorDeclarationSyntax(
            actorKeyword, identifier, colonToken, baseIdentifier,
            replacesKeyword, replacesIdentifier, editorNumber,
            openBrace, body, closeBrace);
    }

    private StructDeclarationSyntax ParseStructDeclaration(ImmutableArray<SyntaxToken> modifiers)
    {
        var structKeyword = Match(SyntaxTokenKind.StructKeyword);
        var identifier = Match(SyntaxTokenKind.Identifier);
        var openBrace = Match(SyntaxTokenKind.OpenBrace);
        var members = ParseClassMembers();
        var closeBrace = Match(SyntaxTokenKind.CloseBrace);

        return new StructDeclarationSyntax(
            modifiers, structKeyword, identifier,
            openBrace, members, closeBrace);
    }

    private EnumDeclarationSyntax ParseEnumDeclaration(ImmutableArray<SyntaxToken> modifiers)
    {
        var enumKeyword = Match(SyntaxTokenKind.EnumKeyword);
        var identifier = Match(SyntaxTokenKind.Identifier);

        // Optional base type (: int, : uint8, etc.)
        if (Current.Kind == SyntaxTokenKind.Colon)
        {
            NextToken(); // consume :
            NextToken(); // consume the type token
        }

        var openBrace = Match(SyntaxTokenKind.OpenBrace);

        var members = ImmutableArray.CreateBuilder<SyntaxToken>();
        while (Current.Kind != SyntaxTokenKind.CloseBrace && !IsAtEnd)
        {
            members.Add(NextToken());
        }

        var closeBrace = Match(SyntaxTokenKind.CloseBrace);

        // Optional trailing semicolon
        MatchOptional(SyntaxTokenKind.Semicolon);

        return new EnumDeclarationSyntax(
            enumKeyword, identifier, openBrace,
            members.ToImmutable(), closeBrace);
    }

    private ConstDeclarationSyntax ParseConstDeclaration(ImmutableArray<SyntaxToken> modifiers)
    {
        var constKeyword = Match(SyntaxTokenKind.ConstKeyword);

        TypeSyntax type;
        if (IsTypeStart())
        {
            type = ParseType();
        }
        else
        {
            // Implicit int type for DECORATE-style "const FOO = value;"
            type = new PredefinedTypeSyntax(new SyntaxToken(
                SyntaxTokenKind.IntKeyword, string.Empty, Current.Span, isMissing: true));
        }

        var identifier = Match(SyntaxTokenKind.Identifier);
        var equalsToken = Match(SyntaxTokenKind.Equals);
        var value = ParseExpression();
        var semicolon = Match(SyntaxTokenKind.Semicolon);

        return new ConstDeclarationSyntax(constKeyword, type, identifier, equalsToken, value, semicolon);
    }

    private BaseListSyntax? ParseBaseList()
    {
        if (Current.Kind != SyntaxTokenKind.Colon)
            return null;

        var colon = NextToken();
        var baseType = ParseType();
        return new BaseListSyntax(colon, baseType);
    }

    // ------------------------------------------------------------------
    // Class/Actor Members
    // ------------------------------------------------------------------

    private ImmutableArray<MemberDeclarationSyntax> ParseClassMembers()
    {
        var members = ImmutableArray.CreateBuilder<MemberDeclarationSyntax>();

        while (Current.Kind != SyntaxTokenKind.CloseBrace && !IsAtEnd)
        {
            var startPos = _position;

            if (Current.Kind == SyntaxTokenKind.DefaultKeyword && Peek(1).Kind == SyntaxTokenKind.OpenBrace)
            {
                members.Add(ParseDefaultBlock());
                continue;
            }

            if (Current.Kind == SyntaxTokenKind.StatesKeyword)
            {
                members.Add(ParseStatesBlock());
                continue;
            }

            if (Current.Kind == SyntaxTokenKind.EnumKeyword)
            {
                members.Add(ParseEnumDeclaration(ImmutableArray<SyntaxToken>.Empty));
                continue;
            }

            if (Current.Kind == SyntaxTokenKind.ConstKeyword)
            {
                members.Add(ParseConstDeclaration(ImmutableArray<SyntaxToken>.Empty));
                continue;
            }

            var modifiers = ParseModifiers();

            if (Current.Kind == SyntaxTokenKind.EnumKeyword)
            {
                members.Add(ParseEnumDeclaration(modifiers));
            }
            else if (Current.Kind == SyntaxTokenKind.ConstKeyword)
            {
                members.Add(ParseConstDeclaration(modifiers));
            }
            else if (Current.Kind == SyntaxTokenKind.StructKeyword)
            {
                members.Add(ParseStructDeclaration(modifiers));
            }
            else if (IsTypeStart() || modifiers.Length > 0)
            {
                members.Add(ParseMethodOrField(modifiers));
            }
            else
            {
                // Error recovery
                if (_position == startPos)
                {
                    _diagnostics.Add(new Diagnostic(
                        DiagnosticSeverity.Error,
                        $"Unexpected token '{Current.Kind}' in class body",
                        Current.Span));
                    NextToken();
                }
            }
        }

        return members.ToImmutable();
    }

    private ImmutableArray<MemberDeclarationSyntax> ParseActorBody()
    {
        var members = ImmutableArray.CreateBuilder<MemberDeclarationSyntax>();

        while (Current.Kind != SyntaxTokenKind.CloseBrace && !IsAtEnd)
        {
            var startPos = _position;

            if (Current.Kind == SyntaxTokenKind.StatesKeyword)
            {
                members.Add(ParseStatesBlock());
                continue;
            }

            // Flags: +FLAG or -FLAG
            if (Current.Kind == SyntaxTokenKind.Plus || Current.Kind == SyntaxTokenKind.Minus)
            {
                members.Add(ParseFlagAsDeclaration());
                continue;
            }

            // Property assignments: identifier value or identifier.identifier value
            if (IsActorPropertyStart())
            {
                members.Add(ParsePropertyAsDeclaration());
                continue;
            }

            // Error recovery
            if (_position == startPos)
            {
                _diagnostics.Add(new Diagnostic(
                    DiagnosticSeverity.Error,
                    $"Unexpected token '{Current.Kind}' in actor body",
                    Current.Span));
                NextToken();
            }
        }

        return members.ToImmutable();
    }

    private bool IsActorPropertyStart()
    {
        // Property: Identifier [.Identifier] values...
        // But not keywords that start states blocks etc.
        if (Current.Kind == SyntaxTokenKind.Identifier ||
            IsPredefinedTypeKeyword(Current.Kind) ||
            Current.Kind == SyntaxTokenKind.NameKeyword ||
            Current.Kind == SyntaxTokenKind.SoundKeyword ||
            Current.Kind == SyntaxTokenKind.StateKeyword)
        {
            return true;
        }

        return false;
    }

    // Wraps a FlagDefinitionSyntax as a DefaultBlockSyntax for use in actor body
    private DefaultBlockSyntax ParseFlagAsDeclaration()
    {
        var flag = ParseFlagDefinition();
        var items = ImmutableArray.Create<DefaultItemSyntax>(flag);

        // Create a synthetic default block around the single flag
        var keyword = new SyntaxToken(SyntaxTokenKind.DefaultKeyword, string.Empty, flag.Span, isMissing: true);
        var openBrace = new SyntaxToken(SyntaxTokenKind.OpenBrace, string.Empty, flag.Span, isMissing: true);
        var closeBrace = new SyntaxToken(SyntaxTokenKind.CloseBrace, string.Empty, flag.Span, isMissing: true);

        return new DefaultBlockSyntax(keyword, openBrace, items, closeBrace);
    }

    private DefaultBlockSyntax ParsePropertyAsDeclaration()
    {
        var prop = ParsePropertyAssignment();
        var items = ImmutableArray.Create<DefaultItemSyntax>(prop);

        var keyword = new SyntaxToken(SyntaxTokenKind.DefaultKeyword, string.Empty, prop.Span, isMissing: true);
        var openBrace = new SyntaxToken(SyntaxTokenKind.OpenBrace, string.Empty, prop.Span, isMissing: true);
        var closeBrace = new SyntaxToken(SyntaxTokenKind.CloseBrace, string.Empty, prop.Span, isMissing: true);

        return new DefaultBlockSyntax(keyword, openBrace, items, closeBrace);
    }

    // ------------------------------------------------------------------
    // Default block
    // ------------------------------------------------------------------

    private DefaultBlockSyntax ParseDefaultBlock()
    {
        var keyword = Match(SyntaxTokenKind.DefaultKeyword);
        var openBrace = Match(SyntaxTokenKind.OpenBrace);
        var items = ParseDefaultItems();
        var closeBrace = Match(SyntaxTokenKind.CloseBrace);

        return new DefaultBlockSyntax(keyword, openBrace, items, closeBrace);
    }

    private ImmutableArray<DefaultItemSyntax> ParseDefaultItems()
    {
        var items = ImmutableArray.CreateBuilder<DefaultItemSyntax>();

        while (Current.Kind != SyntaxTokenKind.CloseBrace && !IsAtEnd)
        {
            var startPos = _position;

            if (Current.Kind == SyntaxTokenKind.Plus || Current.Kind == SyntaxTokenKind.Minus)
            {
                items.Add(ParseFlagDefinition());
            }
            else if (Current.Kind == SyntaxTokenKind.Identifier ||
                     IsPredefinedTypeKeyword(Current.Kind) ||
                     Current.Kind == SyntaxTokenKind.NameKeyword ||
                     Current.Kind == SyntaxTokenKind.SoundKeyword ||
                     Current.Kind == SyntaxTokenKind.StateKeyword)
            {
                items.Add(ParsePropertyAssignment());
            }
            else
            {
                // Error recovery
                if (_position == startPos)
                {
                    _diagnostics.Add(new Diagnostic(
                        DiagnosticSeverity.Error,
                        $"Unexpected token '{Current.Kind}' in default block",
                        Current.Span));
                    NextToken();
                }
            }
        }

        return items.ToImmutable();
    }

    private FlagDefinitionSyntax ParseFlagDefinition()
    {
        var plusOrMinus = NextToken(); // + or -

        SyntaxToken? prefixIdentifier = null;
        SyntaxToken? dotToken = null;
        SyntaxToken flagName;

        // Accept any identifier-like token (including keywords used as names)
        var first = MatchIdentifierOrKeywordAsIdentifier();

        if (Current.Kind == SyntaxTokenKind.Dot)
        {
            prefixIdentifier = first;
            dotToken = NextToken();
            flagName = MatchIdentifierOrKeywordAsIdentifier();
        }
        else
        {
            flagName = first;
        }

        var semicolonToken = MatchOptional(SyntaxTokenKind.Semicolon);

        return new FlagDefinitionSyntax(plusOrMinus, prefixIdentifier, dotToken, flagName, semicolonToken);
    }

    private PropertyAssignmentSyntax ParsePropertyAssignment()
    {
        SyntaxToken? prefixIdentifier = null;
        SyntaxToken? dotToken = null;
        SyntaxToken propertyName;

        var first = MatchIdentifierOrKeywordAsIdentifier();

        if (Current.Kind == SyntaxTokenKind.Dot)
        {
            prefixIdentifier = first;
            dotToken = NextToken();
            propertyName = MatchIdentifierOrKeywordAsIdentifier();
        }
        else
        {
            propertyName = first;
        }

        var values = ParsePropertyValues();
        var semicolon = Match(SyntaxTokenKind.Semicolon);

        return new PropertyAssignmentSyntax(prefixIdentifier, dotToken, propertyName, values, semicolon);
    }

    private ImmutableArray<ExpressionSyntax> ParsePropertyValues()
    {
        var values = ImmutableArray.CreateBuilder<ExpressionSyntax>();

        while (Current.Kind != SyntaxTokenKind.Semicolon &&
               Current.Kind != SyntaxTokenKind.CloseBrace &&
               !IsAtEnd)
        {
            var startPos = _position;
            values.Add(ParseExpression());
            // Optional comma between values
            MatchOptional(SyntaxTokenKind.Comma);

            // Safety: ensure progress to avoid infinite loop when
            // ParseExpression encounters a token it cannot consume
            if (_position == startPos)
            {
                _diagnostics.Add(new Diagnostic(
                    DiagnosticSeverity.Error,
                    $"Unexpected token '{Current.Kind}' in property values",
                    Current.Span));
                NextToken();
            }
        }

        return values.ToImmutable();
    }

    // ------------------------------------------------------------------
    // States block
    // ------------------------------------------------------------------

    private StatesBlockSyntax ParseStatesBlock()
    {
        var keyword = Match(SyntaxTokenKind.StatesKeyword);

        SyntaxToken? openParen = null;
        var stateOptions = ImmutableArray<SyntaxToken>.Empty;
        SyntaxToken? closeParen = null;

        if (Current.Kind == SyntaxTokenKind.OpenParen)
        {
            openParen = NextToken();
            var optBuilder = ImmutableArray.CreateBuilder<SyntaxToken>();
            while (Current.Kind != SyntaxTokenKind.CloseParen && !IsAtEnd)
            {
                optBuilder.Add(NextToken());
                MatchOptional(SyntaxTokenKind.Comma);
            }
            stateOptions = optBuilder.ToImmutable();
            closeParen = Match(SyntaxTokenKind.CloseParen);
        }

        var openBrace = Match(SyntaxTokenKind.OpenBrace);
        var states = ParseStates();
        var closeBrace = Match(SyntaxTokenKind.CloseBrace);

        return new StatesBlockSyntax(keyword, openParen, stateOptions, closeParen,
            openBrace, states, closeBrace);
    }

    private ImmutableArray<StateSyntax> ParseStates()
    {
        var states = ImmutableArray.CreateBuilder<StateSyntax>();

        while (Current.Kind != SyntaxTokenKind.CloseBrace && !IsAtEnd)
        {
            var startPos = _position;
            var state = ParseState();
            if (state != null)
            {
                states.Add(state);
            }
            else if (_position == startPos)
            {
                _diagnostics.Add(new Diagnostic(
                    DiagnosticSeverity.Error,
                    $"Unexpected token '{Current.Kind}' in states block",
                    Current.Span));
                NextToken();
            }
        }

        return states.ToImmutable();
    }

    private StateSyntax? ParseState()
    {
        // Flow keywords
        switch (Current.Kind)
        {
            case SyntaxTokenKind.StopKeyword:
                return new StateStopSyntax(NextToken());
            case SyntaxTokenKind.WaitKeyword:
                return new StateWaitSyntax(NextToken());
            case SyntaxTokenKind.LoopKeyword:
                return new StateLoopSyntax(NextToken());
            case SyntaxTokenKind.FailKeyword:
                return new StateFailSyntax(NextToken());
            case SyntaxTokenKind.GotoKeyword:
                return ParseStateGoto();
        }

        // State label or state frame?
        // Label: Identifier Colon  -or-  Identifier Dot Identifier Colon
        if (IsIdentifierLike(Current.Kind))
        {
            if (Peek(1).Kind == SyntaxTokenKind.Colon)
            {
                return ParseStateLabel();
            }
            if (Peek(1).Kind == SyntaxTokenKind.Dot &&
                IsIdentifierLike(Peek(2).Kind) &&
                Peek(3).Kind == SyntaxTokenKind.Colon)
            {
                return ParseStateLabel();
            }

            // State frame: Sprite Frames Duration [modifiers] [action]
            if (IsIdentifierLike(Peek(1).Kind))
            {
                return ParseStateFrame();
            }
        }

        return null;
    }

    private StateLabelSyntax ParseStateLabel()
    {
        var identifier = NextToken();
        SyntaxToken? dotToken = null;
        SyntaxToken? subIdentifier = null;

        if (Current.Kind == SyntaxTokenKind.Dot)
        {
            dotToken = NextToken();
            subIdentifier = NextToken();
        }

        var colon = Match(SyntaxTokenKind.Colon);
        return new StateLabelSyntax(identifier, dotToken, subIdentifier, colon);
    }

    private StateFrameSyntax ParseStateFrame()
    {
        var sprite = NextToken(); // Sprite name
        var frames = NextToken(); // Frame letters

        SyntaxToken duration;
        if (Current.Kind == SyntaxTokenKind.IntegerLiteral)
        {
            duration = NextToken();
        }
        else if (Current.Kind == SyntaxTokenKind.Minus && Peek(1).Kind == SyntaxTokenKind.IntegerLiteral)
        {
            // Negative duration like -1
            var minus = NextToken();
            var num = NextToken();
            // Combine into a single token conceptually - use the minus token with negative value
            duration = new SyntaxToken(SyntaxTokenKind.IntegerLiteral,
                minus.Text + num.Text,
                new TextSpan(minus.Span.Start, minus.Span.Length + num.Span.Length),
                minus.LeadingTrivia, num.TrailingTrivia,
                num.Value is long lv ? -lv : num.Value);
        }
        else
        {
            duration = Match(SyntaxTokenKind.IntegerLiteral);
        }

        // Modifiers (Bright, Offset, etc.)
        var modifiers = ImmutableArray.CreateBuilder<SyntaxToken>();
        while (Current.Kind == SyntaxTokenKind.Identifier &&
               string.Equals(Current.Text, "Bright", StringComparison.OrdinalIgnoreCase))
        {
            modifiers.Add(NextToken());
        }

        // Optional action
        StateActionSyntax? action = null;
        if (!IsStateTerminator() && Current.Kind != SyntaxTokenKind.CloseBrace)
        {
            action = ParseStateAction();
        }

        return new StateFrameSyntax(sprite, frames, duration,
            modifiers.ToImmutable(), action);
    }

    private bool IsStateTerminator()
    {
        // Peek at current: is it the start of another state entry?
        if (Current.Kind == SyntaxTokenKind.StopKeyword ||
            Current.Kind == SyntaxTokenKind.WaitKeyword ||
            Current.Kind == SyntaxTokenKind.LoopKeyword ||
            Current.Kind == SyntaxTokenKind.FailKeyword ||
            Current.Kind == SyntaxTokenKind.GotoKeyword)
            return true;

        // Another label
        if (IsIdentifierLike(Current.Kind) && Peek(1).Kind == SyntaxTokenKind.Colon)
            return true;
        if (IsIdentifierLike(Current.Kind) && Peek(1).Kind == SyntaxTokenKind.Dot &&
            IsIdentifierLike(Peek(2).Kind) && Peek(3).Kind == SyntaxTokenKind.Colon)
            return true;

        // Another frame line: two consecutive identifiers (sprite + frames)
        if (IsIdentifierLike(Current.Kind) && IsIdentifierLike(Peek(1).Kind) &&
            Peek(1).Kind != SyntaxTokenKind.Colon)
        {
            // Check if it looks like Sprite Frames Duration
            var kind2 = Peek(2).Kind;
            if (kind2 == SyntaxTokenKind.IntegerLiteral ||
                (kind2 == SyntaxTokenKind.Minus && Peek(3).Kind == SyntaxTokenKind.IntegerLiteral))
                return true;
        }

        return false;
    }

    private StateActionSyntax ParseStateAction()
    {
        // Anonymous block
        if (Current.Kind == SyntaxTokenKind.OpenBrace)
        {
            var block = ParseBlockStatement();
            return new StateActionSyntax(null, null, ImmutableArray<ArgumentSyntax>.Empty, null, block);
        }

        // Named action
        var actionId = NextToken();

        SyntaxToken? openParen = null;
        var args = ImmutableArray<ArgumentSyntax>.Empty;
        SyntaxToken? closeParen = null;

        if (Current.Kind == SyntaxTokenKind.OpenParen)
        {
            openParen = NextToken();
            args = ParseArgumentList();
            closeParen = Match(SyntaxTokenKind.CloseParen);
        }

        // Optional semicolon after action
        MatchOptional(SyntaxTokenKind.Semicolon);

        return new StateActionSyntax(actionId, openParen, args, closeParen, null);
    }

    private StateGotoSyntax ParseStateGoto()
    {
        var gotoKeyword = Match(SyntaxTokenKind.GotoKeyword);

        SyntaxToken? classIdentifier = null;
        SyntaxToken? colonColonToken = null;

        // Check for Class::Label pattern
        if (IsIdentifierLike(Current.Kind) && Peek(1).Kind == SyntaxTokenKind.ColonColon)
        {
            classIdentifier = NextToken();
            colonColonToken = NextToken();
        }

        var labelIdentifier = MatchIdentifierOrKeywordAsIdentifier();

        SyntaxToken? plusToken = null;
        SyntaxToken? offset = null;

        if (Current.Kind == SyntaxTokenKind.Plus)
        {
            plusToken = NextToken();
            offset = Match(SyntaxTokenKind.IntegerLiteral);
        }

        return new StateGotoSyntax(gotoKeyword, classIdentifier, colonColonToken,
            labelIdentifier, plusToken, offset);
    }

    // ------------------------------------------------------------------
    // Method or Field disambiguation
    // ------------------------------------------------------------------

    private MemberDeclarationSyntax ParseMethodOrField(ImmutableArray<SyntaxToken> modifiers)
    {
        var type = ParseType();
        var identifier = Match(SyntaxTokenKind.Identifier);

        // Method: type name (
        if (Current.Kind == SyntaxTokenKind.OpenParen)
        {
            return ParseMethodDeclarationRest(modifiers, type, identifier);
        }

        // Field: type name [= expr][, name2 [= expr2]]... ;
        return ParseFieldDeclarationRest(modifiers, type, identifier);
    }

    private MethodDeclarationSyntax ParseMethodDeclarationRest(
        ImmutableArray<SyntaxToken> modifiers, TypeSyntax returnType, SyntaxToken identifier)
    {
        var openParen = Match(SyntaxTokenKind.OpenParen);
        var parameters = ParseParameterList();
        var closeParen = Match(SyntaxTokenKind.CloseParen);

        SyntaxToken? constKeyword = null;
        if (Current.Kind == SyntaxTokenKind.ConstKeyword)
        {
            constKeyword = NextToken();
        }

        BlockStatementSyntax? body = null;
        SyntaxToken? semicolon = null;

        if (Current.Kind == SyntaxTokenKind.OpenBrace)
        {
            body = ParseBlockStatement();
        }
        else
        {
            semicolon = Match(SyntaxTokenKind.Semicolon);
        }

        return new MethodDeclarationSyntax(
            modifiers, returnType, identifier, openParen,
            parameters, closeParen, constKeyword, body, semicolon);
    }

    private FieldDeclarationSyntax ParseFieldDeclarationRest(
        ImmutableArray<SyntaxToken> modifiers, TypeSyntax type, SyntaxToken identifier)
    {
        var variables = ImmutableArray.CreateBuilder<VariableDeclaratorSyntax>();

        SyntaxToken? equalsToken = null;
        ExpressionSyntax? initializer = null;

        if (Current.Kind == SyntaxTokenKind.Equals)
        {
            equalsToken = NextToken();
            initializer = ParseExpression();
        }

        variables.Add(new VariableDeclaratorSyntax(identifier, equalsToken, initializer));

        while (Current.Kind == SyntaxTokenKind.Comma)
        {
            NextToken(); // consume comma
            var nextId = Match(SyntaxTokenKind.Identifier);
            SyntaxToken? nextEquals = null;
            ExpressionSyntax? nextInit = null;
            if (Current.Kind == SyntaxTokenKind.Equals)
            {
                nextEquals = NextToken();
                nextInit = ParseExpression();
            }
            variables.Add(new VariableDeclaratorSyntax(nextId, nextEquals, nextInit));
        }

        var semicolon = Match(SyntaxTokenKind.Semicolon);

        return new FieldDeclarationSyntax(
            modifiers, type,
            new SeparatedSyntaxList<VariableDeclaratorSyntax>(variables.ToImmutable()),
            semicolon);
    }

    private SeparatedSyntaxList<ParameterSyntax> ParseParameterList()
    {
        var parameters = ImmutableArray.CreateBuilder<ParameterSyntax>();

        if (Current.Kind != SyntaxTokenKind.CloseParen)
        {
            parameters.Add(ParseParameter());

            while (Current.Kind == SyntaxTokenKind.Comma)
            {
                NextToken(); // consume comma
                parameters.Add(ParseParameter());
            }
        }

        return new SeparatedSyntaxList<ParameterSyntax>(parameters.ToImmutable());
    }

    private ParameterSyntax ParseParameter()
    {
        var paramModifiers = ImmutableArray.CreateBuilder<SyntaxToken>();

        while (Current.Kind == SyntaxTokenKind.OutKeyword ||
               Current.Kind == SyntaxTokenKind.InKeyword)
        {
            paramModifiers.Add(NextToken());
        }

        var type = ParseType();
        var identifier = Match(SyntaxTokenKind.Identifier);

        SyntaxToken? equalsToken = null;
        ExpressionSyntax? defaultValue = null;

        if (Current.Kind == SyntaxTokenKind.Equals)
        {
            equalsToken = NextToken();
            defaultValue = ParseExpression();
        }

        return new ParameterSyntax(paramModifiers.ToImmutable(), type, identifier, equalsToken, defaultValue);
    }

    // ------------------------------------------------------------------
    // Types
    // ------------------------------------------------------------------

    private TypeSyntax ParseType()
    {
        // Array<T>
        if (Current.Kind == SyntaxTokenKind.ArrayKeyword)
        {
            var arrayKw = NextToken();
            var lt = Match(SyntaxTokenKind.LessThan);
            var elementType = ParseType();
            var gt = Match(SyntaxTokenKind.GreaterThan);
            return new ArrayTypeSyntax(arrayKw, lt, elementType, gt);
        }

        // Map<K, V>
        if (Current.Kind == SyntaxTokenKind.MapKeyword)
        {
            var mapKw = NextToken();
            var lt = Match(SyntaxTokenKind.LessThan);
            var keyType = ParseType();
            var comma = Match(SyntaxTokenKind.Comma);
            var valueType = ParseType();
            var gt = Match(SyntaxTokenKind.GreaterThan);
            return new MapTypeSyntax(mapKw, lt, keyType, comma, valueType, gt);
        }

        // Class<T>
        if (Current.Kind == SyntaxTokenKind.ClassKeyword && Peek(1).Kind == SyntaxTokenKind.LessThan)
        {
            var classKw = NextToken();
            var lt = Match(SyntaxTokenKind.LessThan);
            var constraintType = ParseType();
            var gt = Match(SyntaxTokenKind.GreaterThan);
            return new ClassTypeSyntax(classKw, lt, constraintType, gt);
        }

        // Predefined types
        if (IsPredefinedTypeKeyword(Current.Kind))
        {
            return new PredefinedTypeSyntax(NextToken());
        }

        // Named type (identifier or keyword used as type name)
        if (Current.Kind == SyntaxTokenKind.Identifier || IsIdentifierLike(Current.Kind))
        {
            var identifier = NextToken();
            TypeArgumentListSyntax? typeArgs = null;

            if (Current.Kind == SyntaxTokenKind.LessThan)
            {
                typeArgs = ParseTypeArgumentList();
            }

            return new NamedTypeSyntax(identifier, typeArgs);
        }

        // Fallback: produce a missing identifier token for error recovery
        _diagnostics.Add(new Diagnostic(
            DiagnosticSeverity.Error,
            $"Expected type, got '{Current.Kind}'",
            Current.Span));

        var missing = new SyntaxToken(SyntaxTokenKind.Identifier, string.Empty,
            Current.Span, isMissing: true);
        return new NamedTypeSyntax(missing);
    }

    private TypeArgumentListSyntax ParseTypeArgumentList()
    {
        var lt = Match(SyntaxTokenKind.LessThan);
        var args = ImmutableArray.CreateBuilder<TypeSyntax>();

        args.Add(ParseType());
        while (Current.Kind == SyntaxTokenKind.Comma)
        {
            NextToken(); // consume comma
            args.Add(ParseType());
        }

        var gt = Match(SyntaxTokenKind.GreaterThan);
        return new TypeArgumentListSyntax(lt,
            new SeparatedSyntaxList<TypeSyntax>(args.ToImmutable()), gt);
    }

    private bool IsTypeStart()
    {
        if (IsPredefinedTypeKeyword(Current.Kind))
            return true;
        if (Current.Kind == SyntaxTokenKind.Identifier)
            return true;
        if (Current.Kind == SyntaxTokenKind.ArrayKeyword)
            return true;
        if (Current.Kind == SyntaxTokenKind.MapKeyword)
            return true;
        if (Current.Kind == SyntaxTokenKind.ClassKeyword && Peek(1).Kind == SyntaxTokenKind.LessThan)
            return true;
        if (Current.Kind == SyntaxTokenKind.LetKeyword)
            return true;
        return false;
    }

    private static bool IsPredefinedTypeKeyword(SyntaxTokenKind kind)
    {
        switch (kind)
        {
            case SyntaxTokenKind.VoidKeyword:
            case SyntaxTokenKind.IntKeyword:
            case SyntaxTokenKind.UIntKeyword:
            case SyntaxTokenKind.FloatKeyword:
            case SyntaxTokenKind.DoubleKeyword:
            case SyntaxTokenKind.BoolKeyword:
            case SyntaxTokenKind.StringKeyword:
            case SyntaxTokenKind.VectorKeyword:
            case SyntaxTokenKind.NameKeyword:
            case SyntaxTokenKind.StateKeyword:
            case SyntaxTokenKind.ColorKeyword:
            case SyntaxTokenKind.SoundKeyword:
            case SyntaxTokenKind.LetKeyword:
                return true;
            default:
                return false;
        }
    }

    // ------------------------------------------------------------------
    // Expressions (precedence climbing)
    // ------------------------------------------------------------------

    /// <summary>
    /// Parses an expression using precedence climbing.
    /// </summary>
    public ExpressionSyntax ParseExpression(int precedence = 0)
    {
        var left = ParseUnaryExpression();

        while (true)
        {
            var binaryPrec = GetBinaryOperatorPrecedence(Current.Kind);
            if (binaryPrec == 0 || binaryPrec <= precedence)
                break;

            // Ternary conditional
            if (Current.Kind == SyntaxTokenKind.Question)
            {
                var questionToken = NextToken();
                var whenTrue = ParseExpression();
                var colonToken = Match(SyntaxTokenKind.Colon);
                var whenFalse = ParseExpression();
                left = new ConditionalExpressionSyntax(left, questionToken, whenTrue, colonToken, whenFalse);
                continue;
            }

            var operatorToken = NextToken();
            var right = ParseExpression(binaryPrec);
            left = new BinaryExpressionSyntax(left, operatorToken, right);
        }

        return left;
    }

    private ExpressionSyntax ParseUnaryExpression()
    {
        if (IsUnaryOperator(Current.Kind))
        {
            var op = NextToken();
            var operand = ParseUnaryExpression();
            return new UnaryExpressionSyntax(op, operand);
        }

        return ParsePostfixExpression();
    }

    private ExpressionSyntax ParsePostfixExpression()
    {
        var expr = ParsePrimaryExpression();

        while (true)
        {
            if (Current.Kind == SyntaxTokenKind.Dot)
            {
                var dot = NextToken();
                var name = MatchIdentifierOrKeywordAsIdentifier();
                expr = new MemberAccessExpressionSyntax(expr, dot, name);
            }
            else if (Current.Kind == SyntaxTokenKind.OpenParen)
            {
                var openParen = NextToken();
                var args = ParseArguments();
                var closeParen = Match(SyntaxTokenKind.CloseParen);
                expr = new InvocationExpressionSyntax(expr, openParen, args, closeParen);
            }
            else if (Current.Kind == SyntaxTokenKind.OpenBracket)
            {
                var openBracket = NextToken();
                var index = ParseExpression();
                var closeBracket = Match(SyntaxTokenKind.CloseBracket);
                expr = new ArrayAccessExpressionSyntax(expr, openBracket, index, closeBracket);
            }
            else
            {
                break;
            }
        }

        return expr;
    }

    private ExpressionSyntax ParsePrimaryExpression()
    {
        switch (Current.Kind)
        {
            case SyntaxTokenKind.IntegerLiteral:
            case SyntaxTokenKind.FloatLiteral:
            case SyntaxTokenKind.StringLiteral:
            case SyntaxTokenKind.NameLiteral:
            case SyntaxTokenKind.TrueKeyword:
            case SyntaxTokenKind.FalseKeyword:
            case SyntaxTokenKind.NullKeyword:
                return new LiteralExpressionSyntax(NextToken());

            case SyntaxTokenKind.Identifier:
                return new IdentifierExpressionSyntax(NextToken());

            case SyntaxTokenKind.OpenParen:
            {
                var open = NextToken();
                var inner = ParseExpression();
                var close = Match(SyntaxTokenKind.CloseParen);
                return new ParenthesizedExpressionSyntax(open, inner, close);
            }

            default:
                // For identifiers that are keywords but can be used as expressions
                // (e.g. "self", type keywords in certain contexts)
                _diagnostics.Add(new Diagnostic(
                    DiagnosticSeverity.Error,
                    $"Expected expression, got '{Current.Kind}'",
                    Current.Span));
                var missing = new SyntaxToken(SyntaxTokenKind.Identifier, string.Empty,
                    Current.Span, isMissing: true);
                return new IdentifierExpressionSyntax(missing);
        }
    }

    private SeparatedSyntaxList<ArgumentSyntax> ParseArguments()
    {
        var args = ImmutableArray.CreateBuilder<ArgumentSyntax>();

        if (Current.Kind != SyntaxTokenKind.CloseParen)
        {
            args.Add(ParseArgument());

            while (Current.Kind == SyntaxTokenKind.Comma)
            {
                NextToken(); // consume comma
                args.Add(ParseArgument());
            }
        }

        return new SeparatedSyntaxList<ArgumentSyntax>(args.ToImmutable());
    }

    private ImmutableArray<ArgumentSyntax> ParseArgumentList()
    {
        var args = ImmutableArray.CreateBuilder<ArgumentSyntax>();

        if (Current.Kind != SyntaxTokenKind.CloseParen)
        {
            args.Add(ParseArgument());

            while (Current.Kind == SyntaxTokenKind.Comma)
            {
                NextToken(); // consume comma
                args.Add(ParseArgument());
            }
        }

        return args.ToImmutable();
    }

    private ArgumentSyntax ParseArgument()
    {
        var expr = ParseExpression();
        return new ArgumentSyntax(null, expr);
    }

    private static int GetBinaryOperatorPrecedence(SyntaxTokenKind kind)
    {
        switch (kind)
        {
            case SyntaxTokenKind.PipePipe:
                return 1;
            case SyntaxTokenKind.AmpersandAmpersand:
                return 2;
            case SyntaxTokenKind.Pipe:
                return 3;
            case SyntaxTokenKind.Caret:
                return 4;
            case SyntaxTokenKind.Ampersand:
                return 5;
            case SyntaxTokenKind.EqualsEquals:
            case SyntaxTokenKind.ExclamationEquals:
            case SyntaxTokenKind.ApproxEquals:
                return 6;
            case SyntaxTokenKind.LessThan:
            case SyntaxTokenKind.GreaterThan:
            case SyntaxTokenKind.LessThanEquals:
            case SyntaxTokenKind.GreaterThanEquals:
                return 7;
            case SyntaxTokenKind.LessThanLessThan:
            case SyntaxTokenKind.GreaterThanGreaterThan:
            case SyntaxTokenKind.GreaterThanGreaterThanGreaterThan:
                return 8;
            case SyntaxTokenKind.Plus:
            case SyntaxTokenKind.Minus:
                return 9;
            case SyntaxTokenKind.Asterisk:
            case SyntaxTokenKind.Slash:
            case SyntaxTokenKind.Percent:
                return 10;
            case SyntaxTokenKind.Question:
                return 1; // Ternary at lowest precedence
            default:
                return 0;
        }
    }

    private static bool IsUnaryOperator(SyntaxTokenKind kind)
    {
        switch (kind)
        {
            case SyntaxTokenKind.Plus:
            case SyntaxTokenKind.Minus:
            case SyntaxTokenKind.Exclamation:
            case SyntaxTokenKind.Tilde:
            case SyntaxTokenKind.PlusPlus:
            case SyntaxTokenKind.MinusMinus:
                return true;
            default:
                return false;
        }
    }

    private static bool IsAssignmentOperator(SyntaxTokenKind kind)
    {
        switch (kind)
        {
            case SyntaxTokenKind.Equals:
            case SyntaxTokenKind.PlusEquals:
            case SyntaxTokenKind.MinusEquals:
            case SyntaxTokenKind.AsteriskEquals:
            case SyntaxTokenKind.SlashEquals:
            case SyntaxTokenKind.PercentEquals:
            case SyntaxTokenKind.AmpersandEquals:
            case SyntaxTokenKind.PipeEquals:
            case SyntaxTokenKind.CaretEquals:
                return true;
            default:
                return false;
        }
    }

    // ------------------------------------------------------------------
    // Statements
    // ------------------------------------------------------------------

    private StatementSyntax ParseStatement()
    {
        switch (Current.Kind)
        {
            case SyntaxTokenKind.OpenBrace:
                return ParseBlockStatement();
            case SyntaxTokenKind.IfKeyword:
                return ParseIfStatement();
            case SyntaxTokenKind.WhileKeyword:
                return ParseWhileStatement();
            case SyntaxTokenKind.DoKeyword:
                return ParseDoWhileStatement();
            case SyntaxTokenKind.ForKeyword:
                return ParseForStatement();
            case SyntaxTokenKind.ReturnKeyword:
                return ParseReturnStatement();
            case SyntaxTokenKind.BreakKeyword:
            {
                var kw = NextToken();
                var semi = Match(SyntaxTokenKind.Semicolon);
                return new BreakStatementSyntax(kw, semi);
            }
            case SyntaxTokenKind.ContinueKeyword:
            {
                var kw = NextToken();
                var semi = Match(SyntaxTokenKind.Semicolon);
                return new ContinueStatementSyntax(kw, semi);
            }
            default:
                return ParseExpressionOrAssignmentStatement();
        }
    }

    private BlockStatementSyntax ParseBlockStatement()
    {
        var openBrace = Match(SyntaxTokenKind.OpenBrace);
        var statements = ImmutableArray.CreateBuilder<StatementSyntax>();

        while (Current.Kind != SyntaxTokenKind.CloseBrace && !IsAtEnd)
        {
            var startPos = _position;
            statements.Add(ParseStatement());
            // Safety: ensure progress
            if (_position == startPos)
            {
                _diagnostics.Add(new Diagnostic(
                    DiagnosticSeverity.Error,
                    $"Unexpected token '{Current.Kind}' in block",
                    Current.Span));
                NextToken();
            }
        }

        var closeBrace = Match(SyntaxTokenKind.CloseBrace);
        return new BlockStatementSyntax(openBrace, statements.ToImmutable(), closeBrace);
    }

    private IfStatementSyntax ParseIfStatement()
    {
        var ifKw = Match(SyntaxTokenKind.IfKeyword);
        var openParen = Match(SyntaxTokenKind.OpenParen);
        var condition = ParseExpression();
        var closeParen = Match(SyntaxTokenKind.CloseParen);
        var statement = ParseStatement();

        SyntaxToken? elseKw = null;
        StatementSyntax? elseStmt = null;

        if (Current.Kind == SyntaxTokenKind.ElseKeyword)
        {
            elseKw = NextToken();
            elseStmt = ParseStatement();
        }

        return new IfStatementSyntax(ifKw, openParen, condition, closeParen,
            statement, elseKw, elseStmt);
    }

    private WhileStatementSyntax ParseWhileStatement()
    {
        var whileKw = Match(SyntaxTokenKind.WhileKeyword);
        var openParen = Match(SyntaxTokenKind.OpenParen);
        var condition = ParseExpression();
        var closeParen = Match(SyntaxTokenKind.CloseParen);
        var body = ParseStatement();

        return new WhileStatementSyntax(whileKw, openParen, condition, closeParen, body);
    }

    private DoWhileStatementSyntax ParseDoWhileStatement()
    {
        var doKw = Match(SyntaxTokenKind.DoKeyword);
        var body = ParseStatement();
        var whileKw = Match(SyntaxTokenKind.WhileKeyword);
        var openParen = Match(SyntaxTokenKind.OpenParen);
        var condition = ParseExpression();
        var closeParen = Match(SyntaxTokenKind.CloseParen);
        var semi = Match(SyntaxTokenKind.Semicolon);

        return new DoWhileStatementSyntax(doKw, body, whileKw, openParen, condition, closeParen, semi);
    }

    private ForStatementSyntax ParseForStatement()
    {
        var forKw = Match(SyntaxTokenKind.ForKeyword);
        var openParen = Match(SyntaxTokenKind.OpenParen);

        // Initializer (expression statement with semicolon, or just a semicolon)
        StatementSyntax? initializer = null;
        if (Current.Kind != SyntaxTokenKind.Semicolon)
        {
            initializer = ParseExpressionOrAssignmentStatement();
        }
        else
        {
            NextToken(); // consume the semicolon
        }

        // Condition
        ExpressionSyntax? condition = null;
        if (Current.Kind != SyntaxTokenKind.Semicolon)
        {
            condition = ParseExpression();
        }
        var semi = Match(SyntaxTokenKind.Semicolon);

        // Incrementor (could be an expression or assignment like i = i + 1)
        ExpressionSyntax? incrementor = null;
        if (Current.Kind != SyntaxTokenKind.CloseParen)
        {
            incrementor = ParseExpression();
            // If followed by assignment, parse it as part of the incrementor
            // We treat assignment as a binary expression for the for-loop incrementor
            if (IsAssignmentOperator(Current.Kind))
            {
                var assignOp = NextToken();
                var rhs = ParseExpression();
                incrementor = new BinaryExpressionSyntax(incrementor, assignOp, rhs);
            }
        }

        var closeParen = Match(SyntaxTokenKind.CloseParen);
        var body = ParseStatement();

        return new ForStatementSyntax(forKw, openParen, initializer, condition,
            semi, incrementor, closeParen, body);
    }

    private ReturnStatementSyntax ParseReturnStatement()
    {
        var returnKw = Match(SyntaxTokenKind.ReturnKeyword);

        ExpressionSyntax? expr = null;
        if (Current.Kind != SyntaxTokenKind.Semicolon)
        {
            expr = ParseExpression();
        }

        var semi = Match(SyntaxTokenKind.Semicolon);
        return new ReturnStatementSyntax(returnKw, expr, semi);
    }

    private StatementSyntax ParseExpressionOrAssignmentStatement()
    {
        var expr = ParseExpression();

        // Check for assignment operator
        if (IsAssignmentOperator(Current.Kind))
        {
            var op = NextToken();
            var value = ParseExpression();
            var semi = Match(SyntaxTokenKind.Semicolon);
            return new AssignmentStatementSyntax(expr, op, value, semi);
        }

        var semicolon = Match(SyntaxTokenKind.Semicolon);
        return new ExpressionStatementSyntax(expr, semicolon);
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    /// <summary>
    /// Matches the current token as an identifier, accepting keywords that
    /// may be used as identifiers in certain contexts.
    /// </summary>
    private SyntaxToken MatchIdentifierOrKeywordAsIdentifier()
    {
        if (Current.Kind == SyntaxTokenKind.Identifier)
            return NextToken();

        // Many keywords are used as identifiers in ZScript/DECORATE contexts
        if (IsIdentifierLike(Current.Kind))
            return NextToken();

        return Match(SyntaxTokenKind.Identifier);
    }

    /// <summary>
    /// Returns true if the token kind could be treated as an identifier in certain contexts.
    /// Keywords in ZScript/DECORATE can appear as names in many contexts.
    /// </summary>
    private static bool IsIdentifierLike(SyntaxTokenKind kind)
    {
        switch (kind)
        {
            case SyntaxTokenKind.Identifier:
            case SyntaxTokenKind.ActorKeyword:
            case SyntaxTokenKind.ReplacesKeyword:
            case SyntaxTokenKind.VersionKeyword:
            case SyntaxTokenKind.ExtendKeyword:
            case SyntaxTokenKind.MixinKeyword:
            case SyntaxTokenKind.DeprecatedKeyword:
            case SyntaxTokenKind.DefaultKeyword:
            case SyntaxTokenKind.StatesKeyword:
            case SyntaxTokenKind.StopKeyword:
            case SyntaxTokenKind.WaitKeyword:
            case SyntaxTokenKind.FailKeyword:
            case SyntaxTokenKind.LoopKeyword:
            case SyntaxTokenKind.GotoKeyword:
            case SyntaxTokenKind.NativeKeyword:
            case SyntaxTokenKind.VirtualKeyword:
            case SyntaxTokenKind.OverrideKeyword:
            case SyntaxTokenKind.FinalKeyword:
            case SyntaxTokenKind.AbstractKeyword:
            case SyntaxTokenKind.PrivateKeyword:
            case SyntaxTokenKind.ProtectedKeyword:
            case SyntaxTokenKind.StaticKeyword:
            case SyntaxTokenKind.ReadOnlyKeyword:
            case SyntaxTokenKind.ConstKeyword:
            case SyntaxTokenKind.NameKeyword:
            case SyntaxTokenKind.SoundKeyword:
            case SyntaxTokenKind.StateKeyword:
            case SyntaxTokenKind.ColorKeyword:
            case SyntaxTokenKind.IntKeyword:
            case SyntaxTokenKind.UIntKeyword:
            case SyntaxTokenKind.FloatKeyword:
            case SyntaxTokenKind.DoubleKeyword:
            case SyntaxTokenKind.BoolKeyword:
            case SyntaxTokenKind.StringKeyword:
            case SyntaxTokenKind.VoidKeyword:
            case SyntaxTokenKind.VectorKeyword:
            case SyntaxTokenKind.TrueKeyword:
            case SyntaxTokenKind.FalseKeyword:
            case SyntaxTokenKind.NullKeyword:
                return true;
            default:
                return false;
        }
    }
}
