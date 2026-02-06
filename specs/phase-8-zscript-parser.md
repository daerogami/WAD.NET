# Phase 8: Roslyn-Style ZScript/DECORATE Parser

## Overview

This phase implements a full-fidelity, Roslyn-inspired AST parser for ZScript and DECORATE languages. The parser architecture enables:

- **Linting & Static Analysis**: Detect unused actors, missing states, invalid property values
- **IDE Tooling**: Syntax highlighting, code completion, go-to-definition
- **C# Transpilation**: Convert ZScript to equivalent C# code for native .NET mods
- **Source Transformation**: Automated refactoring, code formatting, migrations
- **Round-trip Preservation**: Parse and re-emit source with identical whitespace/comments

### Why Roslyn-Style?

| Approach | Pros | Cons |
|----------|------|------|
| Regex-based (current) | Simple, fast for detection | Cannot handle nested structures, loses context, no AST |
| Custom simple AST | Flexible, tailored to domain | Requires inventing patterns, harder to extend |
| **Roslyn-style AST** | Proven architecture, full fidelity, excellent tooling patterns | More complex upfront, larger API surface |

The Roslyn architecture provides:
- **Immutable trees** - Thread-safe, cacheable, efficient incremental updates
- **Red-Green trees** - Separate internal (green) nodes from position-aware (red) wrappers
- **Full fidelity** - Every character from source is represented (trivia preservation)
- **Visitor/Rewriter patterns** - Extensible traversal and transformation

## Priority: MEDIUM

Full parsing is required for accurate weapon/monster detection, conflict analysis, and advanced tooling.

---

## Architecture Overview

```
Source Text
    │
    ▼
┌─────────────────┐
│     Lexer       │  Produces SyntaxTokens with trivia
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│     Parser      │  Builds syntax tree from tokens
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│   Syntax Tree   │  Immutable, full-fidelity AST
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Semantic Model  │  Symbol resolution, type checking
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│    Tooling      │  Walkers, rewriters, formatters
└─────────────────┘
```

---

## Task 8.1: Core Syntax Infrastructure

### SyntaxTrivia

Represents non-semantic content: whitespace, comments, preprocessor directives.

```csharp
namespace WAD.NET.ZScript.Syntax;

public readonly struct SyntaxTrivia : IEquatable<SyntaxTrivia>
{
    public SyntaxTriviaKind Kind { get; }
    public string Text { get; }
    public TextSpan Span { get; }

    public bool IsWhitespace => Kind is SyntaxTriviaKind.Whitespace
                                      or SyntaxTriviaKind.EndOfLine;
    public bool IsComment => Kind is SyntaxTriviaKind.SingleLineComment
                                   or SyntaxTriviaKind.MultiLineComment;

    public SyntaxTrivia(SyntaxTriviaKind kind, string text, TextSpan span)
    {
        Kind = kind;
        Text = text;
        Span = span;
    }
}

public enum SyntaxTriviaKind
{
    None,
    Whitespace,
    EndOfLine,
    SingleLineComment,      // // comment
    MultiLineComment,       // /* comment */
    PreprocessorDirective,  // #include, #define
    SkippedTokens,          // Error recovery
}

public readonly struct TextSpan
{
    public int Start { get; }
    public int Length { get; }
    public int End => Start + Length;

    public TextSpan(int start, int length)
    {
        Start = start;
        Length = length;
    }
}

public readonly struct SyntaxTriviaList : IReadOnlyList<SyntaxTrivia>
{
    private readonly ImmutableArray<SyntaxTrivia> _trivia;

    public int Count => _trivia.Length;
    public SyntaxTrivia this[int index] => _trivia[index];

    public static SyntaxTriviaList Empty { get; } = new(ImmutableArray<SyntaxTrivia>.Empty);

    public SyntaxTriviaList(ImmutableArray<SyntaxTrivia> trivia)
    {
        _trivia = trivia;
    }

    public IEnumerator<SyntaxTrivia> GetEnumerator() =>
        ((IEnumerable<SyntaxTrivia>)_trivia).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
```

### SyntaxToken

Represents a single lexical token with leading/trailing trivia.

```csharp
public readonly struct SyntaxToken : IEquatable<SyntaxToken>
{
    public SyntaxTokenKind Kind { get; }
    public string Text { get; }
    public TextSpan Span { get; }
    public TextSpan FullSpan { get; }  // Includes trivia

    public SyntaxTriviaList LeadingTrivia { get; }
    public SyntaxTriviaList TrailingTrivia { get; }

    // For literals
    public object? Value { get; }

    public bool IsMissing { get; }  // Synthesized during error recovery

    public SyntaxToken(
        SyntaxTokenKind kind,
        string text,
        TextSpan span,
        SyntaxTriviaList leadingTrivia,
        SyntaxTriviaList trailingTrivia,
        object? value = null,
        bool isMissing = false)
    {
        Kind = kind;
        Text = text;
        Span = span;
        LeadingTrivia = leadingTrivia;
        TrailingTrivia = trailingTrivia;
        Value = value;
        IsMissing = isMissing;

        var start = leadingTrivia.Count > 0
            ? leadingTrivia[0].Span.Start
            : span.Start;
        var end = trailingTrivia.Count > 0
            ? trailingTrivia[^1].Span.End
            : span.End;
        FullSpan = new TextSpan(start, end - start);
    }

    public SyntaxToken WithLeadingTrivia(SyntaxTriviaList trivia) =>
        new(Kind, Text, Span, trivia, TrailingTrivia, Value, IsMissing);

    public SyntaxToken WithTrailingTrivia(SyntaxTriviaList trivia) =>
        new(Kind, Text, Span, LeadingTrivia, trivia, Value, IsMissing);
}

public enum SyntaxTokenKind
{
    None,
    EndOfFile,

    // Literals
    IntegerLiteral,
    FloatLiteral,
    StringLiteral,
    NameLiteral,        // 'name'

    // Identifiers
    Identifier,

    // Keywords - Shared
    ClassKeyword,       // class
    StructKeyword,      // struct
    EnumKeyword,        // enum
    ConstKeyword,       // const
    StaticKeyword,      // static
    PrivateKeyword,     // private
    ProtectedKeyword,   // protected
    VirtualKeyword,     // virtual
    OverrideKeyword,    // override
    FinalKeyword,       // final
    NativeKeyword,      // native
    DefaultKeyword,     // default / Default
    StatesKeyword,      // states / States

    // Keywords - DECORATE specific
    ActorKeyword,       // actor
    ReplacesKeyword,    // replaces

    // Keywords - ZScript specific
    VersionKeyword,     // version
    ExtendKeyword,      // extend
    MixinKeyword,       // mixin
    AbstractKeyword,    // abstract
    DeprecatedKeyword,  // deprecated
    ReadOnlyKeyword,    // readonly
    LetKeyword,         // let
    OutKeyword,         // out
    InKeyword,          // in

    // Control flow
    IfKeyword,
    ElseKeyword,
    WhileKeyword,
    DoKeyword,
    ForKeyword,
    ForEachKeyword,
    SwitchKeyword,
    CaseKeyword,
    BreakKeyword,
    ContinueKeyword,
    ReturnKeyword,
    GotoKeyword,

    // Types
    VoidKeyword,
    IntKeyword,
    UIntKeyword,
    FloatKeyword,
    DoubleKeyword,
    BoolKeyword,
    StringKeyword,
    VectorKeyword,
    NameKeyword,
    StateKeyword,
    ColorKeyword,
    SoundKeyword,
    ArrayKeyword,
    MapKeyword,

    // Boolean literals
    TrueKeyword,
    FalseKeyword,
    NullKeyword,

    // State-specific
    StopKeyword,
    WaitKeyword,
    FailKeyword,
    LoopKeyword,

    // Operators
    Plus,               // +
    Minus,              // -
    Asterisk,           // *
    Slash,              // /
    Percent,            // %
    Ampersand,          // &
    Pipe,               // |
    Caret,              // ^
    Tilde,              // ~
    Exclamation,        // !
    Question,           // ?

    // Comparison
    EqualsEquals,       // ==
    ExclamationEquals,  // !=
    LessThan,           // <
    GreaterThan,        // >
    LessThanEquals,     // <=
    GreaterThanEquals,  // >=
    ApproxEquals,       // ~==

    // Logical
    AmpersandAmpersand, // &&
    PipePipe,           // ||

    // Assignment
    Equals,             // =
    PlusEquals,         // +=
    MinusEquals,        // -=
    AsteriskEquals,     // *=
    SlashEquals,        // /=
    PercentEquals,      // %=
    AmpersandEquals,    // &=
    PipeEquals,         // |=
    CaretEquals,        // ^=

    // Increment/Decrement
    PlusPlus,           // ++
    MinusMinus,         // --

    // Shift
    LessThanLessThan,   // <<
    GreaterThanGreaterThan, // >>
    GreaterThanGreaterThanGreaterThan, // >>>

    // Punctuation
    OpenParen,          // (
    CloseParen,         // )
    OpenBrace,          // {
    CloseBrace,         // }
    OpenBracket,        // [
    CloseBracket,       // ]
    Semicolon,          // ;
    Colon,              // :
    ColonColon,         // ::
    Comma,              // ,
    Dot,                // .
    DotDot,             // ..
    Arrow,              // ->

    // Preprocessor
    Hash,               // #

    // Special
    BadToken,           // Unrecognized
}
```

### SyntaxNode

Base class for all AST nodes.

```csharp
public abstract class SyntaxNode
{
    public abstract SyntaxNodeKind Kind { get; }
    public SyntaxNode? Parent { get; internal set; }
    public TextSpan Span { get; }
    public TextSpan FullSpan { get; }

    protected SyntaxNode(TextSpan span, TextSpan fullSpan)
    {
        Span = span;
        FullSpan = fullSpan;
    }

    public abstract IEnumerable<SyntaxNode> ChildNodes();
    public abstract IEnumerable<SyntaxToken> ChildTokens();

    public IEnumerable<SyntaxNodeOrToken> ChildNodesAndTokens()
    {
        // Returns children in source order
        // Implementation merges ChildNodes() and ChildTokens() by position
    }

    public IEnumerable<SyntaxNode> DescendantNodes()
    {
        foreach (var child in ChildNodes())
        {
            yield return child;
            foreach (var descendant in child.DescendantNodes())
                yield return descendant;
        }
    }

    public IEnumerable<SyntaxToken> DescendantTokens()
    {
        foreach (var child in ChildNodes())
        {
            foreach (var token in child.DescendantTokens())
                yield return token;
        }
        foreach (var token in ChildTokens())
            yield return token;
    }

    public IEnumerable<SyntaxTrivia> DescendantTrivia()
    {
        foreach (var token in DescendantTokens())
        {
            foreach (var trivia in token.LeadingTrivia)
                yield return trivia;
            foreach (var trivia in token.TrailingTrivia)
                yield return trivia;
        }
    }

    public T? FirstAncestorOrSelf<T>() where T : SyntaxNode
    {
        SyntaxNode? node = this;
        while (node != null)
        {
            if (node is T result)
                return result;
            node = node.Parent;
        }
        return null;
    }

    public abstract void Accept(SyntaxVisitor visitor);
    public abstract TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor);

    public string ToFullString()
    {
        var builder = new StringBuilder();
        foreach (var token in DescendantTokens())
        {
            foreach (var trivia in token.LeadingTrivia)
                builder.Append(trivia.Text);
            builder.Append(token.Text);
            foreach (var trivia in token.TrailingTrivia)
                builder.Append(trivia.Text);
        }
        return builder.ToString();
    }

    public override string ToString()
    {
        var builder = new StringBuilder();
        foreach (var token in DescendantTokens())
            builder.Append(token.Text);
        return builder.ToString();
    }
}

public readonly struct SyntaxNodeOrToken
{
    public bool IsNode { get; }
    public bool IsToken => !IsNode;

    private readonly SyntaxNode? _node;
    private readonly SyntaxToken _token;

    public SyntaxNode? AsNode() => IsNode ? _node : null;
    public SyntaxToken AsToken() => IsToken ? _token : default;

    public TextSpan Span => IsNode ? _node!.Span : _token.Span;
    public TextSpan FullSpan => IsNode ? _node!.FullSpan : _token.FullSpan;
}
```

