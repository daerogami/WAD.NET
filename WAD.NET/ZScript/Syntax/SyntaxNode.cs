using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WAD.NET.ZScript.Syntax;

/// <summary>
/// Base class for all syntax tree nodes in the ZScript/DECORATE parser.
/// </summary>
public abstract class SyntaxNode
{
    /// <summary>
    /// The kind of this syntax node.
    /// </summary>
    public abstract SyntaxNodeKind Kind { get; }

    /// <summary>
    /// The parent node, or <see langword="null"/> if this is the root.
    /// </summary>
    public SyntaxNode? Parent { get; internal set; }

    /// <summary>
    /// The span of this node (excluding trivia).
    /// </summary>
    public TextSpan Span { get; }

    /// <summary>
    /// The full span of this node (including trivia).
    /// </summary>
    public TextSpan FullSpan { get; }

    /// <summary>
    /// Creates a new <see cref="SyntaxNode"/> with the specified spans.
    /// </summary>
    protected SyntaxNode(TextSpan span, TextSpan fullSpan)
    {
        Span = span;
        FullSpan = fullSpan;
    }

    /// <summary>
    /// Returns the immediate child nodes of this node.
    /// </summary>
    public abstract IEnumerable<SyntaxNode> ChildNodes();

    /// <summary>
    /// Returns the immediate child tokens of this node.
    /// </summary>
    public abstract IEnumerable<SyntaxToken> ChildTokens();

    /// <summary>
    /// Returns all immediate child nodes and tokens, merged by source position.
    /// </summary>
    public IEnumerable<SyntaxNodeOrToken> ChildNodesAndTokens()
    {
        var nodes = ChildNodes().GetEnumerator();
        var tokens = ChildTokens().GetEnumerator();

        var hasNode = nodes.MoveNext();
        var hasToken = tokens.MoveNext();

        while (hasNode && hasToken)
        {
            if (nodes.Current.Span.Start <= tokens.Current.Span.Start)
            {
                yield return SyntaxNodeOrToken.FromNode(nodes.Current);
                hasNode = nodes.MoveNext();
            }
            else
            {
                yield return SyntaxNodeOrToken.FromToken(tokens.Current);
                hasToken = tokens.MoveNext();
            }
        }

        while (hasNode)
        {
            yield return SyntaxNodeOrToken.FromNode(nodes.Current);
            hasNode = nodes.MoveNext();
        }

        while (hasToken)
        {
            yield return SyntaxNodeOrToken.FromToken(tokens.Current);
            hasToken = tokens.MoveNext();
        }
    }

    /// <summary>
    /// Returns all descendant nodes recursively (depth-first).
    /// </summary>
    public IEnumerable<SyntaxNode> DescendantNodes()
    {
        foreach (var child in ChildNodes())
        {
            yield return child;
            foreach (var descendant in child.DescendantNodes())
            {
                yield return descendant;
            }
        }
    }

    /// <summary>
    /// Returns all descendant tokens in source order by walking the tree depth-first,
    /// visiting both child nodes and child tokens sorted by position.
    /// </summary>
    public IEnumerable<SyntaxToken> DescendantTokens()
    {
        foreach (var childOrToken in ChildNodesAndTokens())
        {
            if (childOrToken.IsToken)
            {
                yield return childOrToken.AsToken();
            }
            else
            {
                foreach (var token in childOrToken.AsNode()!.DescendantTokens())
                {
                    yield return token;
                }
            }
        }
    }

    /// <summary>
    /// Returns all trivia from all descendant tokens in source order.
    /// </summary>
    public IEnumerable<SyntaxTrivia> DescendantTrivia()
    {
        foreach (var token in DescendantTokens())
        {
            foreach (var trivia in token.LeadingTrivia)
            {
                yield return trivia;
            }

            foreach (var trivia in token.TrailingTrivia)
            {
                yield return trivia;
            }
        }
    }

    /// <summary>
    /// Walks up the ancestor chain looking for a node of type <typeparamref name="T"/>.
    /// Includes <see langword="this"/> node in the search.
    /// </summary>
    public T? FirstAncestorOrSelf<T>() where T : SyntaxNode
    {
        SyntaxNode? current = this;
        while (current != null)
        {
            if (current is T result)
                return result;
            current = current.Parent;
        }
        return null;
    }

    /// <summary>
    /// Dispatches to the appropriate Visit method on the visitor.
    /// </summary>
    public abstract void Accept(SyntaxVisitor visitor);

    /// <summary>
    /// Dispatches to the appropriate Visit method on the visitor and returns its result.
    /// </summary>
    public abstract TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor);

    /// <summary>
    /// Rebuilds the full source text including all trivia.
    /// </summary>
    public string ToFullString()
    {
        var sb = new StringBuilder();
        foreach (var token in DescendantTokens())
        {
            foreach (var trivia in token.LeadingTrivia)
            {
                sb.Append(trivia.Text);
            }

            sb.Append(token.Text);

            foreach (var trivia in token.TrailingTrivia)
            {
                sb.Append(trivia.Text);
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Rebuilds the source text from tokens, excluding trivia.
    /// </summary>
    public override string ToString()
    {
        var sb = new StringBuilder();
        foreach (var token in DescendantTokens())
        {
            sb.Append(token.Text);
        }
        return sb.ToString();
    }
}
