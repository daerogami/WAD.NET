using System.Collections.Generic;
using System.Collections.Immutable;

namespace WAD.NET.ZScript.Syntax;

/// <summary>
/// Abstract base class for all type syntax nodes.
/// </summary>
public abstract class TypeSyntax : SyntaxNode
{
    protected TypeSyntax(TextSpan span, TextSpan fullSpan)
        : base(span, fullSpan)
    {
    }
}

/// <summary>
/// A predefined (built-in) type such as <c>int</c>, <c>float</c>, <c>string</c>, <c>bool</c>, etc.
/// </summary>
public sealed class PredefinedTypeSyntax : TypeSyntax
{
    /// <summary>
    /// The keyword token representing this built-in type.
    /// </summary>
    public SyntaxToken Keyword { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.PredefinedType;

    public PredefinedTypeSyntax(SyntaxToken keyword)
        : base(keyword.Span, keyword.FullSpan)
    {
        Keyword = keyword;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        yield break;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        yield return Keyword;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitPredefinedType(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitPredefinedType(this);
}

/// <summary>
/// A named type reference such as <c>Actor</c> or <c>DoomPlayer</c>, optionally with type arguments.
/// </summary>
public sealed class NamedTypeSyntax : TypeSyntax
{
    /// <summary>
    /// The identifier token naming the type.
    /// </summary>
    public SyntaxToken Identifier { get; }

    /// <summary>
    /// Optional type argument list (e.g., <c>&lt;int&gt;</c>).
    /// </summary>
    public TypeArgumentListSyntax? TypeArguments { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.NamedType;

    public NamedTypeSyntax(SyntaxToken identifier, TypeArgumentListSyntax? typeArguments = null)
        : base(
            ComputeSpan(identifier.Span, typeArguments?.Span ?? identifier.Span),
            ComputeSpan(identifier.FullSpan, typeArguments?.FullSpan ?? identifier.FullSpan))
    {
        Identifier = identifier;
        TypeArguments = typeArguments;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        if (TypeArguments != null) yield return TypeArguments;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        yield return Identifier;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitNamedType(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitNamedType(this);

    private static TextSpan ComputeSpan(TextSpan first, TextSpan last)
    {
        return new TextSpan(first.Start, last.End - first.Start);
    }
}

/// <summary>
/// An array type such as <c>Array&lt;int&gt;</c>.
/// </summary>
public sealed class ArrayTypeSyntax : TypeSyntax
{
    /// <summary>
    /// The <c>Array</c> keyword token.
    /// </summary>
    public SyntaxToken ArrayKeyword { get; }

    /// <summary>
    /// The <c>&lt;</c> token.
    /// </summary>
    public SyntaxToken LessThanToken { get; }

    /// <summary>
    /// The element type.
    /// </summary>
    public TypeSyntax ElementType { get; }

    /// <summary>
    /// The <c>&gt;</c> token.
    /// </summary>
    public SyntaxToken GreaterThanToken { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.ArrayType;

    public ArrayTypeSyntax(
        SyntaxToken arrayKeyword,
        SyntaxToken lessThanToken,
        TypeSyntax elementType,
        SyntaxToken greaterThanToken)
        : base(
            ComputeSpan(arrayKeyword.Span, greaterThanToken.Span),
            ComputeSpan(arrayKeyword.FullSpan, greaterThanToken.FullSpan))
    {
        ArrayKeyword = arrayKeyword;
        LessThanToken = lessThanToken;
        ElementType = elementType;
        GreaterThanToken = greaterThanToken;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        yield return ElementType;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        yield return ArrayKeyword;
        yield return LessThanToken;
        yield return GreaterThanToken;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitArrayType(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitArrayType(this);

    private static TextSpan ComputeSpan(TextSpan first, TextSpan last)
    {
        return new TextSpan(first.Start, last.End - first.Start);
    }
}

/// <summary>
/// A map type such as <c>Map&lt;string, int&gt;</c>.
/// </summary>
public sealed class MapTypeSyntax : TypeSyntax
{
    /// <summary>
    /// The <c>Map</c> keyword token.
    /// </summary>
    public SyntaxToken MapKeyword { get; }

    /// <summary>
    /// The <c>&lt;</c> token.
    /// </summary>
    public SyntaxToken LessThanToken { get; }

    /// <summary>
    /// The key type.
    /// </summary>
    public TypeSyntax KeyType { get; }

    /// <summary>
    /// The <c>,</c> token separating key and value types.
    /// </summary>
    public SyntaxToken CommaToken { get; }

    /// <summary>
    /// The value type.
    /// </summary>
    public TypeSyntax ValueType { get; }

    /// <summary>
    /// The <c>&gt;</c> token.
    /// </summary>
    public SyntaxToken GreaterThanToken { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.MapType;

    public MapTypeSyntax(
        SyntaxToken mapKeyword,
        SyntaxToken lessThanToken,
        TypeSyntax keyType,
        SyntaxToken commaToken,
        TypeSyntax valueType,
        SyntaxToken greaterThanToken)
        : base(
            ComputeSpan(mapKeyword.Span, greaterThanToken.Span),
            ComputeSpan(mapKeyword.FullSpan, greaterThanToken.FullSpan))
    {
        MapKeyword = mapKeyword;
        LessThanToken = lessThanToken;
        KeyType = keyType;
        CommaToken = commaToken;
        ValueType = valueType;
        GreaterThanToken = greaterThanToken;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        yield return KeyType;
        yield return ValueType;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        yield return MapKeyword;
        yield return LessThanToken;
        yield return CommaToken;
        yield return GreaterThanToken;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitMapType(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitMapType(this);

    private static TextSpan ComputeSpan(TextSpan first, TextSpan last)
    {
        return new TextSpan(first.Start, last.End - first.Start);
    }
}

/// <summary>
/// A class-constrained type such as <c>Class&lt;Actor&gt;</c>.
/// </summary>
public sealed class ClassTypeSyntax : TypeSyntax
{
    /// <summary>
    /// The <c>Class</c> keyword token.
    /// </summary>
    public SyntaxToken ClassKeyword { get; }

    /// <summary>
    /// The <c>&lt;</c> token.
    /// </summary>
    public SyntaxToken LessThanToken { get; }

    /// <summary>
    /// The constraint type.
    /// </summary>
    public TypeSyntax ConstraintType { get; }

    /// <summary>
    /// The <c>&gt;</c> token.
    /// </summary>
    public SyntaxToken GreaterThanToken { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.ClassType;

    public ClassTypeSyntax(
        SyntaxToken classKeyword,
        SyntaxToken lessThanToken,
        TypeSyntax constraintType,
        SyntaxToken greaterThanToken)
        : base(
            ComputeSpan(classKeyword.Span, greaterThanToken.Span),
            ComputeSpan(classKeyword.FullSpan, greaterThanToken.FullSpan))
    {
        ClassKeyword = classKeyword;
        LessThanToken = lessThanToken;
        ConstraintType = constraintType;
        GreaterThanToken = greaterThanToken;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        yield return ConstraintType;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        yield return ClassKeyword;
        yield return LessThanToken;
        yield return GreaterThanToken;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitClassType(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitClassType(this);

    private static TextSpan ComputeSpan(TextSpan first, TextSpan last)
    {
        return new TextSpan(first.Start, last.End - first.Start);
    }
}

/// <summary>
/// A type argument list such as <c>&lt;int, string&gt;</c>.
/// </summary>
public sealed class TypeArgumentListSyntax : SyntaxNode
{
    /// <summary>
    /// The <c>&lt;</c> token.
    /// </summary>
    public SyntaxToken LessThanToken { get; }

    /// <summary>
    /// The comma-separated list of type arguments.
    /// </summary>
    public SeparatedSyntaxList<TypeSyntax> Arguments { get; }

    /// <summary>
    /// The <c>&gt;</c> token.
    /// </summary>
    public SyntaxToken GreaterThanToken { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.TypeArgumentList;

    public TypeArgumentListSyntax(
        SyntaxToken lessThanToken,
        SeparatedSyntaxList<TypeSyntax> arguments,
        SyntaxToken greaterThanToken)
        : base(
            ComputeSpan(lessThanToken.Span, greaterThanToken.Span),
            ComputeSpan(lessThanToken.FullSpan, greaterThanToken.FullSpan))
    {
        LessThanToken = lessThanToken;
        Arguments = arguments;
        GreaterThanToken = greaterThanToken;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        foreach (var arg in Arguments)
            yield return arg;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        yield return LessThanToken;
        yield return GreaterThanToken;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitTypeArgumentList(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitTypeArgumentList(this);

    private static TextSpan ComputeSpan(TextSpan first, TextSpan last)
    {
        return new TextSpan(first.Start, last.End - first.Start);
    }
}