---

## Task 8.2: ZScript/DECORATE Syntax Nodes

### Compilation Unit

```csharp
public enum SyntaxNodeKind
{
    // Top-level
    CompilationUnit,
    VersionDirective,
    IncludeDirective,

    // Declarations
    ClassDeclaration,
    ActorDeclaration,      // DECORATE actor
    StructDeclaration,
    EnumDeclaration,
    ConstDeclaration,

    // Members
    FieldDeclaration,
    PropertyDeclaration,
    MethodDeclaration,
    DefaultBlock,
    StatesBlock,
    FlagDefinition,

    // States
    StateLabel,
    StateFrame,
    StateAction,
    StateGoto,
    StateStop,
    StateWait,
    StateLoop,
    StateFail,

    // Types
    PredefinedType,
    NamedType,
    ArrayType,
    MapType,
    ClassType,

    // Expressions
    LiteralExpression,
    IdentifierExpression,
    MemberAccessExpression,
    InvocationExpression,
    BinaryExpression,
    UnaryExpression,
    ConditionalExpression,
    CastExpression,
    ArrayAccessExpression,
    ParenthesizedExpression,

    // Statements
    BlockStatement,
    ExpressionStatement,
    IfStatement,
    WhileStatement,
    DoWhileStatement,
    ForStatement,
    ForEachStatement,
    SwitchStatement,
    CaseLabel,
    DefaultLabel,
    ReturnStatement,
    BreakStatement,
    ContinueStatement,
    LocalDeclarationStatement,
    AssignmentStatement,

    // Parameters
    Parameter,
    Argument,

    // Other
    AttributeList,
    Attribute,
    TypeArgumentList,
    BaseList,
}

// Top-level compilation unit
public sealed class CompilationUnitSyntax : SyntaxNode
{
    public override SyntaxNodeKind Kind => SyntaxNodeKind.CompilationUnit;

    public SyntaxToken? VersionDirective { get; }
    public ImmutableArray<IncludeDirectiveSyntax> Includes { get; }
    public ImmutableArray<MemberDeclarationSyntax> Members { get; }
    public SyntaxToken EndOfFileToken { get; }

    // Convenience
    public IEnumerable<ClassDeclarationSyntax> Classes =>
        Members.OfType<ClassDeclarationSyntax>();
    public IEnumerable<ActorDeclarationSyntax> Actors =>
        Members.OfType<ActorDeclarationSyntax>();
}

public sealed class VersionDirectiveSyntax : SyntaxNode
{
    public override SyntaxNodeKind Kind => SyntaxNodeKind.VersionDirective;

    public SyntaxToken VersionKeyword { get; }
    public SyntaxToken VersionString { get; }  // "4.10.0"
}

public sealed class IncludeDirectiveSyntax : SyntaxNode
{
    public override SyntaxNodeKind Kind => SyntaxNodeKind.IncludeDirective;

    public SyntaxToken HashToken { get; }
    public SyntaxToken IncludeKeyword { get; }
    public SyntaxToken PathString { get; }
}
```

### Class and Actor Declarations

```csharp
public abstract class MemberDeclarationSyntax : SyntaxNode { }

public sealed class ClassDeclarationSyntax : MemberDeclarationSyntax
{
    public override SyntaxNodeKind Kind => SyntaxNodeKind.ClassDeclaration;

    public ImmutableArray<SyntaxToken> Modifiers { get; }  // abstract, native, etc.
    public SyntaxToken ClassKeyword { get; }
    public SyntaxToken Identifier { get; }
    public BaseListSyntax? BaseList { get; }              // : BaseClass
    public SyntaxToken? ReplacesKeyword { get; }
    public SyntaxToken? ReplacesIdentifier { get; }
    public SyntaxToken? EditorNumber { get; }             // DoomEd number
    public SyntaxToken OpenBraceToken { get; }
    public ImmutableArray<MemberDeclarationSyntax> Members { get; }
    public SyntaxToken CloseBraceToken { get; }

    // Convenience properties
    public string Name => Identifier.Text;
    public string? BaseClassName => BaseList?.BaseType.ToString();
    public string? ReplacesClassName => ReplacesIdentifier?.Text;
    public int? DoomEdNumber => EditorNumber?.Value as int?;

    public DefaultBlockSyntax? DefaultBlock =>
        Members.OfType<DefaultBlockSyntax>().FirstOrDefault();
    public StatesBlockSyntax? StatesBlock =>
        Members.OfType<StatesBlockSyntax>().FirstOrDefault();
    public IEnumerable<MethodDeclarationSyntax> Methods =>
        Members.OfType<MethodDeclarationSyntax>();
    public IEnumerable<FieldDeclarationSyntax> Fields =>
        Members.OfType<FieldDeclarationSyntax>();
}

// DECORATE-style actor (legacy, but still widely used)
public sealed class ActorDeclarationSyntax : MemberDeclarationSyntax
{
    public override SyntaxNodeKind Kind => SyntaxNodeKind.ActorDeclaration;

    public SyntaxToken ActorKeyword { get; }
    public SyntaxToken Identifier { get; }
    public SyntaxToken? ColonToken { get; }
    public SyntaxToken? BaseIdentifier { get; }
    public SyntaxToken? ReplacesKeyword { get; }
    public SyntaxToken? ReplacesIdentifier { get; }
    public SyntaxToken? EditorNumber { get; }
    public SyntaxToken OpenBraceToken { get; }
    public ImmutableArray<ActorBodyItemSyntax> Body { get; }
    public SyntaxToken CloseBraceToken { get; }

    public string Name => Identifier.Text;
    public string? BaseName => BaseIdentifier?.Text;
    public string? ReplacesName => ReplacesIdentifier?.Text;
    public int? DoomEdNumber => EditorNumber?.Value as int?;
}

public abstract class ActorBodyItemSyntax : SyntaxNode { }

public sealed class BaseListSyntax : SyntaxNode
{
    public SyntaxToken ColonToken { get; }
    public TypeSyntax BaseType { get; }
}
```

### Default Block (Properties & Flags)

```csharp
public sealed class DefaultBlockSyntax : MemberDeclarationSyntax
{
    public override SyntaxNodeKind Kind => SyntaxNodeKind.DefaultBlock;

    public SyntaxToken DefaultKeyword { get; }
    public SyntaxToken OpenBraceToken { get; }
    public ImmutableArray<DefaultItemSyntax> Items { get; }
    public SyntaxToken CloseBraceToken { get; }

    public IEnumerable<PropertyAssignmentSyntax> Properties =>
        Items.OfType<PropertyAssignmentSyntax>();
    public IEnumerable<FlagDefinitionSyntax> Flags =>
        Items.OfType<FlagDefinitionSyntax>();
}

public abstract class DefaultItemSyntax : SyntaxNode { }

public sealed class PropertyAssignmentSyntax : DefaultItemSyntax
{
    public SyntaxToken? PrefixIdentifier { get; }  // Monster, Weapon, etc.
    public SyntaxToken? DotToken { get; }
    public SyntaxToken PropertyName { get; }
    public ImmutableArray<ExpressionSyntax> Values { get; }  // Can have multiple values
    public SyntaxToken SemicolonToken { get; }

    public string FullPropertyName => PrefixIdentifier != null
        ? $"{PrefixIdentifier.Text}.{PropertyName.Text}"
        : PropertyName.Text;
}

public sealed class FlagDefinitionSyntax : DefaultItemSyntax
{
    public override SyntaxNodeKind Kind => SyntaxNodeKind.FlagDefinition;

    public SyntaxToken PlusOrMinus { get; }  // + or -
    public SyntaxToken? PrefixIdentifier { get; }
    public SyntaxToken? DotToken { get; }
    public SyntaxToken FlagName { get; }
    public SyntaxToken? SemicolonToken { get; }

    public bool IsSet => PlusOrMinus.Kind == SyntaxTokenKind.Plus;
    public string FullFlagName => PrefixIdentifier != null
        ? $"{PrefixIdentifier.Text}.{FlagName.Text}"
        : FlagName.Text;
}
```

### States Block

```csharp
public sealed class StatesBlockSyntax : MemberDeclarationSyntax
{
    public override SyntaxNodeKind Kind => SyntaxNodeKind.StatesBlock;

    public SyntaxToken StatesKeyword { get; }
    public SyntaxToken? OpenParenToken { get; }
    public ImmutableArray<SyntaxToken> StateOptions { get; }  // Actor, Weapon, Overlay, etc.
    public SyntaxToken? CloseParenToken { get; }
    public SyntaxToken OpenBraceToken { get; }
    public ImmutableArray<StateSyntax> States { get; }
    public SyntaxToken CloseBraceToken { get; }

    public IEnumerable<StateLabelSyntax> Labels => States.OfType<StateLabelSyntax>();
    public IEnumerable<StateFrameSyntax> Frames => States.OfType<StateFrameSyntax>();
}

public abstract class StateSyntax : SyntaxNode { }

public sealed class StateLabelSyntax : StateSyntax
{
    public override SyntaxNodeKind Kind => SyntaxNodeKind.StateLabel;

    public SyntaxToken Identifier { get; }
    public SyntaxToken? DotToken { get; }
    public SyntaxToken? SubIdentifier { get; }
    public SyntaxToken ColonToken { get; }

    public string LabelName => SubIdentifier != null
        ? $"{Identifier.Text}.{SubIdentifier.Text}"
        : Identifier.Text;
}

public sealed class StateFrameSyntax : StateSyntax
{
    public override SyntaxNodeKind Kind => SyntaxNodeKind.StateFrame;

    public SyntaxToken SpriteIdentifier { get; }  // 4-char sprite name (e.g., "POSS")
    public SyntaxToken FrameLetters { get; }      // Frame letters (e.g., "ABCD")
    public SyntaxToken Duration { get; }          // Tic count or -1 for infinite
    public ImmutableArray<SyntaxToken> Modifiers { get; }  // Bright, Fast, Slow, NoDelay, CanRaise, Offset
    public StateActionSyntax? Action { get; }

    public string SpriteName => SpriteIdentifier.Text;
    public string Frames => FrameLetters.Text;
    public int DurationTics => (int)(Duration.Value ?? -1);
    public bool IsBright => Modifiers.Any(m => m.Text.Equals("Bright", StringComparison.OrdinalIgnoreCase));
}

public sealed class StateActionSyntax : SyntaxNode
{
    public override SyntaxNodeKind Kind => SyntaxNodeKind.StateAction;

    // Can be simple: A_Chase
    // Or block: { A_Chase(); A_Look(); }
    public SyntaxToken? ActionIdentifier { get; }
    public SyntaxToken? OpenParenToken { get; }
    public ImmutableArray<ArgumentSyntax> Arguments { get; }
    public SyntaxToken? CloseParenToken { get; }

    // Or anonymous action block
    public BlockStatementSyntax? ActionBlock { get; }

    public bool IsAnonymousBlock => ActionBlock != null;
}

public sealed class StateGotoSyntax : StateSyntax
{
    public override SyntaxNodeKind Kind => SyntaxNodeKind.StateGoto;

    public SyntaxToken GotoKeyword { get; }
    public SyntaxToken? ClassIdentifier { get; }
    public SyntaxToken? ColonColonToken { get; }
    public SyntaxToken LabelIdentifier { get; }
    public SyntaxToken? PlusToken { get; }
    public SyntaxToken? Offset { get; }

    public string TargetLabel => ClassIdentifier != null
        ? $"{ClassIdentifier.Text}::{LabelIdentifier.Text}"
        : LabelIdentifier.Text;
    public int FrameOffset => Offset?.Value as int? ?? 0;
}

public sealed class StateStopSyntax : StateSyntax
{
    public override SyntaxNodeKind Kind => SyntaxNodeKind.StateStop;
    public SyntaxToken StopKeyword { get; }
}

public sealed class StateWaitSyntax : StateSyntax
{
    public override SyntaxNodeKind Kind => SyntaxNodeKind.StateWait;
    public SyntaxToken WaitKeyword { get; }
}

public sealed class StateLoopSyntax : StateSyntax
{
    public override SyntaxNodeKind Kind => SyntaxNodeKind.StateLoop;
    public SyntaxToken LoopKeyword { get; }
}

public sealed class StateFailSyntax : StateSyntax
{
    public override SyntaxNodeKind Kind => SyntaxNodeKind.StateFail;
    public SyntaxToken FailKeyword { get; }
}
```

