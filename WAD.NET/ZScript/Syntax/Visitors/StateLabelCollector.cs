using System.Collections.Generic;

namespace WAD.NET.ZScript.Syntax;

/// <summary>
/// A syntax walker that collects all state label names from a syntax tree.
/// </summary>
public class StateLabelCollector : SyntaxVisitor
{
    /// <summary>
    /// The collected state label names, in the order they were encountered.
    /// </summary>
    public List<string> Labels { get; } = new List<string>();

    /// <inheritdoc />
    public override void VisitStateLabel(StateLabelSyntax node)
    {
        Labels.Add(node.LabelName);
        base.VisitStateLabel(node);
    }
}
