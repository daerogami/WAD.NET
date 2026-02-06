using System.Collections.Generic;
using System.Collections.Immutable;
using WAD.NET.ZScript.Syntax;

namespace WAD.NET.ZScript.Formatting;

/// <summary>
/// A syntax rewriter that adjusts trivia on tokens to apply formatting rules
/// such as indentation normalization and brace placement.
/// </summary>
public class FormattingRewriter : SyntaxRewriter
{
    private readonly FormattingOptions _options;
    private int _indentLevel;

    /// <summary>
    /// Creates a new <see cref="FormattingRewriter"/> with the specified options.
    /// </summary>
    public FormattingRewriter(FormattingOptions options)
    {
        _options = options;
    }

    private string IndentString
    {
        get
        {
            if (_options.UseTabs)
                return new string('\t', _indentLevel);
            return new string(' ', _indentLevel * _options.IndentSize);
        }
    }

    private SyntaxTrivia NewLineTrivia =>
        new SyntaxTrivia(SyntaxTriviaKind.EndOfLine, "\n", default);

    private SyntaxTrivia MakeIndentTrivia()
    {
        var text = IndentString;
        return new SyntaxTrivia(SyntaxTriviaKind.Whitespace, text, default);
    }

    private SyntaxTrivia SpaceTrivia =>
        new SyntaxTrivia(SyntaxTriviaKind.Whitespace, " ", default);

    private SyntaxTriviaList MakeNewLineAndIndent()
    {
        var items = new List<SyntaxTrivia>();
        items.Add(NewLineTrivia);
        var indent = IndentString;
        if (indent.Length > 0)
            items.Add(MakeIndentTrivia());
        return new SyntaxTriviaList(items.ToImmutableArray());
    }

    private SyntaxTriviaList PreserveCommentsWithNewLineAndIndent(SyntaxTriviaList existing)
    {
        var items = new List<SyntaxTrivia>();
        foreach (var trivia in existing)
        {
            if (trivia.IsComment)
            {
                items.Add(NewLineTrivia);
                var indent = IndentString;
                if (indent.Length > 0)
                    items.Add(MakeIndentTrivia());
                items.Add(trivia);
            }
        }
        items.Add(NewLineTrivia);
        var finalIndent = IndentString;
        if (finalIndent.Length > 0)
            items.Add(MakeIndentTrivia());
        return new SyntaxTriviaList(items.ToImmutableArray());
    }

    /// <inheritdoc/>
    public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        var openBrace = node.OpenBraceToken;
        if (_options.NewLineBeforeOpenBrace)
        {
            openBrace = openBrace.WithLeadingTrivia(MakeNewLineAndIndent());
        }
        else
        {
            openBrace = openBrace.WithLeadingTrivia(
                new SyntaxTriviaList(ImmutableArray.Create(SpaceTrivia)));
        }

        _indentLevel++;
        var members = VisitList(node.Members);
        _indentLevel--;

        var closeBrace = node.CloseBraceToken.WithLeadingTrivia(MakeNewLineAndIndent());