### Methods and Fields

```csharp
public sealed class MethodDeclarationSyntax : MemberDeclarationSyntax
{
    public override SyntaxNodeKind Kind => SyntaxNodeKind.MethodDeclaration;

    public ImmutableArray<SyntaxToken> Modifiers { get; }  // virtual, override, action, etc.
    public TypeSyntax ReturnType { get; }
    public SyntaxToken Identifier { get; }
    public SyntaxToken OpenParenToken { get; }
    public SeparatedSyntaxList<ParameterSyntax> Parameters { get; }
    public SyntaxToken CloseParenToken { get; }
    public SyntaxToken? ConstKeyword { get; }
    public BlockStatementSyntax? Body { get; }
    public SyntaxToken? SemicolonToken { get; }  // For native/abstract methods

    public string Name => Identifier.Text;
    public bool IsVirtual => Modifiers.Any(m => m.Kind == SyntaxTokenKind.VirtualKeyword);
    public bool IsOverride => Modifiers.Any(m => m.Kind == SyntaxTokenKind.OverrideKeyword);
    public bool IsNative => Modifiers.Any(m => m.Kind == SyntaxTokenKind.NativeKeyword);
    public bool IsAction => Modifiers.Any(m => m.Text.Equals("action", StringComparison.OrdinalIgnoreCase));
}

public sealed class FieldDeclarationSyntax : MemberDeclarationSyntax
{
    public override SyntaxNodeKind Kind => SyntaxNodeKind.FieldDeclaration;

    public ImmutableArray<SyntaxToken> Modifiers { get; }
    public TypeSyntax Type { get; }
    public SeparatedSyntaxList<VariableDeclaratorSyntax> Variables { get; }
    public SyntaxToken SemicolonToken { get; }
}

public sealed class ParameterSyntax : SyntaxNode
{
    public override SyntaxNodeKind Kind => SyntaxNodeKind.Parameter;

    public ImmutableArray<SyntaxToken> Modifiers { get; }  // in, out, etc.
    public TypeSyntax Type { get; }
    public SyntaxToken Identifier { get; }
    public SyntaxToken? EqualsToken { get; }
    public ExpressionSyntax? DefaultValue { get; }
}
```

### Types and Expressions

```csharp
public abstract class TypeSyntax : SyntaxNode { }

public sealed class PredefinedTypeSyntax : TypeSyntax
{
    public override SyntaxNodeKind Kind => SyntaxNodeKind.PredefinedType;
    public SyntaxToken Keyword { get; }  // int, float, string, etc.
}

public sealed class NamedTypeSyntax : TypeSyntax
{
    public override SyntaxNodeKind Kind => SyntaxNodeKind.NamedType;
    public SyntaxToken Identifier { get; }
    public TypeArgumentListSyntax? TypeArguments { get; }
}

public sealed class ArrayTypeSyntax : TypeSyntax
{
    public override SyntaxNodeKind Kind => SyntaxNodeKind.ArrayType;
    public SyntaxToken ArrayKeyword { get; }
    public SyntaxToken LessThanToken { get; }
    public TypeSyntax ElementType { get; }
    public SyntaxToken GreaterThanToken { get; }
}

public sealed class ClassTypeSyntax : TypeSyntax
{
    public override SyntaxNodeKind Kind => SyntaxNodeKind.ClassType;
    public SyntaxToken ClassKeyword { get; }
    public SyntaxToken LessThanToken { get; }
    public TypeSyntax ConstraintType { get; }
    public SyntaxToken GreaterThanToken { get; }
}

public abstract class ExpressionSyntax : SyntaxNode { }

public sealed class LiteralExpressionSyntax : ExpressionSyntax
{
    public override SyntaxNodeKind Kind => SyntaxNodeKind.LiteralExpression;
    public SyntaxToken Token { get; }
    public object? Value => Token.Value;
}

public sealed class IdentifierExpressionSyntax : ExpressionSyntax
{
    public override SyntaxNodeKind Kind => SyntaxNodeKind.IdentifierExpression;
    public SyntaxToken Identifier { get; }
}

public sealed class MemberAccessExpressionSyntax : ExpressionSyntax
{
    public override SyntaxNodeKind Kind => SyntaxNodeKind.MemberAccessExpression;
    public ExpressionSyntax Expression { get; }
    public SyntaxToken DotToken { get; }
    public SyntaxToken Name { get; }
}

public sealed class InvocationExpressionSyntax : ExpressionSyntax
{
    public override SyntaxNodeKind Kind => SyntaxNodeKind.InvocationExpression;
    public ExpressionSyntax Expression { get; }
    public SyntaxToken OpenParenToken { get; }
    public SeparatedSyntaxList<ArgumentSyntax> Arguments { get; }
    public SyntaxToken CloseParenToken { get; }
}

public sealed class BinaryExpressionSyntax : ExpressionSyntax
{
    public override SyntaxNodeKind Kind => SyntaxNodeKind.BinaryExpression;
    public ExpressionSyntax Left { get; }
    public SyntaxToken OperatorToken { get; }
    public ExpressionSyntax Right { get; }
}

public sealed class UnaryExpressionSyntax : ExpressionSyntax
{
    public override SyntaxNodeKind Kind => SyntaxNodeKind.UnaryExpression;
    public SyntaxToken OperatorToken { get; }
    public ExpressionSyntax Operand { get; }
}

public sealed class ArgumentSyntax : SyntaxNode
{
    public override SyntaxNodeKind Kind => SyntaxNodeKind.Argument;
    public SyntaxToken? NameColon { get; }  // Named argument
    public ExpressionSyntax Expression { get; }
}
```

### Statements

```csharp
public abstract class StatementSyntax : SyntaxNode { }

public sealed class BlockStatementSyntax : StatementSyntax
{
    public override SyntaxNodeKind Kind => SyntaxNodeKind.BlockStatement;
    public SyntaxToken OpenBraceToken { get; }
    public ImmutableArray<StatementSyntax> Statements { get; }
    public SyntaxToken CloseBraceToken { get; }
}

public sealed class ExpressionStatementSyntax : StatementSyntax
{
    public override SyntaxNodeKind Kind => SyntaxNodeKind.ExpressionStatement;
    public ExpressionSyntax Expression { get; }
    public SyntaxToken SemicolonToken { get; }
}

public sealed class IfStatementSyntax : StatementSyntax
{
    public override SyntaxNodeKind Kind => SyntaxNodeKind.IfStatement;
    public SyntaxToken IfKeyword { get; }
    public SyntaxToken OpenParenToken { get; }
    public ExpressionSyntax Condition { get; }
    public SyntaxToken CloseParenToken { get; }
    public StatementSyntax Statement { get; }
    public SyntaxToken? ElseKeyword { get; }
    public StatementSyntax? ElseStatement { get; }
}

public sealed class ReturnStatementSyntax : StatementSyntax
{
    public override SyntaxNodeKind Kind => SyntaxNodeKind.ReturnStatement;
    public SyntaxToken ReturnKeyword { get; }
    public ExpressionSyntax? Expression { get; }
    public SyntaxToken SemicolonToken { get; }
}

public sealed class LocalDeclarationStatementSyntax : StatementSyntax
{
    public override SyntaxNodeKind Kind => SyntaxNodeKind.LocalDeclarationStatement;
    public SyntaxToken? LetKeyword { get; }
    public TypeSyntax? Type { get; }
    public SeparatedSyntaxList<VariableDeclaratorSyntax> Variables { get; }
    public SyntaxToken SemicolonToken { get; }
}

public sealed class VariableDeclaratorSyntax : SyntaxNode
{
    public SyntaxToken Identifier { get; }
    public SyntaxToken? EqualsToken { get; }
    public ExpressionSyntax? Initializer { get; }
}
```

---

## Task 8.3: Lexer Implementation

