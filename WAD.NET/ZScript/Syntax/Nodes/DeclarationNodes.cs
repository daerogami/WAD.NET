using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace WAD.NET.ZScript.Syntax;

/// <summary>
/// Abstract base class for all member declaration syntax nodes (classes, methods, fields, etc.).
/// </summary>
public abstract class MemberDeclarationSyntax : SyntaxNode
{
    protected MemberDeclarationSyntax(TextSpan span, TextSpan fullSpan)
        : base(span, fullSpan)
    {
    }
}

/// <summary>
/// Abstract base class for items that can appear inside a Default block.
/// </summary>
public abstract class DefaultItemSyntax : SyntaxNode
{
    protected DefaultItemSyntax(TextSpan span, TextSpan fullSpan)
        : base(span, fullSpan)
    {
    }
}

/// <summary>
/// The root node representing an entire ZScript compilation unit (file).
/// </summary>
public sealed class CompilationUnitSyntax : SyntaxNode
{
    /// <summary>
    /// The optional version directive (e.g., <c>version "4.10"</c>).
    /// </summary>
    public VersionDirectiveSyntax? VersionDirective { get; }

    /// <summary>
    /// The include directives at the top of the file.
    /// </summary>
    public ImmutableArray<IncludeDirectiveSyntax> Includes { get; }

    /// <summary>
    /// The top-level member declarations (classes, structs, enums, etc.).
    /// </summary>
    public ImmutableArray<MemberDeclarationSyntax> Members { get; }

    /// <summary>
    /// The end-of-file token.
    /// </summary>
    public SyntaxToken EndOfFileToken { get; }

    /// <summary>
    /// Returns all class declarations in this compilation unit.
    /// </summary>
    public IEnumerable<ClassDeclarationSyntax> Classes =>
        Members.OfType<ClassDeclarationSyntax>();

    /// <summary>
    /// Returns all actor declarations in this compilation unit.
    /// </summary>
    public IEnumerable<ActorDeclarationSyntax> Actors =>
        Members.OfType<ActorDeclarationSyntax>();

    public override SyntaxNodeKind Kind => SyntaxNodeKind.CompilationUnit;