        return new ClassDeclarationSyntax(
            node.Modifiers, node.ClassKeyword, node.Identifier,
            node.BaseList, node.ReplacesKeyword, node.ReplacesIdentifier,
            node.EditorNumber, openBrace, members, closeBrace);
    }

    /// <inheritdoc/>
    public override SyntaxNode? VisitActorDeclaration(ActorDeclarationSyntax node)
    {
        var openBrace = node.OpenBraceToken;
        if (_options.NewLineBeforeOpenBrace)
        {
            openBrace = openBrace.WithLeadingTrivia(MakeNewLineAndIndent());
        }
        else
        {
            openBrace = openBrace.WithLeadingTrivia(
                new SyntaxTriviaList(ImmutableArray.Create(SpaceTrivia)));
        }

        _indentLevel++;
        var body = VisitList(node.Body);
        _indentLevel--;

        var closeBrace = node.CloseBraceToken.WithLeadingTrivia(MakeNewLineAndIndent());

        return new ActorDeclarationSyntax(
            node.ActorKeyword, node.Identifier, node.ColonToken,
            node.BaseIdentifier, node.ReplacesKeyword, node.ReplacesIdentifier,
            node.EditorNumber, openBrace, body, closeBrace);
    }

    /// <inheritdoc/>
    public override SyntaxNode? VisitDefaultBlock(DefaultBlockSyntax node)
    {
        var defaultKeyword = node.DefaultKeyword.WithLeadingTrivia(
            PreserveCommentsWithNewLineAndIndent(node.DefaultKeyword.LeadingTrivia));

        var openBrace = node.OpenBraceToken;
        if (_options.NewLineBeforeOpenBrace)
        {
            openBrace = openBrace.WithLeadingTrivia(MakeNewLineAndIndent());
        }
        else
        {
            openBrace = openBrace.WithLeadingTrivia(
                new SyntaxTriviaList(ImmutableArray.Create(SpaceTrivia)));
        }

        _indentLevel++;
        var items = VisitList(node.Items);
        _indentLevel--;

        var closeBrace = node.CloseBraceToken.WithLeadingTrivia(MakeNewLineAndIndent());

        return new DefaultBlockSyntax(defaultKeyword, openBrace, items, closeBrace);
    }

    /// <inheritdoc/>
    public override SyntaxNode? VisitStatesBlock(StatesBlockSyntax node)
    {
        var statesKeyword = node.StatesKeyword.WithLeadingTrivia(
            PreserveCommentsWithNewLineAndIndent(node.StatesKeyword.LeadingTrivia));

        var openBrace = node.OpenBraceToken;
        if (_options.NewLineBeforeOpenBrace)
        {
            openBrace = openBrace.WithLeadingTrivia(MakeNewLineAndIndent());
        }
        else
        {
            openBrace = openBrace.WithLeadingTrivia(
                new SyntaxTriviaList(ImmutableArray.Create(SpaceTrivia)));
        }

        _indentLevel++;
        var states = VisitList(node.States);
        _indentLevel--;

        var closeBrace = node.CloseBraceToken.WithLeadingTrivia(MakeNewLineAndIndent());

        return new StatesBlockSyntax(
            statesKeyword, node.OpenParenToken, node.StateOptions,
            node.CloseParenToken, openBrace, states, closeBrace);
    }

    /// <inheritdoc/>
    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        var returnType = (TypeSyntax?)Visit(node.ReturnType) ?? node.ReturnType;
        var parameters = VisitSeparatedList(node.Parameters, out bool parametersChanged);
        var body = node.Body != null
            ? (BlockStatementSyntax?)Visit(node.Body)
            : null;

        if (returnType != node.ReturnType || parametersChanged || body != node.Body)
        {
            return new MethodDeclarationSyntax(
                node.Modifiers, returnType, node.Identifier,
                node.OpenParenToken, parameters, node.CloseParenToken,
                node.ConstKeyword, body, node.SemicolonToken);
        }
        return node;
    }

    /// <inheritdoc/>
    public override SyntaxNode? VisitBlockStatement(BlockStatementSyntax node)
    {
        var openBrace = node.OpenBraceToken;
        if (_options.NewLineBeforeOpenBrace)
        {
            openBrace = openBrace.WithLeadingTrivia(MakeNewLineAndIndent());
        }
        else
        {
            openBrace = openBrace.WithLeadingTrivia(
                new SyntaxTriviaList(ImmutableArray.Create(SpaceTrivia)));
        }

        _indentLevel++;
        var statements = VisitList(node.Statements);
        _indentLevel--;

        var closeBrace = node.CloseBraceToken.WithLeadingTrivia(MakeNewLineAndIndent());

        return new BlockStatementSyntax(openBrace, statements, closeBrace);
    }
}
