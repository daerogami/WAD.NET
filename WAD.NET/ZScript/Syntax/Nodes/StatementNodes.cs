using System.Collections.Generic;
using System.Collections.Immutable;

namespace WAD.NET.ZScript.Syntax;

/// <summary>
/// Abstract base class for all statement syntax nodes.
/// </summary>
public abstract class StatementSyntax : SyntaxNode
{
    protected StatementSyntax(TextSpan span, TextSpan fullSpan)
        : base(span, fullSpan)
    {
    }
}

/// <summary>
/// A block statement delimited by braces, containing zero or more statements.
/// </summary>
public sealed class BlockStatementSyntax : StatementSyntax
{
    /// <summary>
    /// The <c>{</c> token.
    /// </summary>
    public SyntaxToken OpenBraceToken { get; }

    /// <summary>
    /// The statements within the block.
    /// </summary>
    public ImmutableArray<StatementSyntax> Statements { get; }

    /// <summary>
    /// The <c>}</c> token.
    /// </summary>
    public SyntaxToken CloseBraceToken { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.BlockStatement;

    public BlockStatementSyntax(
        SyntaxToken openBraceToken,
        ImmutableArray<StatementSyntax> statements,
        SyntaxToken closeBraceToken)
        : base(
            ComputeSpan(openBraceToken.Span, closeBraceToken.Span),
            ComputeSpan(openBraceToken.FullSpan, closeBraceToken.FullSpan))
    {
        OpenBraceToken = openBraceToken;
        Statements = statements;
        CloseBraceToken = closeBraceToken;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        foreach (var stmt in Statements)
            yield return stmt;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        yield return OpenBraceToken;
        yield return CloseBraceToken;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitBlockStatement(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitBlockStatement(this);

    private static TextSpan ComputeSpan(TextSpan first, TextSpan last)
    {
        return new TextSpan(first.Start, last.End - first.Start);
    }
}

/// <summary>
/// An expression statement (an expression followed by a semicolon).
/// </summary>
public sealed class ExpressionStatementSyntax : StatementSyntax
{
    /// <summary>
    /// The expression.
    /// </summary>
    public ExpressionSyntax Expression { get; }

    /// <summary>
    /// The <c>;</c> token.
    /// </summary>
    public SyntaxToken SemicolonToken { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.ExpressionStatement;

    public ExpressionStatementSyntax(ExpressionSyntax expression, SyntaxToken semicolonToken)
        : base(
            ComputeSpan(expression.Span, semicolonToken.Span),
            ComputeSpan(expression.FullSpan, semicolonToken.FullSpan))
    {
        Expression = expression;
        SemicolonToken = semicolonToken;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        yield return Expression;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        yield return SemicolonToken;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitExpressionStatement(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitExpressionStatement(this);

    private static TextSpan ComputeSpan(TextSpan first, TextSpan last)
    {
        return new TextSpan(first.Start, last.End - first.Start);
    }
}

/// <summary>
/// An if statement, optionally with an else clause.
/// </summary>
public sealed class IfStatementSyntax : StatementSyntax
{
    /// <summary>
    /// The <c>if</c> keyword.
    /// </summary>
    public SyntaxToken IfKeyword { get; }

    /// <summary>
    /// The <c>(</c> token.
    /// </summary>
    public SyntaxToken OpenParenToken { get; }

    /// <summary>
    /// The condition expression.
    /// </summary>
    public ExpressionSyntax Condition { get; }

    /// <summary>
    /// The <c>)</c> token.
    /// </summary>
    public SyntaxToken CloseParenToken { get; }

    /// <summary>
    /// The statement executed when the condition is true.
    /// </summary>
    public StatementSyntax Statement { get; }

    /// <summary>
    /// The optional <c>else</c> keyword.
    /// </summary>
    public SyntaxToken? ElseKeyword { get; }

    /// <summary>
    /// The optional statement executed when the condition is false.
    /// </summary>
    public StatementSyntax? ElseStatement { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.IfStatement;

    public IfStatementSyntax(
        SyntaxToken ifKeyword,
        SyntaxToken openParenToken,
        ExpressionSyntax condition,
        SyntaxToken closeParenToken,
        StatementSyntax statement,
        SyntaxToken? elseKeyword = null,
        StatementSyntax? elseStatement = null)
        : base(
            ComputeSpan(ifKeyword.Span, (elseStatement?.Span ?? statement.Span)),
            ComputeSpan(ifKeyword.FullSpan, (elseStatement?.FullSpan ?? statement.FullSpan)))
    {
        IfKeyword = ifKeyword;
        OpenParenToken = openParenToken;
        Condition = condition;
        CloseParenToken = closeParenToken;
        Statement = statement;
        ElseKeyword = elseKeyword;
        ElseStatement = elseStatement;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        yield return Condition;
        yield return Statement;
        if (ElseStatement != null)
            yield return ElseStatement;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        yield return IfKeyword;
        yield return OpenParenToken;
        yield return CloseParenToken;
        if (ElseKeyword.HasValue)
            yield return ElseKeyword.Value;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitIfStatement(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitIfStatement(this);

    private static TextSpan ComputeSpan(TextSpan first, TextSpan last)
    {
        return new TextSpan(first.Start, last.End - first.Start);
    }
}

/// <summary>
/// A while loop statement.
/// </summary>
public sealed class WhileStatementSyntax : StatementSyntax
{
    /// <summary>
    /// The <c>while</c> keyword.
    /// </summary>
    public SyntaxToken WhileKeyword { get; }

    /// <summary>
    /// The <c>(</c> token.
    /// </summary>
    public SyntaxToken OpenParenToken { get; }

    /// <summary>
    /// The loop condition expression.
    /// </summary>
    public ExpressionSyntax Condition { get; }

    /// <summary>
    /// The <c>)</c> token.
    /// </summary>
    public SyntaxToken CloseParenToken { get; }

    /// <summary>
    /// The loop body statement.
    /// </summary>
    public StatementSyntax Body { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.WhileStatement;

    public WhileStatementSyntax(
        SyntaxToken whileKeyword,
        SyntaxToken openParenToken,
        ExpressionSyntax condition,
        SyntaxToken closeParenToken,
        StatementSyntax body)
        : base(
            ComputeSpan(whileKeyword.Span, body.Span),
            ComputeSpan(whileKeyword.FullSpan, body.FullSpan))
    {
        WhileKeyword = whileKeyword;
        OpenParenToken = openParenToken;
        Condition = condition;
        CloseParenToken = closeParenToken;
        Body = body;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        yield return Condition;
        yield return Body;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        yield return WhileKeyword;
        yield return OpenParenToken;
        yield return CloseParenToken;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitWhileStatement(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitWhileStatement(this);

    private static TextSpan ComputeSpan(TextSpan first, TextSpan last)
    {
        return new TextSpan(first.Start, last.End - first.Start);
    }
}

/// <summary>
/// A do-while loop statement.
/// </summary>
public sealed class DoWhileStatementSyntax : StatementSyntax
{
    /// <summary>
    /// The <c>do</c> keyword.
    /// </summary>
    public SyntaxToken DoKeyword { get; }

    /// <summary>
    /// The loop body statement.
    /// </summary>
    public StatementSyntax Body { get; }

    /// <summary>
    /// The <c>while</c> keyword.
    /// </summary>
    public SyntaxToken WhileKeyword { get; }

    /// <summary>
    /// The <c>(</c> token.
    /// </summary>
    public SyntaxToken OpenParenToken { get; }

    /// <summary>
    /// The loop condition expression.
    /// </summary>
    public ExpressionSyntax Condition { get; }

    /// <summary>
    /// The <c>)</c> token.
    /// </summary>
    public SyntaxToken CloseParenToken { get; }

    /// <summary>
    /// The <c>;</c> token.
    /// </summary>
    public SyntaxToken SemicolonToken { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.DoWhileStatement;

    public DoWhileStatementSyntax(
        SyntaxToken doKeyword,
        StatementSyntax body,
        SyntaxToken whileKeyword,
        SyntaxToken openParenToken,
        ExpressionSyntax condition,
        SyntaxToken closeParenToken,
        SyntaxToken semicolonToken)
        : base(
            ComputeSpan(doKeyword.Span, semicolonToken.Span),
            ComputeSpan(doKeyword.FullSpan, semicolonToken.FullSpan))
    {
        DoKeyword = doKeyword;
        Body = body;
        WhileKeyword = whileKeyword;
        OpenParenToken = openParenToken;
        Condition = condition;
        CloseParenToken = closeParenToken;
        SemicolonToken = semicolonToken;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        yield return Body;
        yield return Condition;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        yield return DoKeyword;
        yield return WhileKeyword;
        yield return OpenParenToken;
        yield return CloseParenToken;
        yield return SemicolonToken;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitDoWhileStatement(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitDoWhileStatement(this);

    private static TextSpan ComputeSpan(TextSpan first, TextSpan last)
    {
        return new TextSpan(first.Start, last.End - first.Start);
    }
}

/// <summary>
/// A for loop statement.
/// </summary>
public sealed class ForStatementSyntax : StatementSyntax
{
    /// <summary>
    /// The <c>for</c> keyword.
    /// </summary>
    public SyntaxToken ForKeyword { get; }

    /// <summary>
    /// The <c>(</c> token.
    /// </summary>
    public SyntaxToken OpenParenToken { get; }

    /// <summary>
    /// The optional initializer statement.
    /// </summary>
    public StatementSyntax? Initializer { get; }

    /// <summary>
    /// The optional loop condition expression.
    /// </summary>
    public ExpressionSyntax? Condition { get; }

    /// <summary>
    /// The <c>;</c> token after the condition.
    /// </summary>
    public SyntaxToken SemicolonToken { get; }

    /// <summary>
    /// The optional incrementor expression.
    /// </summary>
    public ExpressionSyntax? Incrementor { get; }

    /// <summary>
    /// The <c>)</c> token.
    /// </summary>
    public SyntaxToken CloseParenToken { get; }

    /// <summary>
    /// The loop body statement.
    /// </summary>
    public StatementSyntax Body { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.ForStatement;

    public ForStatementSyntax(
        SyntaxToken forKeyword,
        SyntaxToken openParenToken,
        StatementSyntax? initializer,
        ExpressionSyntax? condition,
        SyntaxToken semicolonToken,
        ExpressionSyntax? incrementor,
        SyntaxToken closeParenToken,
        StatementSyntax body)
        : base(
            ComputeSpan(forKeyword.Span, body.Span),
            ComputeSpan(forKeyword.FullSpan, body.FullSpan))
    {
        ForKeyword = forKeyword;
        OpenParenToken = openParenToken;
        Initializer = initializer;
        Condition = condition;
        SemicolonToken = semicolonToken;
        Incrementor = incrementor;
        CloseParenToken = closeParenToken;
        Body = body;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        if (Initializer != null) yield return Initializer;
        if (Condition != null) yield return Condition;
        if (Incrementor != null) yield return Incrementor;
        yield return Body;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        yield return ForKeyword;
        yield return OpenParenToken;
        yield return SemicolonToken;
        yield return CloseParenToken;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitForStatement(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitForStatement(this);

    private static TextSpan ComputeSpan(TextSpan first, TextSpan last)
    {
        return new TextSpan(first.Start, last.End - first.Start);
    }
}

/// <summary>
/// A foreach loop statement.
/// </summary>
public sealed class ForEachStatementSyntax : StatementSyntax
{
    /// <summary>
    /// The <c>foreach</c> keyword.
    /// </summary>
    public SyntaxToken ForEachKeyword { get; }

    /// <summary>
    /// The <c>(</c> token.
    /// </summary>
    public SyntaxToken OpenParenToken { get; }

    /// <summary>
    /// The iteration variable type.
    /// </summary>
    public TypeSyntax Type { get; }

    /// <summary>
    /// The iteration variable identifier.
    /// </summary>
    public SyntaxToken Identifier { get; }

    /// <summary>
    /// The <c>in</c> keyword.
    /// </summary>
    public SyntaxToken InKeyword { get; }

    /// <summary>
    /// The collection expression being iterated.
    /// </summary>
    public ExpressionSyntax Expression { get; }

    /// <summary>
    /// The <c>)</c> token.
    /// </summary>
    public SyntaxToken CloseParenToken { get; }

    /// <summary>
    /// The loop body statement.
    /// </summary>
    public StatementSyntax Body { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.ForEachStatement;

    public ForEachStatementSyntax(
        SyntaxToken forEachKeyword,
        SyntaxToken openParenToken,
        TypeSyntax type,
        SyntaxToken identifier,
        SyntaxToken inKeyword,
        ExpressionSyntax expression,
        SyntaxToken closeParenToken,
        StatementSyntax body)
        : base(
            ComputeSpan(forEachKeyword.Span, body.Span),
            ComputeSpan(forEachKeyword.FullSpan, body.FullSpan))
    {
        ForEachKeyword = forEachKeyword;
        OpenParenToken = openParenToken;
        Type = type;
        Identifier = identifier;
        InKeyword = inKeyword;
        Expression = expression;
        CloseParenToken = closeParenToken;
        Body = body;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        yield return Type;
        yield return Expression;
        yield return Body;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        yield return ForEachKeyword;
        yield return OpenParenToken;
        yield return Identifier;
        yield return InKeyword;
        yield return CloseParenToken;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitForEachStatement(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitForEachStatement(this);

    private static TextSpan ComputeSpan(TextSpan first, TextSpan last)
    {
        return new TextSpan(first.Start, last.End - first.Start);
    }
}

/// <summary>
/// A switch statement.
/// </summary>
public sealed class SwitchStatementSyntax : StatementSyntax
{
    /// <summary>
    /// The <c>switch</c> keyword.
    /// </summary>
    public SyntaxToken SwitchKeyword { get; }

    /// <summary>
    /// The <c>(</c> token.
    /// </summary>
    public SyntaxToken OpenParenToken { get; }

    /// <summary>
    /// The switch expression.
    /// </summary>
    public ExpressionSyntax Expression { get; }

    /// <summary>
    /// The <c>)</c> token.
    /// </summary>
    public SyntaxToken CloseParenToken { get; }

    /// <summary>
    /// The <c>{</c> token.
    /// </summary>
    public SyntaxToken OpenBraceToken { get; }

    /// <summary>
    /// The sections of the switch body (case labels, default labels, and statements).
    /// </summary>
    public ImmutableArray<StatementSyntax> Sections { get; }

    /// <summary>
    /// The <c>}</c> token.
    /// </summary>
    public SyntaxToken CloseBraceToken { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.SwitchStatement;

    public SwitchStatementSyntax(
        SyntaxToken switchKeyword,
        SyntaxToken openParenToken,
        ExpressionSyntax expression,
        SyntaxToken closeParenToken,
        SyntaxToken openBraceToken,
        ImmutableArray<StatementSyntax> sections,
        SyntaxToken closeBraceToken)
        : base(
            ComputeSpan(switchKeyword.Span, closeBraceToken.Span),
            ComputeSpan(switchKeyword.FullSpan, closeBraceToken.FullSpan))
    {
        SwitchKeyword = switchKeyword;
        OpenParenToken = openParenToken;
        Expression = expression;
        CloseParenToken = closeParenToken;
        OpenBraceToken = openBraceToken;
        Sections = sections;
        CloseBraceToken = closeBraceToken;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        yield return Expression;
        foreach (var section in Sections)
            yield return section;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        yield return SwitchKeyword;
        yield return OpenParenToken;
        yield return CloseParenToken;
        yield return OpenBraceToken;
        yield return CloseBraceToken;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitSwitchStatement(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitSwitchStatement(this);

    private static TextSpan ComputeSpan(TextSpan first, TextSpan last)
    {
        return new TextSpan(first.Start, last.End - first.Start);
    }
}

/// <summary>
/// A case label in a switch statement, such as <c>case 1:</c>.
/// </summary>
public sealed class CaseLabelSyntax : StatementSyntax
{
    /// <summary>
    /// The <c>case</c> keyword.
    /// </summary>
    public SyntaxToken CaseKeyword { get; }

    /// <summary>
    /// The case value expression.
    /// </summary>
    public ExpressionSyntax Value { get; }

    /// <summary>
    /// The <c>:</c> token.
    /// </summary>
    public SyntaxToken ColonToken { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.CaseLabel;

    public CaseLabelSyntax(
        SyntaxToken caseKeyword,
        ExpressionSyntax value,
        SyntaxToken colonToken)
        : base(
            ComputeSpan(caseKeyword.Span, colonToken.Span),
            ComputeSpan(caseKeyword.FullSpan, colonToken.FullSpan))
    {
        CaseKeyword = caseKeyword;
        Value = value;
        ColonToken = colonToken;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        yield return Value;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        yield return CaseKeyword;
        yield return ColonToken;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitCaseLabel(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitCaseLabel(this);

    private static TextSpan ComputeSpan(TextSpan first, TextSpan last)
    {
        return new TextSpan(first.Start, last.End - first.Start);
    }
}

/// <summary>
/// A default label in a switch statement, such as <c>default:</c>.
/// </summary>
public sealed class DefaultLabelSyntax : StatementSyntax
{
    /// <summary>
    /// The <c>default</c> keyword.
    /// </summary>
    public SyntaxToken DefaultKeyword { get; }

    /// <summary>
    /// The <c>:</c> token.
    /// </summary>
    public SyntaxToken ColonToken { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.DefaultLabel;

    public DefaultLabelSyntax(SyntaxToken defaultKeyword, SyntaxToken colonToken)
        : base(
            ComputeSpan(defaultKeyword.Span, colonToken.Span),
            ComputeSpan(defaultKeyword.FullSpan, colonToken.FullSpan))
    {
        DefaultKeyword = defaultKeyword;
        ColonToken = colonToken;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        yield break;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        yield return DefaultKeyword;
        yield return ColonToken;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitDefaultLabel(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitDefaultLabel(this);

    private static TextSpan ComputeSpan(TextSpan first, TextSpan last)
    {
        return new TextSpan(first.Start, last.End - first.Start);
    }
}

/// <summary>
/// A return statement, optionally with a return value expression.
/// </summary>
public sealed class ReturnStatementSyntax : StatementSyntax
{
    /// <summary>
    /// The <c>return</c> keyword.
    /// </summary>
    public SyntaxToken ReturnKeyword { get; }

    /// <summary>
    /// The optional return value expression.
    /// </summary>
    public ExpressionSyntax? Expression { get; }

    /// <summary>
    /// The <c>;</c> token.
    /// </summary>
    public SyntaxToken SemicolonToken { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.ReturnStatement;

    public ReturnStatementSyntax(
        SyntaxToken returnKeyword,
        ExpressionSyntax? expression,
        SyntaxToken semicolonToken)
        : base(
            ComputeSpan(returnKeyword.Span, semicolonToken.Span),
            ComputeSpan(returnKeyword.FullSpan, semicolonToken.FullSpan))
    {
        ReturnKeyword = returnKeyword;
        Expression = expression;
        SemicolonToken = semicolonToken;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        if (Expression != null)
            yield return Expression;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        yield return ReturnKeyword;
        yield return SemicolonToken;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitReturnStatement(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitReturnStatement(this);

    private static TextSpan ComputeSpan(TextSpan first, TextSpan last)
    {
        return new TextSpan(first.Start, last.End - first.Start);
    }
}

/// <summary>
/// A break statement.
/// </summary>
public sealed class BreakStatementSyntax : StatementSyntax
{
    /// <summary>
    /// The <c>break</c> keyword.
    /// </summary>
    public SyntaxToken BreakKeyword { get; }

    /// <summary>
    /// The <c>;</c> token.
    /// </summary>
    public SyntaxToken SemicolonToken { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.BreakStatement;

    public BreakStatementSyntax(SyntaxToken breakKeyword, SyntaxToken semicolonToken)
        : base(
            ComputeSpan(breakKeyword.Span, semicolonToken.Span),
            ComputeSpan(breakKeyword.FullSpan, semicolonToken.FullSpan))
    {
        BreakKeyword = breakKeyword;
        SemicolonToken = semicolonToken;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        yield break;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        yield return BreakKeyword;
        yield return SemicolonToken;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitBreakStatement(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitBreakStatement(this);

    private static TextSpan ComputeSpan(TextSpan first, TextSpan last)
    {
        return new TextSpan(first.Start, last.End - first.Start);
    }
}

/// <summary>
/// A continue statement.
/// </summary>
public sealed class ContinueStatementSyntax : StatementSyntax
{
    /// <summary>
    /// The <c>continue</c> keyword.
    /// </summary>
    public SyntaxToken ContinueKeyword { get; }

    /// <summary>
    /// The <c>;</c> token.
    /// </summary>
    public SyntaxToken SemicolonToken { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.ContinueStatement;

    public ContinueStatementSyntax(SyntaxToken continueKeyword, SyntaxToken semicolonToken)
        : base(
            ComputeSpan(continueKeyword.Span, semicolonToken.Span),
            ComputeSpan(continueKeyword.FullSpan, semicolonToken.FullSpan))
    {
        ContinueKeyword = continueKeyword;
        SemicolonToken = semicolonToken;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        yield break;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        yield return ContinueKeyword;
        yield return SemicolonToken;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitContinueStatement(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitContinueStatement(this);

    private static TextSpan ComputeSpan(TextSpan first, TextSpan last)
    {
        return new TextSpan(first.Start, last.End - first.Start);
    }
}

/// <summary>
/// A local variable declaration statement, optionally with <c>let</c> for type inference.
/// </summary>
public sealed class LocalDeclarationStatementSyntax : StatementSyntax
{
    /// <summary>
    /// The optional <c>let</c> keyword for type inference.
    /// </summary>
    public SyntaxToken? LetKeyword { get; }

    /// <summary>
    /// The optional explicit type (null when <c>let</c> is used for inference).
    /// </summary>
    public TypeSyntax? Type { get; }

    /// <summary>
    /// The comma-separated list of variable declarators.
    /// </summary>
    public SeparatedSyntaxList<VariableDeclaratorSyntax> Variables { get; }

    /// <summary>
    /// The <c>;</c> token.
    /// </summary>
    public SyntaxToken SemicolonToken { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.LocalDeclarationStatement;

    public LocalDeclarationStatementSyntax(
        SyntaxToken? letKeyword,
        TypeSyntax? type,
        SeparatedSyntaxList<VariableDeclaratorSyntax> variables,
        SyntaxToken semicolonToken)
        : base(
            ComputeSpanForLocalDecl(letKeyword, type, semicolonToken),
            ComputeFullSpanForLocalDecl(letKeyword, type, semicolonToken))
    {
        LetKeyword = letKeyword;
        Type = type;
        Variables = variables;
        SemicolonToken = semicolonToken;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        if (Type != null)
            yield return Type;
        foreach (var variable in Variables)
            yield return variable;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        if (LetKeyword.HasValue)
            yield return LetKeyword.Value;
        yield return SemicolonToken;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitLocalDeclarationStatement(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitLocalDeclarationStatement(this);

    private static TextSpan ComputeSpanForLocalDecl(SyntaxToken? letKeyword, TypeSyntax? type, SyntaxToken semicolonToken)
    {
        TextSpan first;
        if (letKeyword.HasValue) first = letKeyword.Value.Span;
        else if (type != null) first = type.Span;
        else first = semicolonToken.Span;
        return ComputeSpan(first, semicolonToken.Span);
    }

    private static TextSpan ComputeFullSpanForLocalDecl(SyntaxToken? letKeyword, TypeSyntax? type, SyntaxToken semicolonToken)
    {
        TextSpan first;
        if (letKeyword.HasValue) first = letKeyword.Value.FullSpan;
        else if (type != null) first = type.FullSpan;
        else first = semicolonToken.FullSpan;
        return ComputeSpan(first, semicolonToken.FullSpan);
    }

    private static TextSpan ComputeSpan(TextSpan first, TextSpan last)
    {
        return new TextSpan(first.Start, last.End - first.Start);
    }
}

/// <summary>
/// An assignment statement such as <c>x = 5;</c> or <c>health += 10;</c>.
/// </summary>
public sealed class AssignmentStatementSyntax : StatementSyntax
{
    /// <summary>
    /// The target expression being assigned to.
    /// </summary>
    public ExpressionSyntax Target { get; }

    /// <summary>
    /// The assignment operator token (e.g., <c>=</c>, <c>+=</c>, <c>-=</c>).
    /// </summary>
    public SyntaxToken OperatorToken { get; }

    /// <summary>
    /// The value expression being assigned.
    /// </summary>
    public ExpressionSyntax Value { get; }

    /// <summary>
    /// The <c>;</c> token.
    /// </summary>
    public SyntaxToken SemicolonToken { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.AssignmentStatement;

    public AssignmentStatementSyntax(
        ExpressionSyntax target,
        SyntaxToken operatorToken,
        ExpressionSyntax value,
        SyntaxToken semicolonToken)
        : base(
            ComputeSpan(target.Span, semicolonToken.Span),
            ComputeSpan(target.FullSpan, semicolonToken.FullSpan))
    {
        Target = target;
        OperatorToken = operatorToken;
        Value = value;
        SemicolonToken = semicolonToken;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        yield return Target;
        yield return Value;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        yield return OperatorToken;
        yield return SemicolonToken;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitAssignmentStatement(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitAssignmentStatement(this);

    private static TextSpan ComputeSpan(TextSpan first, TextSpan last)
    {
        return new TextSpan(first.Start, last.End - first.Start);
    }
}