    public CompilationUnitSyntax(
        VersionDirectiveSyntax? versionDirective,
        ImmutableArray<IncludeDirectiveSyntax> includes,
        ImmutableArray<MemberDeclarationSyntax> members,
        SyntaxToken endOfFileToken)
        : base(
            ComputeSpanForUnit(versionDirective, includes, members, endOfFileToken),
            ComputeFullSpanForUnit(versionDirective, includes, members, endOfFileToken))
    {
        VersionDirective = versionDirective;
        Includes = includes;
        Members = members;
        EndOfFileToken = endOfFileToken;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        if (VersionDirective != null)
            yield return VersionDirective;
        foreach (var include in Includes)
            yield return include;
        foreach (var member in Members)
            yield return member;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        yield return EndOfFileToken;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitCompilationUnit(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitCompilationUnit(this);

    private static TextSpan ComputeSpanForUnit(
        VersionDirectiveSyntax? version,
        ImmutableArray<IncludeDirectiveSyntax> includes,
        ImmutableArray<MemberDeclarationSyntax> members,
        SyntaxToken eof)
    {
        TextSpan first;
        if (version != null) first = version.Span;
        else if (includes.Length > 0) first = includes[0].Span;
        else if (members.Length > 0) first = members[0].Span;
        else first = eof.Span;
        return ComputeSpan(first, eof.Span);
    }

    private static TextSpan ComputeFullSpanForUnit(
        VersionDirectiveSyntax? version,
        ImmutableArray<IncludeDirectiveSyntax> includes,
        ImmutableArray<MemberDeclarationSyntax> members,
        SyntaxToken eof)
    {
        TextSpan first;
        if (version != null) first = version.FullSpan;
        else if (includes.Length > 0) first = includes[0].FullSpan;
        else if (members.Length > 0) first = members[0].FullSpan;
        else first = eof.FullSpan;
        return ComputeSpan(first, eof.FullSpan);
    }

    private static TextSpan ComputeSpan(TextSpan first, TextSpan last)
    {
        return new TextSpan(first.Start, last.End - first.Start);
    }
}

/// <summary>
/// A version directive such as <c>version "4.10"</c>.
/// </summary>
public sealed class VersionDirectiveSyntax : SyntaxNode
{
    /// <summary>
    /// The <c>version</c> keyword token.
    /// </summary>
    public SyntaxToken VersionKeyword { get; }

    /// <summary>
    /// The version string token (e.g., <c>"4.10"</c>).
    /// </summary>
    public SyntaxToken VersionString { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.VersionDirective;

    public VersionDirectiveSyntax(SyntaxToken versionKeyword, SyntaxToken versionString)
        : base(
            ComputeSpan(versionKeyword.Span, versionString.Span),
            ComputeSpan(versionKeyword.FullSpan, versionString.FullSpan))
    {
        VersionKeyword = versionKeyword;
        VersionString = versionString;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        yield break;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        yield return VersionKeyword;
        yield return VersionString;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitVersionDirective(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitVersionDirective(this);

    private static TextSpan ComputeSpan(TextSpan first, TextSpan last)
    {
        return new TextSpan(first.Start, last.End - first.Start);
    }
}

/// <summary>
/// An include directive such as <c>#include "zscript/weapons.zs"</c>.
/// </summary>
public sealed class IncludeDirectiveSyntax : SyntaxNode
{
    /// <summary>
    /// The <c>#</c> token.
    /// </summary>
    public SyntaxToken HashToken { get; }

    /// <summary>
    /// The <c>include</c> keyword token.
    /// </summary>
    public SyntaxToken IncludeKeyword { get; }

    /// <summary>
    /// The path string token.
    /// </summary>
    public SyntaxToken PathString { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.IncludeDirective;

    public IncludeDirectiveSyntax(
        SyntaxToken hashToken,
        SyntaxToken includeKeyword,
        SyntaxToken pathString)
        : base(
            ComputeSpan(hashToken.Span, pathString.Span),
            ComputeSpan(hashToken.FullSpan, pathString.FullSpan))
    {
        HashToken = hashToken;
        IncludeKeyword = includeKeyword;
        PathString = pathString;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        yield break;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        yield return HashToken;
        yield return IncludeKeyword;
        yield return PathString;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitIncludeDirective(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitIncludeDirective(this);

    private static TextSpan ComputeSpan(TextSpan first, TextSpan last)
    {
        return new TextSpan(first.Start, last.End - first.Start);
    }
}

/// <summary>
/// A ZScript class declaration such as <c>class DoomPlayer : PlayerPawn { ... }</c>.
/// </summary>
public sealed class ClassDeclarationSyntax : MemberDeclarationSyntax
{
    /// <summary>
    /// Access or other modifier tokens (e.g., <c>abstract</c>, <c>native</c>).
    /// </summary>
    public ImmutableArray<SyntaxToken> Modifiers { get; }

    /// <summary>
    /// The <c>class</c> keyword.
    /// </summary>
    public SyntaxToken ClassKeyword { get; }

    /// <summary>
    /// The class name identifier token.
    /// </summary>
    public SyntaxToken Identifier { get; }

    /// <summary>
    /// The optional base class list.
    /// </summary>
    public BaseListSyntax? BaseList { get; }

    /// <summary>
    /// The optional <c>replaces</c> keyword.
    /// </summary>
    public SyntaxToken? ReplacesKeyword { get; }

    /// <summary>
    /// The optional identifier of the class being replaced.
    /// </summary>
    public SyntaxToken? ReplacesIdentifier { get; }

    /// <summary>
    /// The optional DoomEdNum (editor number).
    /// </summary>
    public SyntaxToken? EditorNumber { get; }

    /// <summary>
    /// The <c>{</c> token.
    /// </summary>
    public SyntaxToken OpenBraceToken { get; }

    /// <summary>
    /// The member declarations within the class body.
    /// </summary>
    public ImmutableArray<MemberDeclarationSyntax> Members { get; }

    /// <summary>
    /// The <c>}</c> token.
    /// </summary>
    public SyntaxToken CloseBraceToken { get; }

    /// <summary>
    /// The class name text.
    /// </summary>
    public string Name => Identifier.Text;

    /// <summary>
    /// The base class name, or null if no base class is specified.
    /// </summary>
    public string? BaseClassName => BaseList?.BaseType?.ToString();

    /// <summary>
    /// The name of the class being replaced, or null if not replacing.
    /// </summary>
    public string? ReplacesClassName => ReplacesIdentifier?.Text;

    /// <summary>
    /// The DoomEdNum value, or null if not specified.
    /// </summary>
    public int? DoomEdNumber => EditorNumber?.Value is long lv ? (int)lv : EditorNumber?.Value as int?;

    /// <summary>
    /// The Default block, if present.
    /// </summary>
    public DefaultBlockSyntax? DefaultBlock => Members.OfType<DefaultBlockSyntax>().FirstOrDefault();

    /// <summary>
    /// The States block, if present.
    /// </summary>
    public StatesBlockSyntax? StatesBlock => Members.OfType<StatesBlockSyntax>().FirstOrDefault();

    /// <summary>
    /// All method declarations in this class.
    /// </summary>
    public IEnumerable<MethodDeclarationSyntax> Methods => Members.OfType<MethodDeclarationSyntax>();

    /// <summary>
    /// All field declarations in this class.
    /// </summary>
    public IEnumerable<FieldDeclarationSyntax> Fields => Members.OfType<FieldDeclarationSyntax>();

    public override SyntaxNodeKind Kind => SyntaxNodeKind.ClassDeclaration;

    public ClassDeclarationSyntax(
        ImmutableArray<SyntaxToken> modifiers,
        SyntaxToken classKeyword,
        SyntaxToken identifier,
        BaseListSyntax? baseList,
        SyntaxToken? replacesKeyword,
        SyntaxToken? replacesIdentifier,
        SyntaxToken? editorNumber,
        SyntaxToken openBraceToken,
        ImmutableArray<MemberDeclarationSyntax> members,
        SyntaxToken closeBraceToken)
        : base(
            ComputeSpanForClass(modifiers, classKeyword, closeBraceToken),
            ComputeFullSpanForClass(modifiers, classKeyword, closeBraceToken))
    {
        Modifiers = modifiers;
        ClassKeyword = classKeyword;
        Identifier = identifier;
        BaseList = baseList;
        ReplacesKeyword = replacesKeyword;
        ReplacesIdentifier = replacesIdentifier;
        EditorNumber = editorNumber;
        OpenBraceToken = openBraceToken;
        Members = members;
        CloseBraceToken = closeBraceToken;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        if (BaseList != null) yield return BaseList;
        foreach (var member in Members)
            yield return member;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        foreach (var mod in Modifiers)
            yield return mod;
        yield return ClassKeyword;
        yield return Identifier;
        if (ReplacesKeyword.HasValue) yield return ReplacesKeyword.Value;
        if (ReplacesIdentifier.HasValue) yield return ReplacesIdentifier.Value;
        if (EditorNumber.HasValue) yield return EditorNumber.Value;
        yield return OpenBraceToken;
        yield return CloseBraceToken;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitClassDeclaration(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitClassDeclaration(this);

    private static TextSpan ComputeSpanForClass(
        ImmutableArray<SyntaxToken> modifiers,
        SyntaxToken classKeyword,
        SyntaxToken closeBrace)
    {
        var first = modifiers.Length > 0 ? modifiers[0].Span : classKeyword.Span;
        return ComputeSpan(first, closeBrace.Span);
    }

    private static TextSpan ComputeFullSpanForClass(
        ImmutableArray<SyntaxToken> modifiers,
        SyntaxToken classKeyword,
        SyntaxToken closeBrace)
    {
        var first = modifiers.Length > 0 ? modifiers[0].FullSpan : classKeyword.FullSpan;
        return ComputeSpan(first, closeBrace.FullSpan);
    }

    private static TextSpan ComputeSpan(TextSpan first, TextSpan last)
    {
        return new TextSpan(first.Start, last.End - first.Start);
    }
}

/// <summary>
/// A DECORATE actor declaration such as <c>actor ZombieMan : DoomMonster replaces ZombieMan 3004 { ... }</c>.
/// </summary>
public sealed class ActorDeclarationSyntax : MemberDeclarationSyntax
{
    /// <summary>
    /// The <c>actor</c> keyword.
    /// </summary>
    public SyntaxToken ActorKeyword { get; }

    /// <summary>
    /// The actor name identifier token.
    /// </summary>
    public SyntaxToken Identifier { get; }

    /// <summary>
    /// The optional <c>:</c> token for base class inheritance.
    /// </summary>
    public SyntaxToken? ColonToken { get; }

    /// <summary>
    /// The optional base actor identifier.
    /// </summary>
    public SyntaxToken? BaseIdentifier { get; }

    /// <summary>
    /// The optional <c>replaces</c> keyword.
    /// </summary>
    public SyntaxToken? ReplacesKeyword { get; }

    /// <summary>
    /// The optional identifier of the actor being replaced.
    /// </summary>
    public SyntaxToken? ReplacesIdentifier { get; }

    /// <summary>
    /// The optional DoomEdNum (editor number).
    /// </summary>
    public SyntaxToken? EditorNumber { get; }

    /// <summary>
    /// The <c>{</c> token.
    /// </summary>
    public SyntaxToken OpenBraceToken { get; }

    /// <summary>
    /// The body items of the actor declaration.
    /// </summary>
    public ImmutableArray<MemberDeclarationSyntax> Body { get; }

    /// <summary>
    /// The <c>}</c> token.
    /// </summary>
    public SyntaxToken CloseBraceToken { get; }

    /// <summary>
    /// The actor name text.
    /// </summary>
    public string Name => Identifier.Text;

    /// <summary>
    /// The base actor name, or null if no inheritance.
    /// </summary>
    public string? BaseName => BaseIdentifier?.Text;

    /// <summary>
    /// The name of the actor being replaced, or null if not replacing.
    /// </summary>
    public string? ReplacesName => ReplacesIdentifier?.Text;

    /// <summary>
    /// The DoomEdNum value, or null if not specified.
    /// </summary>
    public int? DoomEdNumber => EditorNumber?.Value is long lv ? (int)lv : EditorNumber?.Value as int?;

    public override SyntaxNodeKind Kind => SyntaxNodeKind.ActorDeclaration;

    public ActorDeclarationSyntax(
        SyntaxToken actorKeyword,
        SyntaxToken identifier,
        SyntaxToken? colonToken,
        SyntaxToken? baseIdentifier,
        SyntaxToken? replacesKeyword,
        SyntaxToken? replacesIdentifier,
        SyntaxToken? editorNumber,
        SyntaxToken openBraceToken,
        ImmutableArray<MemberDeclarationSyntax> body,
        SyntaxToken closeBraceToken)
        : base(
            ComputeSpan(actorKeyword.Span, closeBraceToken.Span),
            ComputeSpan(actorKeyword.FullSpan, closeBraceToken.FullSpan))
    {
        ActorKeyword = actorKeyword;
        Identifier = identifier;
        ColonToken = colonToken;
        BaseIdentifier = baseIdentifier;
        ReplacesKeyword = replacesKeyword;
        ReplacesIdentifier = replacesIdentifier;
        EditorNumber = editorNumber;
        OpenBraceToken = openBraceToken;
        Body = body;
        CloseBraceToken = closeBraceToken;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        foreach (var item in Body)
            yield return item;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        yield return ActorKeyword;
        yield return Identifier;
        if (ColonToken.HasValue) yield return ColonToken.Value;
        if (BaseIdentifier.HasValue) yield return BaseIdentifier.Value;
        if (ReplacesKeyword.HasValue) yield return ReplacesKeyword.Value;
        if (ReplacesIdentifier.HasValue) yield return ReplacesIdentifier.Value;
        if (EditorNumber.HasValue) yield return EditorNumber.Value;
        yield return OpenBraceToken;
        yield return CloseBraceToken;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitActorDeclaration(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitActorDeclaration(this);

    private static TextSpan ComputeSpan(TextSpan first, TextSpan last)
    {
        return new TextSpan(first.Start, last.End - first.Start);
    }
}

/// <summary>
/// A base class list clause such as <c>: PlayerPawn</c>.
/// </summary>
public sealed class BaseListSyntax : SyntaxNode
{
    /// <summary>
    /// The <c>:</c> token.
    /// </summary>
    public SyntaxToken ColonToken { get; }

    /// <summary>
    /// The base type.
    /// </summary>
    public TypeSyntax BaseType { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.BaseList;

    public BaseListSyntax(SyntaxToken colonToken, TypeSyntax baseType)
        : base(
            ComputeSpan(colonToken.Span, baseType.Span),
            ComputeSpan(colonToken.FullSpan, baseType.FullSpan))
    {
        ColonToken = colonToken;
        BaseType = baseType;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        yield return BaseType;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        yield return ColonToken;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitBaseList(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitBaseList(this);

    private static TextSpan ComputeSpan(TextSpan first, TextSpan last)
    {
        return new TextSpan(first.Start, last.End - first.Start);
    }
}

/// <summary>
/// A Default block containing property assignments and flag definitions.
/// </summary>
public sealed class DefaultBlockSyntax : MemberDeclarationSyntax
{
    /// <summary>
    /// The <c>Default</c> keyword.
    /// </summary>
    public SyntaxToken DefaultKeyword { get; }

    /// <summary>
    /// The <c>{</c> token.
    /// </summary>
    public SyntaxToken OpenBraceToken { get; }

    /// <summary>
    /// The items within the Default block.
    /// </summary>
    public ImmutableArray<DefaultItemSyntax> Items { get; }

    /// <summary>
    /// The <c>}</c> token.
    /// </summary>
    public SyntaxToken CloseBraceToken { get; }

    /// <summary>
    /// All property assignments in this Default block.
    /// </summary>
    public IEnumerable<PropertyAssignmentSyntax> Properties =>
        Items.OfType<PropertyAssignmentSyntax>();

    /// <summary>
    /// All flag definitions in this Default block.
    /// </summary>
    public IEnumerable<FlagDefinitionSyntax> Flags =>
        Items.OfType<FlagDefinitionSyntax>();

    public override SyntaxNodeKind Kind => SyntaxNodeKind.DefaultBlock;

    public DefaultBlockSyntax(
        SyntaxToken defaultKeyword,
        SyntaxToken openBraceToken,
        ImmutableArray<DefaultItemSyntax> items,
        SyntaxToken closeBraceToken)
        : base(
            ComputeSpan(defaultKeyword.Span, closeBraceToken.Span),
            ComputeSpan(defaultKeyword.FullSpan, closeBraceToken.FullSpan))
    {
        DefaultKeyword = defaultKeyword;
        OpenBraceToken = openBraceToken;
        Items = items;
        CloseBraceToken = closeBraceToken;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        foreach (var item in Items)
            yield return item;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        yield return DefaultKeyword;
        yield return OpenBraceToken;
        yield return CloseBraceToken;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitDefaultBlock(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitDefaultBlock(this);

    private static TextSpan ComputeSpan(TextSpan first, TextSpan last)
    {
        return new TextSpan(first.Start, last.End - first.Start);
    }
}

/// <summary>
/// A property assignment inside a Default block, such as <c>Monster.Health 100;</c>.
/// </summary>
public sealed class PropertyAssignmentSyntax : DefaultItemSyntax
{
    /// <summary>
    /// The optional prefix identifier (e.g., <c>Monster</c> in <c>Monster.Health</c>).
    /// </summary>
    public SyntaxToken? PrefixIdentifier { get; }

    /// <summary>
    /// The optional <c>.</c> token.
    /// </summary>
    public SyntaxToken? DotToken { get; }

    /// <summary>
    /// The property name token.
    /// </summary>
    public SyntaxToken PropertyName { get; }

    /// <summary>
    /// The property value expressions.
    /// </summary>
    public ImmutableArray<ExpressionSyntax> Values { get; }

    /// <summary>
    /// The <c>;</c> token.
    /// </summary>
    public SyntaxToken SemicolonToken { get; }

    /// <summary>
    /// The full property name including any prefix (e.g., <c>Monster.Health</c>).
    /// </summary>
    public string FullPropertyName =>
        PrefixIdentifier.HasValue ? $"{PrefixIdentifier.Value.Text}.{PropertyName.Text}" : PropertyName.Text;

    public override SyntaxNodeKind Kind => SyntaxNodeKind.PropertyAssignment;

    public PropertyAssignmentSyntax(
        SyntaxToken? prefixIdentifier,
        SyntaxToken? dotToken,
        SyntaxToken propertyName,
        ImmutableArray<ExpressionSyntax> values,
        SyntaxToken semicolonToken)
        : base(
            ComputeSpanForProp(prefixIdentifier, propertyName, semicolonToken),
            ComputeFullSpanForProp(prefixIdentifier, propertyName, semicolonToken))
    {
        PrefixIdentifier = prefixIdentifier;
        DotToken = dotToken;
        PropertyName = propertyName;
        Values = values;
        SemicolonToken = semicolonToken;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        foreach (var value in Values)
            yield return value;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        if (PrefixIdentifier.HasValue) yield return PrefixIdentifier.Value;
        if (DotToken.HasValue) yield return DotToken.Value;
        yield return PropertyName;
        yield return SemicolonToken;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitPropertyAssignment(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitPropertyAssignment(this);

    private static TextSpan ComputeSpanForProp(
        SyntaxToken? prefix,
        SyntaxToken propertyName,
        SyntaxToken semicolon)
    {
        var first = prefix.HasValue ? prefix.Value.Span : propertyName.Span;
        return ComputeSpan(first, semicolon.Span);
    }

    private static TextSpan ComputeFullSpanForProp(
        SyntaxToken? prefix,
        SyntaxToken propertyName,
        SyntaxToken semicolon)
    {
        var first = prefix.HasValue ? prefix.Value.FullSpan : propertyName.FullSpan;
        return ComputeSpan(first, semicolon.FullSpan);
    }

    private static TextSpan ComputeSpan(TextSpan first, TextSpan last)
    {
        return new TextSpan(first.Start, last.End - first.Start);
    }
}

/// <summary>
/// A flag definition inside a Default block, such as <c>+SOLID</c> or <c>-NOGRAVITY</c>.
/// </summary>
public sealed class FlagDefinitionSyntax : DefaultItemSyntax
{
    /// <summary>
    /// The <c>+</c> or <c>-</c> token indicating whether the flag is set or cleared.
    /// </summary>
    public SyntaxToken PlusOrMinus { get; }

    /// <summary>
    /// The optional prefix identifier (e.g., <c>Monster</c> in <c>+Monster.SOLID</c>).
    /// </summary>
    public SyntaxToken? PrefixIdentifier { get; }

    /// <summary>
    /// The optional <c>.</c> token.
    /// </summary>
    public SyntaxToken? DotToken { get; }

    /// <summary>
    /// The flag name token.
    /// </summary>
    public SyntaxToken FlagName { get; }

    /// <summary>
    /// The optional <c>;</c> token.
    /// </summary>
    public SyntaxToken? SemicolonToken { get; }

    /// <summary>
    /// Returns <see langword="true"/> if the flag is being set (<c>+</c>).
    /// </summary>
    public bool IsSet => PlusOrMinus.Kind == SyntaxTokenKind.Plus;

    /// <summary>
    /// The full flag name including any prefix (e.g., <c>Monster.SOLID</c>).
    /// </summary>
    public string FullFlagName =>
        PrefixIdentifier.HasValue ? $"{PrefixIdentifier.Value.Text}.{FlagName.Text}" : FlagName.Text;

    public override SyntaxNodeKind Kind => SyntaxNodeKind.FlagDefinition;

    public FlagDefinitionSyntax(
        SyntaxToken plusOrMinus,
        SyntaxToken? prefixIdentifier,
        SyntaxToken? dotToken,
        SyntaxToken flagName,
        SyntaxToken? semicolonToken)
        : base(
            ComputeSpan(plusOrMinus.Span, ComputeLastSpanForFlag(flagName, semicolonToken)),
            ComputeSpan(plusOrMinus.FullSpan, ComputeLastFullSpanForFlag(flagName, semicolonToken)))
    {
        PlusOrMinus = plusOrMinus;
        PrefixIdentifier = prefixIdentifier;
        DotToken = dotToken;
        FlagName = flagName;
        SemicolonToken = semicolonToken;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        yield break;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        yield return PlusOrMinus;
        if (PrefixIdentifier.HasValue) yield return PrefixIdentifier.Value;
        if (DotToken.HasValue) yield return DotToken.Value;
        yield return FlagName;
        if (SemicolonToken.HasValue) yield return SemicolonToken.Value;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitFlagDefinition(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitFlagDefinition(this);

    private static TextSpan ComputeLastSpanForFlag(SyntaxToken flagName, SyntaxToken? semicolon) =>
        semicolon.HasValue ? semicolon.Value.Span : flagName.Span;

    private static TextSpan ComputeLastFullSpanForFlag(SyntaxToken flagName, SyntaxToken? semicolon) =>
        semicolon.HasValue ? semicolon.Value.FullSpan : flagName.FullSpan;

    private static TextSpan ComputeSpan(TextSpan first, TextSpan last)
    {
        return new TextSpan(first.Start, last.End - first.Start);
    }
}

/// <summary>
/// A States block containing state labels, frames, and flow control directives.
/// </summary>
public sealed class StatesBlockSyntax : MemberDeclarationSyntax
{
    /// <summary>
    /// The <c>States</c> keyword.
    /// </summary>
    public SyntaxToken StatesKeyword { get; }

    /// <summary>
    /// The optional <c>(</c> token for state options.
    /// </summary>
    public SyntaxToken? OpenParenToken { get; }

    /// <summary>
    /// The state option tokens (e.g., <c>Actor</c>, <c>Weapon</c>, <c>Item</c>).
    /// </summary>
    public ImmutableArray<SyntaxToken> StateOptions { get; }

    /// <summary>
    /// The optional <c>)</c> token.
    /// </summary>
    public SyntaxToken? CloseParenToken { get; }

    /// <summary>
    /// The <c>{</c> token.
    /// </summary>
    public SyntaxToken OpenBraceToken { get; }

    /// <summary>
    /// The state entries (labels, frames, flow control directives).
    /// </summary>
    public ImmutableArray<StateSyntax> States { get; }

    /// <summary>
    /// The <c>}</c> token.
    /// </summary>
    public SyntaxToken CloseBraceToken { get; }

    /// <summary>
    /// All state labels in this States block.
    /// </summary>
    public IEnumerable<StateLabelSyntax> Labels => States.OfType<StateLabelSyntax>();

    /// <summary>
    /// All state frames in this States block.
    /// </summary>
    public IEnumerable<StateFrameSyntax> Frames => States.OfType<StateFrameSyntax>();

    public override SyntaxNodeKind Kind => SyntaxNodeKind.StatesBlock;

    public StatesBlockSyntax(
        SyntaxToken statesKeyword,
        SyntaxToken? openParenToken,
        ImmutableArray<SyntaxToken> stateOptions,
        SyntaxToken? closeParenToken,
        SyntaxToken openBraceToken,
        ImmutableArray<StateSyntax> states,
        SyntaxToken closeBraceToken)
        : base(
            ComputeSpan(statesKeyword.Span, closeBraceToken.Span),
            ComputeSpan(statesKeyword.FullSpan, closeBraceToken.FullSpan))
    {
        StatesKeyword = statesKeyword;
        OpenParenToken = openParenToken;
        StateOptions = stateOptions;
        CloseParenToken = closeParenToken;
        OpenBraceToken = openBraceToken;
        States = states;
        CloseBraceToken = closeBraceToken;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        foreach (var state in States)
            yield return state;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        yield return StatesKeyword;
        if (OpenParenToken.HasValue) yield return OpenParenToken.Value;
        foreach (var opt in StateOptions)
            yield return opt;
        if (CloseParenToken.HasValue) yield return CloseParenToken.Value;
        yield return OpenBraceToken;
        yield return CloseBraceToken;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitStatesBlock(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitStatesBlock(this);

    private static TextSpan ComputeSpan(TextSpan first, TextSpan last)
    {
        return new TextSpan(first.Start, last.End - first.Start);
    }
}

/// <summary>
/// A method (function) declaration.
/// </summary>
public sealed class MethodDeclarationSyntax : MemberDeclarationSyntax
{
    /// <summary>
    /// Access and other modifier tokens (e.g., <c>virtual</c>, <c>override</c>, <c>action</c>).
    /// </summary>
    public ImmutableArray<SyntaxToken> Modifiers { get; }

    /// <summary>
    /// The return type.
    /// </summary>
    public TypeSyntax ReturnType { get; }

    /// <summary>
    /// The method name identifier token.
    /// </summary>
    public SyntaxToken Identifier { get; }

    /// <summary>
    /// The <c>(</c> token.
    /// </summary>
    public SyntaxToken OpenParenToken { get; }

    /// <summary>
    /// The comma-separated list of parameters.
    /// </summary>
    public SeparatedSyntaxList<ParameterSyntax> Parameters { get; }

    /// <summary>
    /// The <c>)</c> token.
    /// </summary>
    public SyntaxToken CloseParenToken { get; }

    /// <summary>
    /// The optional <c>const</c> keyword.
    /// </summary>
    public SyntaxToken? ConstKeyword { get; }

    /// <summary>
    /// The optional method body block.
    /// </summary>
    public BlockStatementSyntax? Body { get; }

    /// <summary>
    /// The optional <c>;</c> token (for abstract/native methods without a body).
    /// </summary>
    public SyntaxToken? SemicolonToken { get; }

    /// <summary>
    /// The method name text.
    /// </summary>
    public string Name => Identifier.Text;

    /// <summary>
    /// Returns <see langword="true"/> if the method has the <c>virtual</c> modifier.
    /// </summary>
    public bool IsVirtual => HasModifier(SyntaxTokenKind.VirtualKeyword);

    /// <summary>
    /// Returns <see langword="true"/> if the method has the <c>override</c> modifier.
    /// </summary>
    public bool IsOverride => HasModifier(SyntaxTokenKind.OverrideKeyword);

    /// <summary>
    /// Returns <see langword="true"/> if the method has the <c>native</c> modifier.
    /// </summary>
    public bool IsNative => HasModifier(SyntaxTokenKind.NativeKeyword);

    /// <summary>
    /// Returns <see langword="true"/> if the method has the <c>action</c> modifier.
    /// Note: "action" is parsed as an identifier token in some contexts.
    /// </summary>
    public bool IsAction
    {
        get
        {
            foreach (var mod in Modifiers)
            {
                if (string.Equals(mod.Text, "action", System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.MethodDeclaration;

    public MethodDeclarationSyntax(
        ImmutableArray<SyntaxToken> modifiers,
        TypeSyntax returnType,
        SyntaxToken identifier,
        SyntaxToken openParenToken,
        SeparatedSyntaxList<ParameterSyntax> parameters,
        SyntaxToken closeParenToken,
        SyntaxToken? constKeyword,
        BlockStatementSyntax? body,
        SyntaxToken? semicolonToken)
        : base(
            ComputeSpanForMethod(modifiers, returnType, body, semicolonToken, closeParenToken, constKeyword),
            ComputeFullSpanForMethod(modifiers, returnType, body, semicolonToken, closeParenToken, constKeyword))
    {
        Modifiers = modifiers;
        ReturnType = returnType;
        Identifier = identifier;
        OpenParenToken = openParenToken;
        Parameters = parameters;
        CloseParenToken = closeParenToken;
        ConstKeyword = constKeyword;
        Body = body;
        SemicolonToken = semicolonToken;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        yield return ReturnType;
        foreach (var param in Parameters)
            yield return param;
        if (Body != null) yield return Body;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        foreach (var mod in Modifiers)
            yield return mod;
        yield return Identifier;
        yield return OpenParenToken;
        yield return CloseParenToken;
        if (ConstKeyword.HasValue) yield return ConstKeyword.Value;
        if (SemicolonToken.HasValue) yield return SemicolonToken.Value;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitMethodDeclaration(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitMethodDeclaration(this);

    private bool HasModifier(SyntaxTokenKind kind)
    {
        foreach (var mod in Modifiers)
        {
            if (mod.Kind == kind) return true;
        }
        return false;
    }

    private static TextSpan ComputeSpanForMethod(
        ImmutableArray<SyntaxToken> modifiers,
        TypeSyntax returnType,
        BlockStatementSyntax? body,
        SyntaxToken? semicolon,
        SyntaxToken closeParen,
        SyntaxToken? constKeyword)
    {
        var first = modifiers.Length > 0 ? modifiers[0].Span : returnType.Span;
        TextSpan last;
        if (body != null) last = body.Span;
        else if (semicolon.HasValue) last = semicolon.Value.Span;
        else if (constKeyword.HasValue) last = constKeyword.Value.Span;
        else last = closeParen.Span;
        return ComputeSpan(first, last);
    }

    private static TextSpan ComputeFullSpanForMethod(
        ImmutableArray<SyntaxToken> modifiers,
        TypeSyntax returnType,
        BlockStatementSyntax? body,
        SyntaxToken? semicolon,
        SyntaxToken closeParen,
        SyntaxToken? constKeyword)
    {
        var first = modifiers.Length > 0 ? modifiers[0].FullSpan : returnType.FullSpan;
        TextSpan last;
        if (body != null) last = body.FullSpan;
        else if (semicolon.HasValue) last = semicolon.Value.FullSpan;
        else if (constKeyword.HasValue) last = constKeyword.Value.FullSpan;
        else last = closeParen.FullSpan;
        return ComputeSpan(first, last);
    }

    private static TextSpan ComputeSpan(TextSpan first, TextSpan last)
    {
        return new TextSpan(first.Start, last.End - first.Start);
    }
}

/// <summary>
/// A field declaration such as <c>int health;</c> or <c>private double speed = 1.0;</c>.
/// </summary>
public sealed class FieldDeclarationSyntax : MemberDeclarationSyntax
{
    /// <summary>
    /// Access and other modifier tokens.
    /// </summary>
    public ImmutableArray<SyntaxToken> Modifiers { get; }

    /// <summary>
    /// The field type.
    /// </summary>
    public TypeSyntax Type { get; }

    /// <summary>
    /// The comma-separated list of variable declarators.
    /// </summary>
    public SeparatedSyntaxList<VariableDeclaratorSyntax> Variables { get; }

    /// <summary>
    /// The <c>;</c> token.
    /// </summary>
    public SyntaxToken SemicolonToken { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.FieldDeclaration;

    public FieldDeclarationSyntax(
        ImmutableArray<SyntaxToken> modifiers,
        TypeSyntax type,
        SeparatedSyntaxList<VariableDeclaratorSyntax> variables,
        SyntaxToken semicolonToken)
        : base(
            ComputeSpanForField(modifiers, type, semicolonToken),
            ComputeFullSpanForField(modifiers, type, semicolonToken))
    {
        Modifiers = modifiers;
        Type = type;
        Variables = variables;
        SemicolonToken = semicolonToken;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        yield return Type;
        foreach (var variable in Variables)
            yield return variable;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        foreach (var mod in Modifiers)
            yield return mod;
        yield return SemicolonToken;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitFieldDeclaration(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitFieldDeclaration(this);

    private static TextSpan ComputeSpanForField(
        ImmutableArray<SyntaxToken> modifiers,
        TypeSyntax type,
        SyntaxToken semicolon)
    {
        var first = modifiers.Length > 0 ? modifiers[0].Span : type.Span;
        return ComputeSpan(first, semicolon.Span);
    }

    private static TextSpan ComputeFullSpanForField(
        ImmutableArray<SyntaxToken> modifiers,
        TypeSyntax type,
        SyntaxToken semicolon)
    {
        var first = modifiers.Length > 0 ? modifiers[0].FullSpan : type.FullSpan;
        return ComputeSpan(first, semicolon.FullSpan);
    }

    private static TextSpan ComputeSpan(TextSpan first, TextSpan last)
    {
        return new TextSpan(first.Start, last.End - first.Start);
    }
}

/// <summary>
/// A method parameter such as <c>int damage</c> or <c>out double result = 0</c>.
/// </summary>
public sealed class ParameterSyntax : SyntaxNode
{
    /// <summary>
    /// Parameter modifier tokens (e.g., <c>out</c>, <c>in</c>).
    /// </summary>
    public ImmutableArray<SyntaxToken> Modifiers { get; }

    /// <summary>
    /// The parameter type.
    /// </summary>
    public TypeSyntax Type { get; }

    /// <summary>
    /// The parameter name identifier token.
    /// </summary>
    public SyntaxToken Identifier { get; }

    /// <summary>
    /// The optional <c>=</c> token for default values.
    /// </summary>
    public SyntaxToken? EqualsToken { get; }

    /// <summary>
    /// The optional default value expression.
    /// </summary>
    public ExpressionSyntax? DefaultValue { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.Parameter;

    public ParameterSyntax(
        ImmutableArray<SyntaxToken> modifiers,
        TypeSyntax type,
        SyntaxToken identifier,
        SyntaxToken? equalsToken = null,
        ExpressionSyntax? defaultValue = null)
        : base(
            ComputeSpanForParam(modifiers, type, identifier, defaultValue),
            ComputeFullSpanForParam(modifiers, type, identifier, defaultValue))
    {
        Modifiers = modifiers;
        Type = type;
        Identifier = identifier;
        EqualsToken = equalsToken;
        DefaultValue = defaultValue;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        yield return Type;
        if (DefaultValue != null)
            yield return DefaultValue;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        foreach (var mod in Modifiers)
            yield return mod;
        yield return Identifier;
        if (EqualsToken.HasValue) yield return EqualsToken.Value;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitParameter(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitParameter(this);

    private static TextSpan ComputeSpanForParam(
        ImmutableArray<SyntaxToken> modifiers,
        TypeSyntax type,
        SyntaxToken identifier,
        ExpressionSyntax? defaultValue)
    {
        var first = modifiers.Length > 0 ? modifiers[0].Span : type.Span;
        var last = defaultValue?.Span ?? identifier.Span;
        return ComputeSpan(first, last);
    }

    private static TextSpan ComputeFullSpanForParam(
        ImmutableArray<SyntaxToken> modifiers,
        TypeSyntax type,
        SyntaxToken identifier,
        ExpressionSyntax? defaultValue)
    {
        var first = modifiers.Length > 0 ? modifiers[0].FullSpan : type.FullSpan;
        var last = defaultValue?.FullSpan ?? identifier.FullSpan;
        return ComputeSpan(first, last);
    }

    private static TextSpan ComputeSpan(TextSpan first, TextSpan last)
    {
        return new TextSpan(first.Start, last.End - first.Start);
    }
}

/// <summary>
/// A variable declarator such as <c>x = 5</c> within a field or local declaration.
/// </summary>
public sealed class VariableDeclaratorSyntax : SyntaxNode
{
    /// <summary>
    /// The identifier token naming the variable.
    /// </summary>
    public SyntaxToken Identifier { get; }

    /// <summary>
    /// The optional <c>=</c> token.
    /// </summary>
    public SyntaxToken? EqualsToken { get; }

    /// <summary>
    /// The optional initializer expression.
    /// </summary>
    public ExpressionSyntax? Initializer { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.VariableDeclarator;

    public VariableDeclaratorSyntax(
        SyntaxToken identifier,
        SyntaxToken? equalsToken = null,
        ExpressionSyntax? initializer = null)
        : base(
            ComputeSpanForVar(identifier, initializer),
            ComputeFullSpanForVar(identifier, initializer))
    {
        Identifier = identifier;
        EqualsToken = equalsToken;
        Initializer = initializer;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        if (Initializer != null)
            yield return Initializer;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        yield return Identifier;
        if (EqualsToken.HasValue)
            yield return EqualsToken.Value;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitVariableDeclarator(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitVariableDeclarator(this);

    private static TextSpan ComputeSpanForVar(SyntaxToken identifier, ExpressionSyntax? initializer)
    {
        var last = initializer?.Span ?? identifier.Span;
        return ComputeSpan(identifier.Span, last);
    }

    private static TextSpan ComputeFullSpanForVar(SyntaxToken identifier, ExpressionSyntax? initializer)
    {
        var last = initializer?.FullSpan ?? identifier.FullSpan;
        return ComputeSpan(identifier.FullSpan, last);
    }

    private static TextSpan ComputeSpan(TextSpan first, TextSpan last)
    {
        return new TextSpan(first.Start, last.End - first.Start);
    }
}

/// <summary>
/// A constant declaration such as <c>const int MAX_HEALTH = 100;</c>.
/// </summary>
public sealed class ConstDeclarationSyntax : MemberDeclarationSyntax
{
    /// <summary>
    /// The <c>const</c> keyword.
    /// </summary>
    public SyntaxToken ConstKeyword { get; }

    /// <summary>
    /// The constant type.
    /// </summary>
    public TypeSyntax Type { get; }

    /// <summary>
    /// The constant name identifier token.
    /// </summary>
    public SyntaxToken Identifier { get; }

    /// <summary>
    /// The <c>=</c> token.
    /// </summary>
    public SyntaxToken EqualsToken { get; }

    /// <summary>
    /// The constant value expression.
    /// </summary>
    public ExpressionSyntax Value { get; }

    /// <summary>
    /// The <c>;</c> token.
    /// </summary>
    public SyntaxToken SemicolonToken { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.ConstDeclaration;

    public ConstDeclarationSyntax(
        SyntaxToken constKeyword,
        TypeSyntax type,
        SyntaxToken identifier,
        SyntaxToken equalsToken,
        ExpressionSyntax value,
        SyntaxToken semicolonToken)
        : base(
            ComputeSpan(constKeyword.Span, semicolonToken.Span),
            ComputeSpan(constKeyword.FullSpan, semicolonToken.FullSpan))
    {
        ConstKeyword = constKeyword;
        Type = type;
        Identifier = identifier;
        EqualsToken = equalsToken;
        Value = value;
        SemicolonToken = semicolonToken;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        yield return Type;
        yield return Value;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        yield return ConstKeyword;
        yield return Identifier;
        yield return EqualsToken;
        yield return SemicolonToken;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitConstDeclaration(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitConstDeclaration(this);

    private static TextSpan ComputeSpan(TextSpan first, TextSpan last)
    {
        return new TextSpan(first.Start, last.End - first.Start);
    }
}

/// <summary>
/// A struct declaration such as <c>struct FLineTraceData { ... }</c>.
/// </summary>
public sealed class StructDeclarationSyntax : MemberDeclarationSyntax
{
    /// <summary>
    /// Access and other modifier tokens.
    /// </summary>
    public ImmutableArray<SyntaxToken> Modifiers { get; }

    /// <summary>
    /// The <c>struct</c> keyword.
    /// </summary>
    public SyntaxToken StructKeyword { get; }

    /// <summary>
    /// The struct name identifier token.
    /// </summary>
    public SyntaxToken Identifier { get; }

    /// <summary>
    /// The <c>{</c> token.
    /// </summary>
    public SyntaxToken OpenBraceToken { get; }

    /// <summary>
    /// The member declarations within the struct body.
    /// </summary>
    public ImmutableArray<MemberDeclarationSyntax> Members { get; }

    /// <summary>
    /// The <c>}</c> token.
    /// </summary>
    public SyntaxToken CloseBraceToken { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.StructDeclaration;

    public StructDeclarationSyntax(
        ImmutableArray<SyntaxToken> modifiers,
        SyntaxToken structKeyword,
        SyntaxToken identifier,
        SyntaxToken openBraceToken,
        ImmutableArray<MemberDeclarationSyntax> members,
        SyntaxToken closeBraceToken)
        : base(
            ComputeSpanForStruct(modifiers, structKeyword, closeBraceToken),
            ComputeFullSpanForStruct(modifiers, structKeyword, closeBraceToken))
    {
        Modifiers = modifiers;
        StructKeyword = structKeyword;
        Identifier = identifier;
        OpenBraceToken = openBraceToken;
        Members = members;
        CloseBraceToken = closeBraceToken;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        foreach (var member in Members)
            yield return member;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        foreach (var mod in Modifiers)
            yield return mod;
        yield return StructKeyword;
        yield return Identifier;
        yield return OpenBraceToken;
        yield return CloseBraceToken;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitStructDeclaration(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitStructDeclaration(this);

    private static TextSpan ComputeSpanForStruct(
        ImmutableArray<SyntaxToken> modifiers,
        SyntaxToken structKeyword,
        SyntaxToken closeBrace)
    {
        var first = modifiers.Length > 0 ? modifiers[0].Span : structKeyword.Span;
        return ComputeSpan(first, closeBrace.Span);
    }

    private static TextSpan ComputeFullSpanForStruct(
        ImmutableArray<SyntaxToken> modifiers,
        SyntaxToken structKeyword,
        SyntaxToken closeBrace)
    {
        var first = modifiers.Length > 0 ? modifiers[0].FullSpan : structKeyword.FullSpan;
        return ComputeSpan(first, closeBrace.FullSpan);
    }

    private static TextSpan ComputeSpan(TextSpan first, TextSpan last)
    {
        return new TextSpan(first.Start, last.End - first.Start);
    }
}

/// <summary>
/// An enum declaration such as <c>enum EFFlags { ... }</c>.
/// </summary>
public sealed class EnumDeclarationSyntax : MemberDeclarationSyntax
{
    /// <summary>
    /// The <c>enum</c> keyword.
    /// </summary>
    public SyntaxToken EnumKeyword { get; }

    /// <summary>
    /// The enum name identifier token.
    /// </summary>
    public SyntaxToken Identifier { get; }

    /// <summary>
    /// The <c>{</c> token.
    /// </summary>
    public SyntaxToken OpenBraceToken { get; }

    /// <summary>
    /// The enum member tokens (identifiers, with optional initializers treated as tokens).
    /// </summary>
    public ImmutableArray<SyntaxToken> Members { get; }

    /// <summary>
    /// The <c>}</c> token.
    /// </summary>
    public SyntaxToken CloseBraceToken { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.EnumDeclaration;

    public EnumDeclarationSyntax(
        SyntaxToken enumKeyword,
        SyntaxToken identifier,
        SyntaxToken openBraceToken,
        ImmutableArray<SyntaxToken> members,
        SyntaxToken closeBraceToken)
        : base(
            ComputeSpan(enumKeyword.Span, closeBraceToken.Span),
            ComputeSpan(enumKeyword.FullSpan, closeBraceToken.FullSpan))
    {
        EnumKeyword = enumKeyword;
        Identifier = identifier;
        OpenBraceToken = openBraceToken;
        Members = members;
        CloseBraceToken = closeBraceToken;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        yield break;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        yield return EnumKeyword;
        yield return Identifier;
        yield return OpenBraceToken;
        foreach (var member in Members)
            yield return member;
        yield return CloseBraceToken;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitEnumDeclaration(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitEnumDeclaration(this);

    private static TextSpan ComputeSpan(TextSpan first, TextSpan last)
    {
        return new TextSpan(first.Start, last.End - first.Start);
    }
}