```csharp
namespace WAD.NET.ZScript.Syntax;

public sealed class Lexer
{
    private readonly string _text;
    private int _position;
    private int _start;
    private SyntaxTokenKind _kind;
    private object? _value;

    private readonly List<SyntaxTrivia> _leadingTrivia = new();
    private readonly List<SyntaxTrivia> _trailingTrivia = new();

    public Lexer(string text)
    {
        _text = text;
    }

    private char Current => Peek(0);
    private char Lookahead => Peek(1);

    private char Peek(int offset)
    {
        var index = _position + offset;
        return index >= _text.Length ? '\0' : _text[index];
    }

    public SyntaxToken Lex()
    {
        ReadTrivia(isLeading: true);
        var leadingTrivia = _leadingTrivia.ToImmutableArray();
        _leadingTrivia.Clear();

        _start = _position;
        ReadToken();
        var text = _text[_start.._position];
        var span = new TextSpan(_start, _position - _start);

        ReadTrivia(isLeading: false);
        var trailingTrivia = _trailingTrivia.ToImmutableArray();
        _trailingTrivia.Clear();

        return new SyntaxToken(
            _kind,
            text,
            span,
            new SyntaxTriviaList(leadingTrivia),
            new SyntaxTriviaList(trailingTrivia),
            _value);
    }

    private void ReadTrivia(bool isLeading)
    {
        var list = isLeading ? _leadingTrivia : _trailingTrivia;

        while (true)
        {
            _start = _position;
            _kind = SyntaxTriviaKind.None;

            switch (Current)
            {
                case '\0':
                    return;

                case '\r':
                case '\n':
                    if (!isLeading) return;  // End of line ends trailing trivia
                    ReadEndOfLine();
                    break;

                case ' ':
                case '\t':
                    ReadWhitespace();
                    break;

                case '/':
                    if (Lookahead == '/')
                        ReadSingleLineComment();
                    else if (Lookahead == '*')
                        ReadMultiLineComment();
                    else
                        return;
                    break;

                case '#':
                    // Could be preprocessor directive
                    if (isLeading && IsPreprocessorDirective())
                        ReadPreprocessorDirective();
                    else
                        return;
                    break;

                default:
                    return;
            }

            var text = _text[_start.._position];
            list.Add(new SyntaxTrivia(_kind, text, new TextSpan(_start, text.Length)));
        }
    }

    private void ReadToken()
    {
        _value = null;

        switch (Current)
        {
            case '\0':
                _kind = SyntaxTokenKind.EndOfFile;
                break;

            case '+':
                if (Lookahead == '+') { _position += 2; _kind = SyntaxTokenKind.PlusPlus; }
                else if (Lookahead == '=') { _position += 2; _kind = SyntaxTokenKind.PlusEquals; }
                else { _position++; _kind = SyntaxTokenKind.Plus; }
                break;

            case '-':
                if (Lookahead == '-') { _position += 2; _kind = SyntaxTokenKind.MinusMinus; }
                else if (Lookahead == '=') { _position += 2; _kind = SyntaxTokenKind.MinusEquals; }
                else if (Lookahead == '>') { _position += 2; _kind = SyntaxTokenKind.Arrow; }
                else { _position++; _kind = SyntaxTokenKind.Minus; }
                break;

            case '*':
                if (Lookahead == '=') { _position += 2; _kind = SyntaxTokenKind.AsteriskEquals; }
                else { _position++; _kind = SyntaxTokenKind.Asterisk; }
                break;

            case '/':
                if (Lookahead == '=') { _position += 2; _kind = SyntaxTokenKind.SlashEquals; }
                else { _position++; _kind = SyntaxTokenKind.Slash; }
                break;

            case '(':
                _position++;
                _kind = SyntaxTokenKind.OpenParen;
                break;

            case ')':
                _position++;
                _kind = SyntaxTokenKind.CloseParen;
                break;

            case '{':
                _position++;
                _kind = SyntaxTokenKind.OpenBrace;
                break;

            case '}':
                _position++;
                _kind = SyntaxTokenKind.CloseBrace;
                break;

            case '[':
                _position++;
                _kind = SyntaxTokenKind.OpenBracket;
                break;

            case ']':
                _position++;
                _kind = SyntaxTokenKind.CloseBracket;
                break;

            case ';':
                _position++;
                _kind = SyntaxTokenKind.Semicolon;
                break;

            case ':':
                if (Lookahead == ':') { _position += 2; _kind = SyntaxTokenKind.ColonColon; }
                else { _position++; _kind = SyntaxTokenKind.Colon; }
                break;

            case ',':
                _position++;
                _kind = SyntaxTokenKind.Comma;
                break;

            case '.':
                if (Lookahead == '.') { _position += 2; _kind = SyntaxTokenKind.DotDot; }
                else if (char.IsDigit(Lookahead)) ReadNumber();
                else { _position++; _kind = SyntaxTokenKind.Dot; }
                break;

            case '=':
                if (Lookahead == '=') { _position += 2; _kind = SyntaxTokenKind.EqualsEquals; }
                else { _position++; _kind = SyntaxTokenKind.Equals; }
                break;

            case '<':
                if (Lookahead == '=') { _position += 2; _kind = SyntaxTokenKind.LessThanEquals; }
                else if (Lookahead == '<') { _position += 2; _kind = SyntaxTokenKind.LessThanLessThan; }
                else { _position++; _kind = SyntaxTokenKind.LessThan; }
                break;

            case '>':
                if (Lookahead == '=') { _position += 2; _kind = SyntaxTokenKind.GreaterThanEquals; }
                else if (Lookahead == '>')
                {
                    if (Peek(2) == '>') { _position += 3; _kind = SyntaxTokenKind.GreaterThanGreaterThanGreaterThan; }
                    else { _position += 2; _kind = SyntaxTokenKind.GreaterThanGreaterThan; }
                }
                else { _position++; _kind = SyntaxTokenKind.GreaterThan; }
                break;

            case '!':
                if (Lookahead == '=') { _position += 2; _kind = SyntaxTokenKind.ExclamationEquals; }
                else { _position++; _kind = SyntaxTokenKind.Exclamation; }
                break;

            case '&':
                if (Lookahead == '&') { _position += 2; _kind = SyntaxTokenKind.AmpersandAmpersand; }
                else if (Lookahead == '=') { _position += 2; _kind = SyntaxTokenKind.AmpersandEquals; }
                else { _position++; _kind = SyntaxTokenKind.Ampersand; }
                break;

            case '|':
                if (Lookahead == '|') { _position += 2; _kind = SyntaxTokenKind.PipePipe; }
                else if (Lookahead == '=') { _position += 2; _kind = SyntaxTokenKind.PipeEquals; }
                else { _position++; _kind = SyntaxTokenKind.Pipe; }
                break;

            case '^':
                if (Lookahead == '=') { _position += 2; _kind = SyntaxTokenKind.CaretEquals; }
                else { _position++; _kind = SyntaxTokenKind.Caret; }
                break;

            case '~':
                if (Lookahead == '=' && Peek(2) == '=') { _position += 3; _kind = SyntaxTokenKind.ApproxEquals; }
                else { _position++; _kind = SyntaxTokenKind.Tilde; }
                break;

            case '?':
                _position++;
                _kind = SyntaxTokenKind.Question;
                break;

            case '#':
                _position++;
                _kind = SyntaxTokenKind.Hash;
                break;

            case '"':
                ReadStringLiteral();
                break;

            case '\'':
                ReadNameLiteral();
                break;

            case '0': case '1': case '2': case '3': case '4':
            case '5': case '6': case '7': case '8': case '9':
                ReadNumber();
                break;

            default:
                if (char.IsLetter(Current) || Current == '_')
                    ReadIdentifierOrKeyword();
                else
                {
                    _position++;
                    _kind = SyntaxTokenKind.BadToken;
                }
                break;
        }
    }

    private void ReadNumber()
    {
        bool isFloat = Current == '.';
        bool isHex = Current == '0' && (Lookahead == 'x' || Lookahead == 'X');

        if (isHex)
        {
            _position += 2;
            while (char.IsAsciiHexDigit(Current))
                _position++;
            _kind = SyntaxTokenKind.IntegerLiteral;
            _value = Convert.ToInt32(_text[(_start + 2).._position], 16);
            return;
        }

        while (char.IsDigit(Current))
            _position++;

        if (Current == '.' && char.IsDigit(Lookahead))
        {
            isFloat = true;
            _position++;
            while (char.IsDigit(Current))
                _position++;
        }

        if (Current == 'e' || Current == 'E')
        {
            isFloat = true;
            _position++;
            if (Current == '+' || Current == '-')
                _position++;
            while (char.IsDigit(Current))
                _position++;
        }

        var text = _text[_start.._position];
        if (isFloat)
        {
            _kind = SyntaxTokenKind.FloatLiteral;
            _value = double.Parse(text, CultureInfo.InvariantCulture);
        }
        else
        {
            _kind = SyntaxTokenKind.IntegerLiteral;
            _value = int.Parse(text);
        }
    }

    private void ReadStringLiteral()
    {
        _position++;  // Opening "
        var builder = new StringBuilder();

        while (Current != '"' && Current != '\0')
        {
            if (Current == '\\')
            {
                _position++;
                builder.Append(Current switch
                {
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    '\\' => '\\',
                    '"' => '"',
                    _ => Current
                });
            }
            else
            {
                builder.Append(Current);
            }
            _position++;
        }

        if (Current == '"')
            _position++;  // Closing "

        _kind = SyntaxTokenKind.StringLiteral;
        _value = builder.ToString();
    }

    private void ReadNameLiteral()
    {
        _position++;  // Opening '
        var start = _position;

        while (Current != '\'' && Current != '\0')
            _position++;

        _value = _text[start.._position];

        if (Current == '\'')
            _position++;  // Closing '

        _kind = SyntaxTokenKind.NameLiteral;
    }

    private void ReadIdentifierOrKeyword()
    {
        while (char.IsLetterOrDigit(Current) || Current == '_')
            _position++;

        var text = _text[_start.._position];
        _kind = GetKeywordKind(text);
    }

    private static SyntaxTokenKind GetKeywordKind(string text)
    {
        return text.ToLowerInvariant() switch
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
            "vector2" or "vector3" => SyntaxTokenKind.VectorKeyword,
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
            _ => SyntaxTokenKind.Identifier
        };
    }

    private void ReadWhitespace()
    {
        while (Current == ' ' || Current == '\t')
            _position++;
        _kind = SyntaxTriviaKind.Whitespace;
    }

    private void ReadEndOfLine()
    {
        if (Current == '\r' && Lookahead == '\n')
            _position += 2;
        else
            _position++;
        _kind = SyntaxTriviaKind.EndOfLine;
    }

    private void ReadSingleLineComment()
    {
        _position += 2;  // Skip //
        while (Current != '\r' && Current != '\n' && Current != '\0')
            _position++;
        _kind = SyntaxTriviaKind.SingleLineComment;
    }

    private void ReadMultiLineComment()
    {
        _position += 2;  // Skip /*
        while (!(Current == '*' && Lookahead == '/') && Current != '\0')
            _position++;
        if (Current != '\0')
            _position += 2;  // Skip */
        _kind = SyntaxTriviaKind.MultiLineComment;
    }
}
```

---

## Task 8.4: Parser Implementation

