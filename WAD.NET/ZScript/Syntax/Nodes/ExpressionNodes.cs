using System.Collections.Generic;
using System.Collections.Immutable;

namespace WAD.NET.ZScript.Syntax;

/// <summary>
/// Abstract base class for all expression syntax nodes.
/// </summary>
public abstract class ExpressionSyntax : SyntaxNode
{
    protected ExpressionSyntax(TextSpan span, TextSpan fullSpan)
        : base(span, fullSpan)
    {
    }
}

/// <summary>
/// A literal expression such as <c>42</c>, <c>3.14</c>, <c>"hello"</c>, <c>true</c>, or <c>null</c>.
/// </summary>
public sealed class LiteralExpressionSyntax : ExpressionSyntax
{
    /// <summary>
    /// The literal token.
    /// </summary>
    public SyntaxToken Token { get; }

    /// <summary>
    /// The semantic value of the literal, derived from the token.
    /// </summary>
    public object? Value => Token.Value;

    public override SyntaxNodeKind Kind => SyntaxNodeKind.LiteralExpression;

    public LiteralExpressionSyntax(SyntaxToken token)
        : base(token.Span, token.FullSpan)
    {
        Token = token;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        yield break;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        yield return Token;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitLiteralExpression(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitLiteralExpression(this);
}

/// <summary>
/// An identifier used as an expression, such as a variable name or type reference.
/// </summary>
public sealed class IdentifierExpressionSyntax : ExpressionSyntax
{
    /// <summary>
    /// The identifier token.
    /// </summary>
    public SyntaxToken Identifier { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.IdentifierExpression;

    public IdentifierExpressionSyntax(SyntaxToken identifier)
        : base(identifier.Span, identifier.FullSpan)
    {
        Identifier = identifier;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        yield break;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        yield return Identifier;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitIdentifierExpression(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitIdentifierExpression(this);
}

/// <summary>
/// A member access expression such as <c>self.health</c>.
/// </summary>
public sealed class MemberAccessExpressionSyntax : ExpressionSyntax
{
    /// <summary>
    /// The expression on the left side of the dot.
    /// </summary>
    public ExpressionSyntax Expression { get; }

    /// <summary>
    /// The <c>.</c> token.
    /// </summary>
    public SyntaxToken DotToken { get; }

    /// <summary>
    /// The member name token on the right side of the dot.
    /// </summary>
    public SyntaxToken Name { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.MemberAccessExpression;

    public MemberAccessExpressionSyntax(
        ExpressionSyntax expression,
        SyntaxToken dotToken,
        SyntaxToken name)
        : base(
            ComputeSpan(expression.Span, name.Span),
            ComputeSpan(expression.FullSpan, name.FullSpan))
    {
        Expression = expression;
        DotToken = dotToken;
        Name = name;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        yield return Expression;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        yield return DotToken;
        yield return Name;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitMemberAccessExpression(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitMemberAccessExpression(this);

    private static TextSpan ComputeSpan(TextSpan first, TextSpan last)
    {
        return new TextSpan(first.Start, last.End - first.Start);
    }
}

/// <summary>
/// A method or function invocation expression such as <c>A_FireBullets(5.6, 0)</c>.
/// </summary>
public sealed class InvocationExpressionSyntax : ExpressionSyntax
{
    /// <summary>
    /// The expression being invoked (typically an identifier or member access).
    /// </summary>
    public ExpressionSyntax Expression { get; }

    /// <summary>
    /// The <c>(</c> token.
    /// </summary>
    public SyntaxToken OpenParenToken { get; }

    /// <summary>
    /// The comma-separated list of arguments.
    /// </summary>
    public SeparatedSyntaxList<ArgumentSyntax> Arguments { get; }

    /// <summary>
    /// The <c>)</c> token.
    /// </summary>
    public SyntaxToken CloseParenToken { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.InvocationExpression;

    public InvocationExpressionSyntax(
        ExpressionSyntax expression,
        SyntaxToken openParenToken,
        SeparatedSyntaxList<ArgumentSyntax> arguments,
        SyntaxToken closeParenToken)
        : base(
            ComputeSpan(expression.Span, closeParenToken.Span),
            ComputeSpan(expression.FullSpan, closeParenToken.FullSpan))
    {
        Expression = expression;
        OpenParenToken = openParenToken;
        Arguments = arguments;
        CloseParenToken = closeParenToken;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        yield return Expression;
        foreach (var arg in Arguments)
            yield return arg;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        yield return OpenParenToken;
        yield return CloseParenToken;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitInvocationExpression(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitInvocationExpression(this);

    private static TextSpan ComputeSpan(TextSpan first, TextSpan last)
    {
        return new TextSpan(first.Start, last.End - first.Start);
    }
}

/// <summary>
/// A binary expression such as <c>a + b</c>, <c>x == 0</c>, or <c>flags &amp; MASK</c>.
/// </summary>
public sealed class BinaryExpressionSyntax : ExpressionSyntax
{
    /// <summary>
    /// The left-hand operand.
    /// </summary>
    public ExpressionSyntax Left { get; }

    /// <summary>
    /// The operator token.
    /// </summary>
    public SyntaxToken OperatorToken { get; }

    /// <summary>
    /// The right-hand operand.
    /// </summary>
    public ExpressionSyntax Right { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.BinaryExpression;

    public BinaryExpressionSyntax(
        ExpressionSyntax left,
        SyntaxToken operatorToken,
        ExpressionSyntax right)
        : base(
            ComputeSpan(left.Span, right.Span),
            ComputeSpan(left.FullSpan, right.FullSpan))
    {
        Left = left;
        OperatorToken = operatorToken;
        Right = right;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        yield return Left;
        yield return Right;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        yield return OperatorToken;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitBinaryExpression(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitBinaryExpression(this);

    private static TextSpan ComputeSpan(TextSpan first, TextSpan last)
    {
        return new TextSpan(first.Start, last.End - first.Start);
    }
}

/// <summary>
/// A unary (prefix) expression such as <c>-x</c>, <c>!flag</c>, or <c>~mask</c>.
/// </summary>
public sealed class UnaryExpressionSyntax : ExpressionSyntax
{
    /// <summary>
    /// The operator token.
    /// </summary>
    public SyntaxToken OperatorToken { get; }

    /// <summary>
    /// The operand expression.
    /// </summary>
    public ExpressionSyntax Operand { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.UnaryExpression;

    public UnaryExpressionSyntax(
        SyntaxToken operatorToken,
        ExpressionSyntax operand)
        : base(
            ComputeSpan(operatorToken.Span, operand.Span),
            ComputeSpan(operatorToken.FullSpan, operand.FullSpan))
    {
        OperatorToken = operatorToken;
        Operand = operand;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        yield return Operand;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        yield return OperatorToken;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitUnaryExpression(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitUnaryExpression(this);

    private static TextSpan ComputeSpan(TextSpan first, TextSpan last)
    {
        return new TextSpan(first.Start, last.End - first.Start);
    }
}

/// <summary>
/// A conditional (ternary) expression such as <c>x &gt; 0 ? x : -x</c>.
/// </summary>
public sealed class ConditionalExpressionSyntax : ExpressionSyntax
{
    /// <summary>
    /// The condition expression.
    /// </summary>
    public ExpressionSyntax Condition { get; }

    /// <summary>
    /// The <c>?</c> token.
    /// </summary>
    public SyntaxToken QuestionToken { get; }

    /// <summary>
    /// The expression evaluated when the condition is true.
    /// </summary>
    public ExpressionSyntax WhenTrue { get; }

    /// <summary>
    /// The <c>:</c> token.
    /// </summary>
    public SyntaxToken ColonToken { get; }

    /// <summary>
    /// The expression evaluated when the condition is false.
    /// </summary>
    public ExpressionSyntax WhenFalse { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.ConditionalExpression;

    public ConditionalExpressionSyntax(
        ExpressionSyntax condition,
        SyntaxToken questionToken,
        ExpressionSyntax whenTrue,
        SyntaxToken colonToken,
        ExpressionSyntax whenFalse)
        : base(
            ComputeSpan(condition.Span, whenFalse.Span),
            ComputeSpan(condition.FullSpan, whenFalse.FullSpan))
    {
        Condition = condition;
        QuestionToken = questionToken;
        WhenTrue = whenTrue;
        ColonToken = colonToken;
        WhenFalse = whenFalse;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        yield return Condition;
        yield return WhenTrue;
        yield return WhenFalse;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        yield return QuestionToken;
        yield return ColonToken;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitConditionalExpression(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitConditionalExpression(this);

    private static TextSpan ComputeSpan(TextSpan first, TextSpan last)
    {
        return new TextSpan(first.Start, last.End - first.Start);
    }
}

/// <summary>
/// A cast expression such as <c>(int)value</c>.
/// </summary>
public sealed class CastExpressionSyntax : ExpressionSyntax
{
    /// <summary>
    /// The <c>(</c> token.
    /// </summary>
    public SyntaxToken OpenParenToken { get; }

    /// <summary>
    /// The target type.
    /// </summary>
    public TypeSyntax Type { get; }

    /// <summary>
    /// The <c>)</c> token.
    /// </summary>
    public SyntaxToken CloseParenToken { get; }

    /// <summary>
    /// The expression being cast.
    /// </summary>
    public ExpressionSyntax Expression { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.CastExpression;

    public CastExpressionSyntax(
        SyntaxToken openParenToken,
        TypeSyntax type,
        SyntaxToken closeParenToken,
        ExpressionSyntax expression)
        : base(
            ComputeSpan(openParenToken.Span, expression.Span),
            ComputeSpan(openParenToken.FullSpan, expression.FullSpan))
    {
        OpenParenToken = openParenToken;
        Type = type;
        CloseParenToken = closeParenToken;
        Expression = expression;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        yield return Type;
        yield return Expression;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        yield return OpenParenToken;
        yield return CloseParenToken;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitCastExpression(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitCastExpression(this);

    private static TextSpan ComputeSpan(TextSpan first, TextSpan last)
    {
        return new TextSpan(first.Start, last.End - first.Start);
    }
}

/// <summary>
/// An array/index access expression such as <c>items[i]</c>.
/// </summary>
public sealed class ArrayAccessExpressionSyntax : ExpressionSyntax
{
    /// <summary>
    /// The expression being indexed.
    /// </summary>
    public ExpressionSyntax Expression { get; }

    /// <summary>
    /// The <c>[</c> token.
    /// </summary>
    public SyntaxToken OpenBracketToken { get; }

    /// <summary>
    /// The index expression.
    /// </summary>
    public ExpressionSyntax Index { get; }

    /// <summary>
    /// The <c>]</c> token.
    /// </summary>
    public SyntaxToken CloseBracketToken { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.ArrayAccessExpression;

    public ArrayAccessExpressionSyntax(
        ExpressionSyntax expression,
        SyntaxToken openBracketToken,
        ExpressionSyntax index,
        SyntaxToken closeBracketToken)
        : base(
            ComputeSpan(expression.Span, closeBracketToken.Span),
            ComputeSpan(expression.FullSpan, closeBracketToken.FullSpan))
    {
        Expression = expression;
        OpenBracketToken = openBracketToken;
        Index = index;
        CloseBracketToken = closeBracketToken;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        yield return Expression;
        yield return Index;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        yield return OpenBracketToken;
        yield return CloseBracketToken;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitArrayAccessExpression(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitArrayAccessExpression(this);

    private static TextSpan ComputeSpan(TextSpan first, TextSpan last)
    {
        return new TextSpan(first.Start, last.End - first.Start);
    }
}

/// <summary>
/// A parenthesized expression such as <c>(a + b)</c>.
/// </summary>
public sealed class ParenthesizedExpressionSyntax : ExpressionSyntax
{
    /// <summary>
    /// The <c>(</c> token.
    /// </summary>
    public SyntaxToken OpenParenToken { get; }

    /// <summary>
    /// The inner expression.
    /// </summary>
    public ExpressionSyntax Expression { get; }

    /// <summary>
    /// The <c>)</c> token.
    /// </summary>
    public SyntaxToken CloseParenToken { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.ParenthesizedExpression;

    public ParenthesizedExpressionSyntax(
        SyntaxToken openParenToken,
        ExpressionSyntax expression,
        SyntaxToken closeParenToken)
        : base(
            ComputeSpan(openParenToken.Span, closeParenToken.Span),
            ComputeSpan(openParenToken.FullSpan, closeParenToken.FullSpan))
    {
        OpenParenToken = openParenToken;
        Expression = expression;
        CloseParenToken = closeParenToken;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        yield return Expression;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        yield return OpenParenToken;
        yield return CloseParenToken;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitParenthesizedExpression(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitParenthesizedExpression(this);

    private static TextSpan ComputeSpan(TextSpan first, TextSpan last)
    {
        return new TextSpan(first.Start, last.End - first.Start);
    }
}

/// <summary>
/// An argument in a method call or invocation, optionally with a named parameter.
/// </summary>
public sealed class ArgumentSyntax : SyntaxNode
{
    /// <summary>
    /// Optional named parameter colon token (e.g., <c>name:</c>).
    /// </summary>
    public SyntaxToken? NameColon { get; }

    /// <summary>
    /// The argument expression.
    /// </summary>
    public ExpressionSyntax Expression { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.Argument;

    public ArgumentSyntax(SyntaxToken? nameColon, ExpressionSyntax expression)
        : base(
            nameColon.HasValue
                ? ComputeSpan(nameColon.Value.Span, expression.Span)
                : expression.Span,
            nameColon.HasValue
                ? ComputeSpan(nameColon.Value.FullSpan, expression.FullSpan)
                : expression.FullSpan)
    {
        NameColon = nameColon;
        Expression = expression;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        yield return Expression;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        if (NameColon.HasValue)
            yield return NameColon.Value;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitArgument(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitArgument(this);

    private static TextSpan ComputeSpan(TextSpan first, TextSpan last)
    {
        return new TextSpan(first.Start, last.End - first.Start);
    }
}
