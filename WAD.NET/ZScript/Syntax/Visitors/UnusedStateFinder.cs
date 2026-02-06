using System;
using System.Collections.Generic;
using System.Linq;

namespace WAD.NET.ZScript.Syntax;

/// <summary>
/// A syntax walker that identifies state labels that are defined but never referenced
/// by a <c>Goto</c> directive within the same tree.
/// </summary>
public class UnusedStateFinder : SyntaxVisitor
{
    private readonly HashSet<string> _definedLabels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _referencedLabels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Returns the set of label names that were defined but never referenced by a goto.
    /// </summary>
    public IEnumerable<string> UnusedLabels => _definedLabels.Where(l => !_referencedLabels.Contains(l));

    /// <inheritdoc />
    public override void VisitStateLabel(StateLabelSyntax node)
    {
        _definedLabels.Add(node.LabelName);
        base.VisitStateLabel(node);
    }

    /// <inheritdoc />
    public override void VisitStateGoto(StateGotoSyntax node)
    {
        _referencedLabels.Add(node.TargetLabel);
        base.VisitStateGoto(node);
    }
}