```csharp
namespace WAD.NET.ZScript.Syntax;

public sealed class Parser
{
    private readonly Lexer _lexer;
    private readonly List<Diagnostic> _diagnostics = new();
    private SyntaxToken _current;

    public Parser(string text)
    {
        _lexer = new Lexer(text);
        _current = _lexer.Lex();
    }

    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics;

    private SyntaxToken Current => _current;

    private SyntaxToken NextToken()
    {
        var current = _current;
        _current = _lexer.Lex();
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

        // Return a missing token for error recovery
        return new SyntaxToken(kind, "", Current.Span,
            SyntaxTriviaList.Empty, SyntaxTriviaList.Empty,
            null, isMissing: true);
    }

    private SyntaxToken? MatchOptional(SyntaxTokenKind kind)
    {
        if (Current.Kind == kind)
            return NextToken();
        return null;
    }

    public CompilationUnitSyntax ParseCompilationUnit()
    {
        var version = ParseVersionDirective();
        var includes = ParseIncludes();
        var members = ParseMembers();
        var endOfFile = Match(SyntaxTokenKind.EndOfFile);

        return new CompilationUnitSyntax(version, includes, members, endOfFile);
    }

    private VersionDirectiveSyntax? ParseVersionDirective()
    {
        if (Current.Kind != SyntaxTokenKind.VersionKeyword)
            return null;

        var versionKeyword = NextToken();
        var versionString = Match(SyntaxTokenKind.StringLiteral);

        return new VersionDirectiveSyntax(versionKeyword, versionString);
    }

    private ImmutableArray<IncludeDirectiveSyntax> ParseIncludes()
    {
        var includes = ImmutableArray.CreateBuilder<IncludeDirectiveSyntax>();

        while (Current.Kind == SyntaxTokenKind.Hash)
        {
            var hash = NextToken();
            var includeKeyword = Match(SyntaxTokenKind.Identifier);  // "include"
            var path = Match(SyntaxTokenKind.StringLiteral);

            includes.Add(new IncludeDirectiveSyntax(hash, includeKeyword, path));
        }

        return includes.ToImmutable();
    }

    private ImmutableArray<MemberDeclarationSyntax> ParseMembers()
    {
        var members = ImmutableArray.CreateBuilder<MemberDeclarationSyntax>();

        while (Current.Kind != SyntaxTokenKind.EndOfFile)
        {
            var member = ParseMemberDeclaration();
            if (member != null)
                members.Add(member);
        }

        return members.ToImmutable();
    }

    private MemberDeclarationSyntax? ParseMemberDeclaration()
    {
        // Collect modifiers
        var modifiers = ParseModifiers();

        return Current.Kind switch
        {
            SyntaxTokenKind.ClassKeyword => ParseClassDeclaration(modifiers),
            SyntaxTokenKind.StructKeyword => ParseStructDeclaration(modifiers),
            SyntaxTokenKind.EnumKeyword => ParseEnumDeclaration(modifiers),
            SyntaxTokenKind.ActorKeyword => ParseActorDeclaration(),
            SyntaxTokenKind.ConstKeyword => ParseConstDeclaration(modifiers),
            _ => null
        };
    }

    private ImmutableArray<SyntaxToken> ParseModifiers()
    {
        var modifiers = ImmutableArray.CreateBuilder<SyntaxToken>();

        while (IsModifier(Current.Kind))
        {
            modifiers.Add(NextToken());
        }

        return modifiers.ToImmutable();
    }

    private static bool IsModifier(SyntaxTokenKind kind) => kind is
        SyntaxTokenKind.AbstractKeyword or
        SyntaxTokenKind.VirtualKeyword or
        SyntaxTokenKind.OverrideKeyword or
        SyntaxTokenKind.FinalKeyword or
        SyntaxTokenKind.NativeKeyword or
        SyntaxTokenKind.PrivateKeyword or
        SyntaxTokenKind.ProtectedKeyword or
        SyntaxTokenKind.StaticKeyword or
        SyntaxTokenKind.ReadOnlyKeyword or
        SyntaxTokenKind.DeprecatedKeyword;

    private ClassDeclarationSyntax ParseClassDeclaration(ImmutableArray<SyntaxToken> modifiers)
    {
        var classKeyword = Match(SyntaxTokenKind.ClassKeyword);
        var identifier = Match(SyntaxTokenKind.Identifier);

        var baseList = ParseBaseList();
        var replacesKeyword = MatchOptional(SyntaxTokenKind.ReplacesKeyword);
        var replacesIdentifier = replacesKeyword != null
            ? Match(SyntaxTokenKind.Identifier)
            : null;
        var editorNumber = MatchOptional(SyntaxTokenKind.IntegerLiteral);

        var openBrace = Match(SyntaxTokenKind.OpenBrace);
        var members = ParseClassMembers();
        var closeBrace = Match(SyntaxTokenKind.CloseBrace);

        return new ClassDeclarationSyntax(
            modifiers, classKeyword, identifier, baseList,
            replacesKeyword, replacesIdentifier, editorNumber,
            openBrace, members, closeBrace);
    }

    private BaseListSyntax? ParseBaseList()
    {
        if (Current.Kind != SyntaxTokenKind.Colon)
            return null;

        var colon = NextToken();
        var baseType = ParseType();

        return new BaseListSyntax(colon, baseType);
    }

    private ImmutableArray<MemberDeclarationSyntax> ParseClassMembers()
    {
        var members = ImmutableArray.CreateBuilder<MemberDeclarationSyntax>();

        while (Current.Kind != SyntaxTokenKind.CloseBrace &&
               Current.Kind != SyntaxTokenKind.EndOfFile)
        {
            var modifiers = ParseModifiers();

            MemberDeclarationSyntax? member = Current.Kind switch
            {
                SyntaxTokenKind.DefaultKeyword => ParseDefaultBlock(),
                SyntaxTokenKind.StatesKeyword => ParseStatesBlock(),
                _ when IsTypeStart() => ParseMethodOrField(modifiers),
                _ => null
            };

            if (member != null)
                members.Add(member);
            else
                NextToken();  // Skip unrecognized token
        }

        return members.ToImmutable();
    }

    private DefaultBlockSyntax ParseDefaultBlock()
    {
        var defaultKeyword = Match(SyntaxTokenKind.DefaultKeyword);
        var openBrace = Match(SyntaxTokenKind.OpenBrace);
        var items = ParseDefaultItems();
        var closeBrace = Match(SyntaxTokenKind.CloseBrace);

        return new DefaultBlockSyntax(defaultKeyword, openBrace, items, closeBrace);
    }

    private ImmutableArray<DefaultItemSyntax> ParseDefaultItems()
    {
        var items = ImmutableArray.CreateBuilder<DefaultItemSyntax>();

        while (Current.Kind != SyntaxTokenKind.CloseBrace &&
               Current.Kind != SyntaxTokenKind.EndOfFile)
        {
            if (Current.Kind == SyntaxTokenKind.Plus || Current.Kind == SyntaxTokenKind.Minus)
            {
                items.Add(ParseFlagDefinition());
            }
            else if (Current.Kind == SyntaxTokenKind.Identifier)
            {
                items.Add(ParsePropertyAssignment());
            }
            else
            {
                NextToken();  // Skip unrecognized
            }
        }

        return items.ToImmutable();
    }

    private FlagDefinitionSyntax ParseFlagDefinition()
    {
        var plusOrMinus = NextToken();  // + or -
        var prefix = MatchOptional(SyntaxTokenKind.Identifier);
        var dot = prefix != null ? MatchOptional(SyntaxTokenKind.Dot) : null;
        var flagName = dot != null || prefix == null
            ? Match(SyntaxTokenKind.Identifier)
            : prefix.Value;
        var semicolon = MatchOptional(SyntaxTokenKind.Semicolon);

        // Adjust if no dot - the "prefix" was actually the flag name
        if (dot == null && prefix != null)
        {
            return new FlagDefinitionSyntax(plusOrMinus, null, null, prefix.Value, semicolon);
        }

        return new FlagDefinitionSyntax(plusOrMinus, prefix, dot, flagName, semicolon);
    }

    private PropertyAssignmentSyntax ParsePropertyAssignment()
    {
        SyntaxToken? prefix = null;
        SyntaxToken? dot = null;

        var first = Match(SyntaxTokenKind.Identifier);

        if (Current.Kind == SyntaxTokenKind.Dot)
        {
            prefix = first;
            dot = NextToken();
            first = Match(SyntaxTokenKind.Identifier);
        }

        var values = ParsePropertyValues();
        var semicolon = Match(SyntaxTokenKind.Semicolon);

        return new PropertyAssignmentSyntax(prefix, dot, first, values, semicolon);
    }

    private ImmutableArray<ExpressionSyntax> ParsePropertyValues()
    {
        var values = ImmutableArray.CreateBuilder<ExpressionSyntax>();

        while (Current.Kind != SyntaxTokenKind.Semicolon &&
               Current.Kind != SyntaxTokenKind.CloseBrace &&
               Current.Kind != SyntaxTokenKind.EndOfFile)
        {
            values.Add(ParseExpression());

            if (Current.Kind == SyntaxTokenKind.Comma)
                NextToken();
        }

        return values.ToImmutable();
    }

    private StatesBlockSyntax ParseStatesBlock()
    {
        var statesKeyword = Match(SyntaxTokenKind.StatesKeyword);

        // Optional state options: States(Actor, Weapon, Overlay)
        var openParen = MatchOptional(SyntaxTokenKind.OpenParen);
        var options = ImmutableArray<SyntaxToken>.Empty;
        SyntaxToken? closeParen = null;

        if (openParen != null)
        {
            var optionsBuilder = ImmutableArray.CreateBuilder<SyntaxToken>();
            while (Current.Kind != SyntaxTokenKind.CloseParen &&
                   Current.Kind != SyntaxTokenKind.EndOfFile)
            {
                optionsBuilder.Add(Match(SyntaxTokenKind.Identifier));
                if (Current.Kind == SyntaxTokenKind.Comma)
                    NextToken();
            }
            options = optionsBuilder.ToImmutable();
            closeParen = Match(SyntaxTokenKind.CloseParen);
        }

        var openBrace = Match(SyntaxTokenKind.OpenBrace);
        var states = ParseStates();
        var closeBrace = Match(SyntaxTokenKind.CloseBrace);

        return new StatesBlockSyntax(
            statesKeyword, openParen, options, closeParen,
            openBrace, states, closeBrace);
    }

    private ImmutableArray<StateSyntax> ParseStates()
    {
        var states = ImmutableArray.CreateBuilder<StateSyntax>();

        while (Current.Kind != SyntaxTokenKind.CloseBrace &&
               Current.Kind != SyntaxTokenKind.EndOfFile)
        {
            var state = ParseState();
            if (state != null)
                states.Add(state);
        }

        return states.ToImmutable();
    }

    private StateSyntax? ParseState()
    {
        // State label: "Spawn:" or "See.Fast:"
        if (Current.Kind == SyntaxTokenKind.Identifier && PeekAhead(SyntaxTokenKind.Colon))
        {
            return ParseStateLabel();
        }

        // Control flow keywords
        return Current.Kind switch
        {
            SyntaxTokenKind.GotoKeyword => ParseStateGoto(),
            SyntaxTokenKind.StopKeyword => new StateStopSyntax(NextToken()),
            SyntaxTokenKind.WaitKeyword => new StateWaitSyntax(NextToken()),
            SyntaxTokenKind.LoopKeyword => new StateLoopSyntax(NextToken()),
            SyntaxTokenKind.FailKeyword => new StateFailSyntax(NextToken()),
            SyntaxTokenKind.Identifier => ParseStateFrame(),
            _ => null
        };
    }

    private StateLabelSyntax ParseStateLabel()
    {
        var identifier = Match(SyntaxTokenKind.Identifier);
        SyntaxToken? dot = null;
        SyntaxToken? subIdentifier = null;

        if (Current.Kind == SyntaxTokenKind.Dot)
        {
            dot = NextToken();
            subIdentifier = Match(SyntaxTokenKind.Identifier);
        }

        var colon = Match(SyntaxTokenKind.Colon);

        return new StateLabelSyntax(identifier, dot, subIdentifier, colon);
    }

    private StateFrameSyntax ParseStateFrame()
    {
        var sprite = Match(SyntaxTokenKind.Identifier);  // POSS, SARG, etc.
        var frames = Match(SyntaxTokenKind.Identifier);  // ABCD
        var duration = Match(SyntaxTokenKind.IntegerLiteral);

        var modifiers = ImmutableArray.CreateBuilder<SyntaxToken>();
        while (IsFrameModifier(Current))
        {
            modifiers.Add(NextToken());
        }

        StateActionSyntax? action = null;
        if (Current.Kind == SyntaxTokenKind.Identifier ||
            Current.Kind == SyntaxTokenKind.OpenBrace)
        {
            action = ParseStateAction();
        }

        return new StateFrameSyntax(sprite, frames, duration, modifiers.ToImmutable(), action);
    }

    private bool IsFrameModifier(SyntaxToken token)
    {
        var text = token.Text.ToLowerInvariant();
        return text is "bright" or "fast" or "slow" or "nodelay" or "canraise" or "offset";
    }

    private StateActionSyntax ParseStateAction()
    {
        if (Current.Kind == SyntaxTokenKind.OpenBrace)
        {
            var block = ParseBlockStatement();
            return new StateActionSyntax(null, null, ImmutableArray<ArgumentSyntax>.Empty, null, block);
        }

        var actionId = Match(SyntaxTokenKind.Identifier);
        SyntaxToken? openParen = null;
        var args = ImmutableArray<ArgumentSyntax>.Empty;
        SyntaxToken? closeParen = null;

        if (Current.Kind == SyntaxTokenKind.OpenParen)
        {
            openParen = NextToken();
            args = ParseArguments();
            closeParen = Match(SyntaxTokenKind.CloseParen);
        }

        return new StateActionSyntax(actionId, openParen, args, closeParen, null);
    }

    private StateGotoSyntax ParseStateGoto()
    {
        var gotoKeyword = Match(SyntaxTokenKind.GotoKeyword);

        SyntaxToken? classId = null;
        SyntaxToken? colonColon = null;

        var first = Match(SyntaxTokenKind.Identifier);

        if (Current.Kind == SyntaxTokenKind.ColonColon)
        {
            classId = first;
            colonColon = NextToken();
            first = Match(SyntaxTokenKind.Identifier);
        }

        SyntaxToken? plus = null;
        SyntaxToken? offset = null;

        if (Current.Kind == SyntaxTokenKind.Plus)
        {
            plus = NextToken();
            offset = Match(SyntaxTokenKind.IntegerLiteral);
        }

        return new StateGotoSyntax(gotoKeyword, classId, colonColon, first, plus, offset);
    }

    // Expression parsing using precedence climbing
    private ExpressionSyntax ParseExpression(int precedence = 0)
    {
        var left = ParsePrimaryExpression();

        while (true)
        {
            var opPrecedence = GetBinaryOperatorPrecedence(Current.Kind);
            if (opPrecedence == 0 || opPrecedence <= precedence)
                break;

            var op = NextToken();
            var right = ParseExpression(opPrecedence);
            left = new BinaryExpressionSyntax(left, op, right);
        }

        return left;
    }

    private ExpressionSyntax ParsePrimaryExpression()
    {
        // Unary operators
        if (IsUnaryOperator(Current.Kind))
        {
            var op = NextToken();
            var operand = ParsePrimaryExpression();
            return new UnaryExpressionSyntax(op, operand);
        }

        ExpressionSyntax left = Current.Kind switch
        {
            SyntaxTokenKind.IntegerLiteral or
            SyntaxTokenKind.FloatLiteral or
            SyntaxTokenKind.StringLiteral or
            SyntaxTokenKind.NameLiteral or
            SyntaxTokenKind.TrueKeyword or
            SyntaxTokenKind.FalseKeyword or
            SyntaxTokenKind.NullKeyword =>
                new LiteralExpressionSyntax(NextToken()),

            SyntaxTokenKind.Identifier =>
                new IdentifierExpressionSyntax(NextToken()),

            SyntaxTokenKind.OpenParen => ParseParenthesizedExpression(),

            _ => new LiteralExpressionSyntax(NextToken())  // Error recovery
        };

        // Postfix operations
        while (true)
        {
            if (Current.Kind == SyntaxTokenKind.Dot)
            {
                var dot = NextToken();
                var name = Match(SyntaxTokenKind.Identifier);
                left = new MemberAccessExpressionSyntax(left, dot, name);
            }
            else if (Current.Kind == SyntaxTokenKind.OpenParen)
            {
                var openParen = NextToken();
                var args = ParseArguments();
                var closeParen = Match(SyntaxTokenKind.CloseParen);
                left = new InvocationExpressionSyntax(left, openParen,
                    new SeparatedSyntaxList<ArgumentSyntax>(args), closeParen);
            }
            else if (Current.Kind == SyntaxTokenKind.OpenBracket)
            {
                var openBracket = NextToken();
                var index = ParseExpression();
                var closeBracket = Match(SyntaxTokenKind.CloseBracket);
                left = new ArrayAccessExpressionSyntax(left, openBracket, index, closeBracket);
            }
            else
            {
                break;
            }
        }

        return left;
    }

    private ImmutableArray<ArgumentSyntax> ParseArguments()
    {
        var args = ImmutableArray.CreateBuilder<ArgumentSyntax>();

        while (Current.Kind != SyntaxTokenKind.CloseParen &&
               Current.Kind != SyntaxTokenKind.EndOfFile)
        {
            var expr = ParseExpression();
            args.Add(new ArgumentSyntax(null, expr));

            if (Current.Kind == SyntaxTokenKind.Comma)
                NextToken();
            else
                break;
        }

        return args.ToImmutable();
    }

    private TypeSyntax ParseType()
    {
        if (IsPredefinedType(Current.Kind))
        {
            return new PredefinedTypeSyntax(NextToken());
        }

        if (Current.Kind == SyntaxTokenKind.ArrayKeyword)
        {
            var arrayKeyword = NextToken();
            var lessThan = Match(SyntaxTokenKind.LessThan);
            var elementType = ParseType();
            var greaterThan = Match(SyntaxTokenKind.GreaterThan);
            return new ArrayTypeSyntax(arrayKeyword, lessThan, elementType, greaterThan);
        }

        if (Current.Kind == SyntaxTokenKind.ClassKeyword)
        {
            var classKeyword = NextToken();
            var lessThan = Match(SyntaxTokenKind.LessThan);
            var constraintType = ParseType();
            var greaterThan = Match(SyntaxTokenKind.GreaterThan);
            return new ClassTypeSyntax(classKeyword, lessThan, constraintType, greaterThan);
        }

        var identifier = Match(SyntaxTokenKind.Identifier);
        return new NamedTypeSyntax(identifier, null);
    }

    private static bool IsPredefinedType(SyntaxTokenKind kind) => kind is
        SyntaxTokenKind.VoidKeyword or
        SyntaxTokenKind.IntKeyword or
        SyntaxTokenKind.UIntKeyword or
        SyntaxTokenKind.FloatKeyword or
        SyntaxTokenKind.DoubleKeyword or
        SyntaxTokenKind.BoolKeyword or
        SyntaxTokenKind.StringKeyword or
        SyntaxTokenKind.VectorKeyword or
        SyntaxTokenKind.NameKeyword or
        SyntaxTokenKind.StateKeyword or
        SyntaxTokenKind.ColorKeyword or
        SyntaxTokenKind.SoundKeyword;

    private static int GetBinaryOperatorPrecedence(SyntaxTokenKind kind) => kind switch
    {
        SyntaxTokenKind.PipePipe => 1,
        SyntaxTokenKind.AmpersandAmpersand => 2,
        SyntaxTokenKind.Pipe => 3,
        SyntaxTokenKind.Caret => 4,
        SyntaxTokenKind.Ampersand => 5,
        SyntaxTokenKind.EqualsEquals or SyntaxTokenKind.ExclamationEquals or
        SyntaxTokenKind.ApproxEquals => 6,
        SyntaxTokenKind.LessThan or SyntaxTokenKind.GreaterThan or
        SyntaxTokenKind.LessThanEquals or SyntaxTokenKind.GreaterThanEquals => 7,
        SyntaxTokenKind.LessThanLessThan or SyntaxTokenKind.GreaterThanGreaterThan or
        SyntaxTokenKind.GreaterThanGreaterThanGreaterThan => 8,
        SyntaxTokenKind.Plus or SyntaxTokenKind.Minus => 9,
        SyntaxTokenKind.Asterisk or SyntaxTokenKind.Slash or SyntaxTokenKind.Percent => 10,
        _ => 0
    };

    private static bool IsUnaryOperator(SyntaxTokenKind kind) => kind is
        SyntaxTokenKind.Plus or
        SyntaxTokenKind.Minus or
        SyntaxTokenKind.Exclamation or
        SyntaxTokenKind.Tilde or
        SyntaxTokenKind.PlusPlus or
        SyntaxTokenKind.MinusMinus;
}
```

