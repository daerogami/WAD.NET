using System;

namespace WAD.NET.ZScript.Syntax;

/// <summary>
/// A syntax rewriter that renames actor declarations by applying a transformation function
/// to each actor name.
/// </summary>
public class ActorRenamer : SyntaxRewriter
{
    private readonly Func<string, string> _rename;

    /// <summary>
    /// Creates a new <see cref="ActorRenamer"/> with the specified renaming function.
    /// </summary>
    /// <param name="rename">A function that transforms an actor name into a new name.</param>
    public ActorRenamer(Func<string, string> rename)
    {
        _rename = rename ?? throw new ArgumentNullException(nameof(rename));
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitActorDeclaration(ActorDeclarationSyntax node)
    {
        var newName = _rename(node.Name);
        if (newName != node.Name)
        {
            var newIdentifier = new SyntaxToken(
                node.Identifier.Kind, newName,
                node.Identifier.Span, node.Identifier.LeadingTrivia,
                node.Identifier.TrailingTrivia, null, false);

            // Visit the body first via the base implementation to handle children,
            // then reconstruct with the new identifier.
            var visitedBody = VisitList(node.Body);

            return new ActorDeclarationSyntax(
                node.ActorKeyword, newIdentifier, node.ColonToken,
                node.BaseIdentifier, node.ReplacesKeyword, node.ReplacesIdentifier,
                node.EditorNumber, node.OpenBraceToken, visitedBody, node.CloseBraceToken);
        }
        return base.VisitActorDeclaration(node);
    }
}