---

## Task 8.5: Semantic Model

```csharp
namespace WAD.NET.ZScript.Semantics;

/// <summary>
/// Provides semantic information about a ZScript compilation.
/// </summary>
public sealed class SemanticModel
{
    private readonly CompilationUnitSyntax _root;
    private readonly SymbolTable _symbols;
    private readonly Dictionary<SyntaxNode, ISymbol> _symbolCache = new();

    public SemanticModel(CompilationUnitSyntax root)
    {
        _root = root;
        _symbols = BuildSymbolTable(root);
    }

    public ISymbol? GetDeclaredSymbol(SyntaxNode node)
    {
        if (_symbolCache.TryGetValue(node, out var cached))
            return cached;

        ISymbol? symbol = node switch
        {
            ClassDeclarationSyntax cls => _symbols.LookupClass(cls.Name),
            ActorDeclarationSyntax actor => _symbols.LookupClass(actor.Name),
            MethodDeclarationSyntax method => LookupMethod(method),
            FieldDeclarationSyntax field => LookupField(field),
            _ => null
        };

        if (symbol != null)
            _symbolCache[node] = symbol;

        return symbol;
    }

    public ISymbol? GetSymbolInfo(ExpressionSyntax expression)
    {
        return expression switch
        {
            IdentifierExpressionSyntax id => LookupIdentifier(id),
            MemberAccessExpressionSyntax member => LookupMember(member),
            InvocationExpressionSyntax invocation => GetSymbolInfo(invocation.Expression),
            _ => null
        };
    }

    public ITypeSymbol? GetTypeInfo(ExpressionSyntax expression)
    {
        var symbol = GetSymbolInfo(expression);
        return symbol switch
        {
            IFieldSymbol field => field.Type,
            IMethodSymbol method => method.ReturnType,
            IPropertySymbol prop => prop.Type,
            ILocalSymbol local => local.Type,
            _ => null
        };
    }

    /// <summary>
    /// Gets the inheritance chain for a class, from most derived to base.
    /// </summary>
    public IEnumerable<IClassSymbol> GetInheritanceChain(IClassSymbol symbol)
    {
        var current = symbol;
        while (current != null)
        {
            yield return current;
            current = current.BaseType;
        }
    }

    /// <summary>
    /// Finds all classes that replace a given actor.
    /// </summary>
    public IEnumerable<IClassSymbol> GetReplacers(string actorName)
    {
        return _symbols.AllClasses
            .Where(c => c.ReplacesName?.Equals(actorName, StringComparison.OrdinalIgnoreCase) == true);
    }

    /// <summary>
    /// Checks if a class is a monster (inherits from Actor and has ISMONSTER flag).
    /// </summary>
    public bool IsMonster(IClassSymbol classSymbol)
    {
        // Check inheritance chain for Actor base
        if (!GetInheritanceChain(classSymbol).Any(c => c.Name.Equals("Actor", StringComparison.OrdinalIgnoreCase)))
            return false;

        // Check for ISMONSTER flag
        return classSymbol.Flags.Contains("ISMONSTER");
    }

    /// <summary>
    /// Checks if a class is a weapon.
    /// </summary>
    public bool IsWeapon(IClassSymbol classSymbol)
    {
        return GetInheritanceChain(classSymbol)
            .Any(c => c.Name.Equals("Weapon", StringComparison.OrdinalIgnoreCase));
    }

    private SymbolTable BuildSymbolTable(CompilationUnitSyntax root)
    {
        var table = new SymbolTable();
        var visitor = new SymbolBuildingVisitor(table);
        root.Accept(visitor);
        return table;
    }
}

public interface ISymbol
{
    string Name { get; }
    SymbolKind Kind { get; }
    SyntaxNode? DeclaringSyntax { get; }
}

public enum SymbolKind
{
    Class,
    Struct,
    Enum,
    Method,
    Field,
    Property,
    Local,
    Parameter,
    State,
    StateLabel
}

public interface IClassSymbol : ISymbol
{
    IClassSymbol? BaseType { get; }
    string? ReplacesName { get; }
    int? DoomEdNumber { get; }
    IReadOnlyList<IMethodSymbol> Methods { get; }
    IReadOnlyList<IFieldSymbol> Fields { get; }
    IReadOnlyDictionary<string, object?> Properties { get; }
    IReadOnlySet<string> Flags { get; }
    IReadOnlyList<IStateSymbol> States { get; }
}

public interface IMethodSymbol : ISymbol
{
    ITypeSymbol ReturnType { get; }
    IReadOnlyList<IParameterSymbol> Parameters { get; }
    bool IsVirtual { get; }
    bool IsOverride { get; }
    bool IsNative { get; }
    bool IsAction { get; }
}

public interface IFieldSymbol : ISymbol
{
    ITypeSymbol Type { get; }
    bool IsStatic { get; }
    bool IsReadOnly { get; }
}

public interface IStateSymbol : ISymbol
{
    string Label { get; }
    IReadOnlyList<StateFrameInfo> Frames { get; }
}

public class StateFrameInfo
{
    public string SpriteName { get; init; }
    public string FrameLetters { get; init; }
    public int Duration { get; init; }
    public bool IsBright { get; init; }
    public string? ActionName { get; init; }
}

public sealed class SymbolTable
{
    private readonly Dictionary<string, IClassSymbol> _classes = new(StringComparer.OrdinalIgnoreCase);

    public IEnumerable<IClassSymbol> AllClasses => _classes.Values;

    public void AddClass(IClassSymbol symbol)
    {
        _classes[symbol.Name] = symbol;
    }

    public IClassSymbol? LookupClass(string name)
    {
        _classes.TryGetValue(name, out var symbol);
        return symbol;
    }

    public void ResolveBaseTypes()
    {
        foreach (var cls in _classes.Values.OfType<ClassSymbol>())
        {
            if (cls.BaseTypeName != null)
            {
                cls.SetBaseType(LookupClass(cls.BaseTypeName));
            }
        }
    }
}
```

---

## Task 8.6: Visitor and Rewriter Patterns

```csharp
namespace WAD.NET.ZScript.Syntax;

/// <summary>
/// Base visitor for traversing syntax trees.
/// </summary>
public abstract class SyntaxVisitor
{
    public virtual void Visit(SyntaxNode? node)
    {
        node?.Accept(this);
    }

    public virtual void DefaultVisit(SyntaxNode node)
    {
        foreach (var child in node.ChildNodes())
            Visit(child);
    }

    public virtual void VisitCompilationUnit(CompilationUnitSyntax node) => DefaultVisit(node);
    public virtual void VisitClassDeclaration(ClassDeclarationSyntax node) => DefaultVisit(node);
    public virtual void VisitActorDeclaration(ActorDeclarationSyntax node) => DefaultVisit(node);
    public virtual void VisitDefaultBlock(DefaultBlockSyntax node) => DefaultVisit(node);
    public virtual void VisitStatesBlock(StatesBlockSyntax node) => DefaultVisit(node);
    public virtual void VisitStateLabel(StateLabelSyntax node) => DefaultVisit(node);
    public virtual void VisitStateFrame(StateFrameSyntax node) => DefaultVisit(node);
    public virtual void VisitMethodDeclaration(MethodDeclarationSyntax node) => DefaultVisit(node);
    public virtual void VisitFieldDeclaration(FieldDeclarationSyntax node) => DefaultVisit(node);
    public virtual void VisitPropertyAssignment(PropertyAssignmentSyntax node) => DefaultVisit(node);
    public virtual void VisitFlagDefinition(FlagDefinitionSyntax node) => DefaultVisit(node);
    // ... more visit methods for each node type
}

/// <summary>
/// Visitor that returns a result for each node.
/// </summary>
public abstract class SyntaxVisitor<TResult>
{
    public virtual TResult? Visit(SyntaxNode? node)
    {
        return node != null ? node.Accept(this) : default;
    }

    public virtual TResult? DefaultVisit(SyntaxNode node) => default;

    public virtual TResult? VisitCompilationUnit(CompilationUnitSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitClassDeclaration(ClassDeclarationSyntax node) => DefaultVisit(node);
    // ... more visit methods
}

/// <summary>
/// Base class for syntax tree rewriters.
/// Returns modified copies of nodes (immutable transformation).
/// </summary>
public abstract class SyntaxRewriter : SyntaxVisitor<SyntaxNode>
{
    public override SyntaxNode? VisitCompilationUnit(CompilationUnitSyntax node)
    {
        var members = VisitList(node.Members);

        if (members != node.Members)
        {
            return node.With(members: members);
        }

        return node;
    }

    public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        var members = VisitList(node.Members);

        if (members != node.Members)
        {
            return node.With(members: members);
        }

        return node;
    }

    protected ImmutableArray<T> VisitList<T>(ImmutableArray<T> list) where T : SyntaxNode
    {
        ImmutableArray<T>.Builder? builder = null;

        for (int i = 0; i < list.Length; i++)
        {
            var item = list[i];
            var visited = Visit(item);

            if (visited != item && builder == null)
            {
                builder = ImmutableArray.CreateBuilder<T>(list.Length);
                for (int j = 0; j < i; j++)
                    builder.Add(list[j]);
            }

            if (builder != null && visited is T typedVisited)
            {
                builder.Add(typedVisited);
            }
        }

        return builder?.ToImmutable() ?? list;
    }
}

// Example rewriter: Rename all actors
public class ActorRenamer : SyntaxRewriter
{
    private readonly Func<string, string> _rename;

    public ActorRenamer(Func<string, string> rename)
    {
        _rename = rename;
    }

    public override SyntaxNode? VisitActorDeclaration(ActorDeclarationSyntax node)
    {
        var newName = _rename(node.Name);
        if (newName != node.Name)
        {
            return node.With(identifier: node.Identifier.WithText(newName));
        }
        return base.VisitActorDeclaration(node);
    }
}

// Example walker: Collect all state labels
public class StateLabelCollector : SyntaxVisitor
{
    public List<string> Labels { get; } = new();

    public override void VisitStateLabel(StateLabelSyntax node)
    {
        Labels.Add(node.LabelName);
        base.VisitStateLabel(node);
    }
}

// Example walker: Find unused states
public class UnusedStateFinder : SyntaxVisitor
{
    private readonly HashSet<string> _definedLabels = new();
    private readonly HashSet<string> _referencedLabels = new();

    public IEnumerable<string> UnusedLabels => _definedLabels.Except(_referencedLabels);

    public override void VisitStateLabel(StateLabelSyntax node)
    {
        _definedLabels.Add(node.LabelName);
        base.VisitStateLabel(node);
    }

    public override void VisitStateGoto(StateGotoSyntax node)
    {
        _referencedLabels.Add(node.TargetLabel);
        base.VisitStateGoto(node);
    }
}
```

---

## Task 8.7: Formatting and Diagnostics

```csharp
namespace WAD.NET.ZScript.Formatting;

public sealed class SyntaxFormatter
{
    private readonly FormattingOptions _options;

    public SyntaxFormatter(FormattingOptions? options = null)
    {
        _options = options ?? FormattingOptions.Default;
    }

    public string Format(SyntaxNode node)
    {
        var rewriter = new FormattingRewriter(_options);
        var formatted = rewriter.Visit(node);
        return formatted?.ToFullString() ?? "";
    }
}

public class FormattingOptions
{
    public static FormattingOptions Default { get; } = new();

    public int IndentSize { get; init; } = 4;
    public bool UseTabs { get; init; } = true;
    public bool SpaceAfterComma { get; init; } = true;
    public bool SpaceAroundBinaryOperators { get; init; } = true;
    public bool NewLineBeforeOpenBrace { get; init; } = true;
    public bool IndentCaseLabels { get; init; } = true;
}

namespace WAD.NET.ZScript.Diagnostics;

public enum DiagnosticSeverity
{
    Hidden,
    Info,
    Warning,
    Error
}

public sealed class Diagnostic
{
    public DiagnosticSeverity Severity { get; }
    public string Id { get; }
    public string Message { get; }
    public TextSpan Span { get; }
    public Location? Location { get; }

    public Diagnostic(DiagnosticSeverity severity, string message, TextSpan span, string? id = null)
    {
        Severity = severity;
        Message = message;
        Span = span;
        Id = id ?? "ZS0000";
    }
}

public sealed class Location
{
    public string FilePath { get; }
    public int Line { get; }
    public int Column { get; }

    public Location(string filePath, int line, int column)
    {
        FilePath = filePath;
        Line = line;
        Column = column;
    }
}

/// <summary>
/// Analyzer base class for creating custom diagnostics.
/// </summary>
public abstract class DiagnosticAnalyzer
{
    public abstract IEnumerable<DiagnosticDescriptor> SupportedDiagnostics { get; }

    public abstract void Analyze(SemanticModel model, Action<Diagnostic> reportDiagnostic);
}

public class DiagnosticDescriptor
{
    public string Id { get; }
    public string Title { get; }
    public string MessageFormat { get; }
    public DiagnosticSeverity DefaultSeverity { get; }
    public string Category { get; }

    public DiagnosticDescriptor(
        string id,
        string title,
        string messageFormat,
        string category,
        DiagnosticSeverity defaultSeverity)
    {
        Id = id;
        Title = title;
        MessageFormat = messageFormat;
        Category = category;
        DefaultSeverity = defaultSeverity;
    }
}

// Example analyzer: Detect missing required states for weapons
public class WeaponStateAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor MissingReadyState = new(
        "ZS1001",
        "Missing Ready state",
        "Weapon '{0}' is missing required 'Ready' state",
        "Completeness",
        DiagnosticSeverity.Error);

    private static readonly DiagnosticDescriptor MissingFireState = new(
        "ZS1002",
        "Missing Fire state",
        "Weapon '{0}' is missing required 'Fire' state",
        "Completeness",
        DiagnosticSeverity.Error);

    public override IEnumerable<DiagnosticDescriptor> SupportedDiagnostics =>
        [MissingReadyState, MissingFireState];

    public override void Analyze(SemanticModel model, Action<Diagnostic> reportDiagnostic)
    {
        foreach (var cls in model.GetAllClasses())
        {
            if (!model.IsWeapon(cls))
                continue;

            var stateLabels = cls.States.Select(s => s.Label).ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (!stateLabels.Contains("Ready"))
            {
                reportDiagnostic(new Diagnostic(
                    DiagnosticSeverity.Error,
                    string.Format(MissingReadyState.MessageFormat, cls.Name),
                    cls.DeclaringSyntax?.Span ?? default,
                    MissingReadyState.Id));
            }

            if (!stateLabels.Contains("Fire"))
            {
                reportDiagnostic(new Diagnostic(
                    DiagnosticSeverity.Error,
                    string.Format(MissingFireState.MessageFormat, cls.Name),
                    cls.DeclaringSyntax?.Span ?? default,
                    MissingFireState.Id));
            }
        }
    }
}
```

---

## Task 8.8: C# Transpilation Support

```csharp
namespace WAD.NET.ZScript.Emit;

/// <summary>
/// Converts ZScript AST to C# code for native .NET mods.
/// </summary>
public sealed class CSharpEmitter : SyntaxVisitor<string>
{
    private readonly SemanticModel _model;
    private readonly StringBuilder _output = new();
    private int _indent;

    public CSharpEmitter(SemanticModel model)
    {
        _model = model;
    }

    public string Emit(CompilationUnitSyntax root)
    {
        _output.Clear();
        _output.AppendLine("// Auto-generated from ZScript");
        _output.AppendLine("using System;");
        _output.AppendLine("using ZDoom.Runtime;");
        _output.AppendLine();

        foreach (var member in root.Members)
        {
            Visit(member);
            _output.AppendLine();
        }

        return _output.ToString();
    }

    public override string? VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        WriteIndent();
        _output.Append("public class ");
        _output.Append(node.Name);

        if (node.BaseClassName != null)
        {
            _output.Append(" : ");
            _output.Append(MapType(node.BaseClassName));
        }

        _output.AppendLine();
        WriteIndent();
        _output.AppendLine("{");
        _indent++;

        // Emit fields from Default block as properties
        if (node.DefaultBlock != null)
        {
            foreach (var prop in node.DefaultBlock.Properties)
            {
                EmitProperty(prop);
            }
        }

        // Emit methods
        foreach (var method in node.Methods)
        {
            Visit(method);
        }

        // Emit states as a dictionary or state machine
        if (node.StatesBlock != null)
        {
            EmitStates(node.StatesBlock);
        }

        _indent--;
        WriteIndent();
        _output.AppendLine("}");

        return null;
    }

    private void EmitProperty(PropertyAssignmentSyntax prop)
    {
        WriteIndent();

        var propType = InferPropertyType(prop.FullPropertyName);
        _output.Append($"public {propType} {prop.PropertyName.Text} {{ get; set; }}");

        if (prop.Values.Length > 0)
        {
            _output.Append(" = ");
            _output.Append(EmitExpression(prop.Values[0]));
        }

        _output.AppendLine(";");
    }

    private void EmitStates(StatesBlockSyntax states)
    {
        WriteIndent();
        _output.AppendLine("protected override void DefineStates()");
        WriteIndent();
        _output.AppendLine("{");
        _indent++;

        string? currentLabel = null;

        foreach (var state in states.States)
        {
            if (state is StateLabelSyntax label)
            {
                currentLabel = label.LabelName;
                WriteIndent();
                _output.AppendLine($"DefineLabel(\"{currentLabel}\");");
            }
            else if (state is StateFrameSyntax frame)
            {
                WriteIndent();
                _output.Append($"AddFrame(\"{frame.SpriteName}\", \"{frame.Frames}\", {frame.DurationTics}");

                if (frame.IsBright)
                    _output.Append(", bright: true");

                if (frame.Action != null && !frame.Action.IsAnonymousBlock)
                {
                    _output.Append($", () => {frame.Action.ActionIdentifier?.Text}()");
                }

                _output.AppendLine(");");
            }
            else if (state is StateGotoSyntax gotoState)
            {
                WriteIndent();
                _output.AppendLine($"Goto(\"{gotoState.TargetLabel}\", {gotoState.FrameOffset});");
            }
            else if (state is StateLoopSyntax)
            {
                WriteIndent();
                _output.AppendLine("Loop();");
            }
            else if (state is StateStopSyntax)
            {
                WriteIndent();
                _output.AppendLine("Stop();");
            }
        }

        _indent--;
        WriteIndent();
        _output.AppendLine("}");
    }

    private string MapType(string zscriptType)
    {
        return zscriptType.ToLowerInvariant() switch
        {
            "int" => "int",
            "uint" => "uint",
            "float" => "float",
            "double" => "double",
            "bool" => "bool",
            "string" => "string",
            "name" => "string",
            "vector2" => "Vector2",
            "vector3" => "Vector3",
            "state" => "State",
            "actor" => "Actor",
            "weapon" => "Weapon",
            _ => zscriptType
        };
    }

    private string InferPropertyType(string propertyName)
    {
        return propertyName.ToLowerInvariant() switch
        {
            "health" or "damage" or "speed" or "radius" or "height" or "mass" => "int",
            "scale" or "painchance" => "float",
            "obituary" or "tag" => "string",
            _ => "object"
        };
    }

    private void WriteIndent()
    {
        _output.Append(new string('\t', _indent));
    }

    private string EmitExpression(ExpressionSyntax expr)
    {
        return expr switch
        {
            LiteralExpressionSyntax lit => lit.Token.Text,
            IdentifierExpressionSyntax id => id.Identifier.Text,
            BinaryExpressionSyntax bin =>
                $"({EmitExpression(bin.Left)} {bin.OperatorToken.Text} {EmitExpression(bin.Right)})",
            _ => "/* unknown */"
        };
    }
}
```

---

## Acceptance Criteria

1. **Lexer** correctly tokenizes ZScript and DECORATE source with full trivia preservation
2. **Parser** produces accurate syntax trees for all ZScript constructs
3. **Round-trip** parsing and re-emitting preserves original source exactly
4. **Semantic model** resolves inheritance chains, property values, and state references
5. **Visitors** can traverse and transform syntax trees without mutation
6. **Diagnostics** can detect missing states, undefined references, and type errors
7. **Weapon/Monster detection** accurately identifies actors by inheritance and flags

---

## Test Cases

```csharp
[Fact]
public void ShouldParseSimpleActor()
{
    var source = @"
actor MyZombie : ZombieMan replaces ZombieMan 1001
{
    Health 50
    +ISMONSTER
    States
    {
    Spawn:
        POSS AB 4
        Loop
    }
}";

    var parser = new Parser(source);
    var unit = parser.ParseCompilationUnit();

    Assert.Empty(parser.Diagnostics);
    Assert.Single(unit.Members);

    var actor = Assert.IsType<ActorDeclarationSyntax>(unit.Members[0]);
    Assert.Equal("MyZombie", actor.Name);
    Assert.Equal("ZombieMan", actor.BaseName);
    Assert.Equal("ZombieMan", actor.ReplacesName);
    Assert.Equal(1001, actor.DoomEdNumber);
}

[Fact]
public void ShouldPreserveTrivia()
{
    var source = "// Comment\nactor Test { }";

    var parser = new Parser(source);
    var unit = parser.ParseCompilationUnit();

    var fullText = unit.ToFullString();
    Assert.Equal(source, fullText);
}

[Fact]
public void ShouldResolveInheritance()
{
    var source = @"
class Demon : Actor { }
class PinkyDemon : Demon { }
class Spectre : PinkyDemon { }
";

    var parser = new Parser(source);
    var unit = parser.ParseCompilationUnit();
    var model = new SemanticModel(unit);

    var spectre = model.GetDeclaredSymbol(unit.Members[2]) as IClassSymbol;
    var chain = model.GetInheritanceChain(spectre!).Select(c => c.Name).ToList();

    Assert.Equal(["Spectre", "PinkyDemon", "Demon", "Actor"], chain);
}

[Fact]
public void ShouldDetectWeapons()
{
    var source = @"
class MyShotgun : Weapon
{
    Default
    {
        Weapon.SlotNumber 3;
        Weapon.AmmoType ""Shell"";
    }
    States
    {
    Ready:
        SHTG A 1 A_WeaponReady
        Loop
    Fire:
        SHTG A 3 A_FireShotgun
        Goto Ready
    }
}
";

    var parser = new Parser(source);
    var unit = parser.ParseCompilationUnit();
    var model = new SemanticModel(unit);

    var weapon = model.GetDeclaredSymbol(unit.Members[0]) as IClassSymbol;
    Assert.True(model.IsWeapon(weapon!));
    Assert.Equal("3", weapon!.Properties["Weapon.SlotNumber"]?.ToString());
}

[Fact]
public void ShouldReportMissingStates()
{
    var source = @"
class BrokenWeapon : Weapon
{
    States
    {
    Ready:
        PISG A 1 A_WeaponReady
        Loop
    // Missing Fire state!
    }
}
";

    var parser = new Parser(source);
    var unit = parser.ParseCompilationUnit();
    var model = new SemanticModel(unit);

    var analyzer = new WeaponStateAnalyzer();
    var diagnostics = new List<Diagnostic>();
    analyzer.Analyze(model, diagnostics.Add);

    Assert.Single(diagnostics);
    Assert.Equal("ZS1002", diagnostics[0].Id);
    Assert.Contains("Fire", diagnostics[0].Message);
}

[Fact]
public void ShouldTranspileToCSharp()
{
    var source = @"
class SimpleMonster : Actor
{
    Default
    {
        Health 100;
        Speed 8;
        +ISMONSTER
    }
}
";

    var parser = new Parser(source);
    var unit = parser.ParseCompilationUnit();
    var model = new SemanticModel(unit);
    var emitter = new CSharpEmitter(model);

    var csharp = emitter.Emit(unit);

    Assert.Contains("public class SimpleMonster : Actor", csharp);
    Assert.Contains("public int Health { get; set; } = 100", csharp);
    Assert.Contains("public int Speed { get; set; } = 8", csharp);
}
```

---

## Implementation Notes

### Priority Order

1. **Lexer + Token types** - Foundation for all parsing
2. **Core syntax nodes** (ClassDeclaration, DefaultBlock, StatesBlock)
3. **Parser** - Basic structure parsing
4. **State frame parsing** - Critical for sprite extraction
5. **Property/flag parsing** - For actor detection
6. **Semantic model** - Inheritance resolution
7. **Expression parsing** - Full expression support
8. **Statements** - Method body parsing
9. **Tooling** - Visitors, rewriters, formatters
10. **C# emission** - Advanced feature

### DECORATE vs ZScript

The parser should support both syntaxes:
- **DECORATE**: `actor Name : Parent replaces Replaced EdNum { }`
- **ZScript**: `class Name : Parent replaces Replaced { }`

Both produce the same AST node types; the difference is primarily keyword usage.

### Performance Considerations

- Use `ImmutableArray<T>` for all collections in AST nodes
- Pool string builders for formatting/emission
- Consider incremental parsing for IDE scenarios
- Cache semantic model lookups with lazy initialization

### Error Recovery

The parser should continue after errors:
- Insert missing tokens (marked `IsMissing = true`)
- Skip to next synchronization point (`;`, `}`, keyword)
- Collect all diagnostics for display

---

## Consumer Requirements: WadAgent Integration

WadAgent (WadDB.WadAgent) currently contains ~600 lines of ad-hoc detection heuristics that must be replaced by this parser's semantic model. The following APIs are **required** for WadAgent integration.

### Required SemanticModel APIs

```csharp
public sealed class SemanticModel
{
    /// <summary>
    /// All classes/actors defined in the parsed sources.
    /// </summary>
    public IEnumerable<IClassSymbol> AllClasses { get; }

    /// <summary>
    /// Look up a class by name (case-insensitive).
    /// </summary>
    public IClassSymbol? LookupClass(string name);

    /// <summary>
    /// Determines if a class is a weapon by tracing inheritance to Weapon base class.
    /// Must handle: Weapon, DoomWeapon, HereticWeapon, HexenWeapon, custom weapon bases.
    /// </summary>
    public bool IsWeapon(IClassSymbol classSymbol);

    /// <summary>
    /// Determines if a class is a monster by checking:
    /// 1. ISMONSTER flag in Default block
    /// 2. Inheritance from known monster classes
    /// 3. Presence of monster state labels (See, Melee, Missile, Pain, Death)
    /// </summary>
    public bool IsMonster(IClassSymbol classSymbol);

    /// <summary>
    /// Gets the full inheritance chain from most derived to base.
    /// Used for conflict detection and property inheritance.
    /// </summary>
    public IEnumerable<IClassSymbol> GetInheritanceChain(IClassSymbol classSymbol);

    /// <summary>
    /// Finds all classes that replace a given actor name.
    /// </summary>
    public IEnumerable<IClassSymbol> GetReplacers(string actorName);
}
```

### Required IClassSymbol APIs

```csharp
public interface IClassSymbol : ISymbol
{
    /// <summary>
    /// The base class this inherits from (null for Actor/Object).
    /// </summary>
    IClassSymbol? BaseType { get; }

    /// <summary>
    /// The actor name this class replaces (from "replaces" keyword).
    /// </summary>
    string? ReplacesName { get; }

    /// <summary>
    /// DoomEd number if specified.
    /// </summary>
    int? DoomEdNumber { get; }

    /// <summary>
    /// Properties from Default block, keyed by property name.
    /// Examples: "Health" -> 100, "Speed" -> 8, "Weapon.SlotNumber" -> 3
    /// </summary>
    IReadOnlyDictionary<string, object?> Properties { get; }

    /// <summary>
    /// Flags from Default block (e.g., "ISMONSTER", "SOLID", "SHOOTABLE").
    /// </summary>
    IReadOnlySet<string> Flags { get; }

    /// <summary>
    /// Parsed state definitions with frames and sprites.
    /// </summary>
    IReadOnlyList<IStateSymbol> States { get; }
}
```

### Required IStateSymbol APIs

```csharp
public interface IStateSymbol : ISymbol
{
    /// <summary>
    /// State label (e.g., "Spawn", "See", "Ready", "Fire").
    /// </summary>
    string Label { get; }

    /// <summary>
    /// All frames in this state sequence.
    /// </summary>
    IReadOnlyList<StateFrameInfo> Frames { get; }
}

public class StateFrameInfo
{
    /// <summary>
    /// 4-character sprite name (e.g., "POSS", "TROO", "PISG").
    /// </summary>
    public string SpriteName { get; init; }

    /// <summary>
    /// Frame letters (e.g., "A", "ABCD", "AB").
    /// </summary>
    public string FrameLetters { get; init; }

    /// <summary>
    /// Duration in tics (1/35 second). -1 for infinite.
    /// </summary>
    public int Duration { get; init; }

    /// <summary>
    /// Whether the Bright modifier is set.
    /// </summary>
    public bool IsBright { get; init; }

    /// <summary>
    /// Action function name if any (e.g., "A_Chase", "A_FireBullets").
    /// </summary>
    public string? ActionName { get; init; }
}
```

### WadAgent Usage Pattern

```csharp
// WadAgent will use the parser like this:
public List<MonsterInfo> GetMonsters(CompositeArchive archive)
{
    // 1. Parse all DECORATE/ZSCRIPT lumps
    var units = new List<CompilationUnitSyntax>();

    if (archive.Contains("DECORATE"))
    {
        var content = archive.ReadLumpAsString("DECORATE");
        var parser = new DecorateParser(content);
        units.Add(parser.ParseCompilationUnit());
    }

    if (archive.Contains("ZSCRIPT"))
    {
        var content = archive.ReadLumpAsString("ZSCRIPT");
        var parser = new ZScriptParser(content);
        units.Add(parser.ParseCompilationUnit());
    }

    // 2. Build semantic model
    var model = SemanticModel.Create(units);

    // 3. Query for monsters
    var monsters = new List<MonsterInfo>();
    foreach (var cls in model.AllClasses)
    {
        if (!model.IsMonster(cls))
            continue;

        // 4. Extract sprite prefixes from States
        var sprites = cls.States
            .SelectMany(s => s.Frames)
            .Select(f => f.SpriteName)
            .Distinct()
            .ToList();

        // 5. Extract properties from Default block
        var health = cls.Properties.GetValueOrDefault("Health") as int?;
        var speed = cls.Properties.GetValueOrDefault("Speed") as int?;

        monsters.Add(new MonsterInfo
        {
            ClassName = cls.Name,
            Replaces = cls.ReplacesName,
            Health = health,
            Speed = speed,
            SpritePrefixes = sprites,
            StateLabels = cls.States.Select(s => s.Label).ToArray(),
            Flags = cls.Flags.ToArray()
        });
    }

    return monsters;
}
```

### Verification Checklist

Before releasing, verify WadAgent can:
- [ ] Detect all monsters in Complex Doom via `IsMonster()`
- [ ] Detect all weapons in Complex Doom via `IsWeapon()`
- [ ] Extract sprite prefixes from States blocks (not via heuristics)
- [ ] Get Health/Speed/Radius/Height from Properties
- [ ] Get ISMONSTER/SOLID/etc from Flags
- [ ] Resolve inheritance chains for custom actor hierarchies
- [ ] Handle `replaces` keyword for predecessor detection
