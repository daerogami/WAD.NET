using WAD.NET.ZScript.Syntax;

namespace WAD.NET.ZScript.Formatting;

/// <summary>
/// Formats ZScript syntax trees according to the specified options.
/// </summary>
public sealed class SyntaxFormatter
{
    private readonly FormattingOptions _options;

    /// <summary>
    /// Creates a new <see cref="SyntaxFormatter"/> with the specified options.
    /// </summary>
    /// <param name="options">The formatting options, or <see langword="null"/> for defaults.</param>
    public SyntaxFormatter(FormattingOptions? options = null)
    {
        _options = options ?? FormattingOptions.Default;
    }

    /// <summary>
    /// Formats a syntax node and returns the formatted source text.
    /// </summary>
    /// <param name="node">The syntax node to format.</param>
    /// <returns>The formatted source text.</returns>
    public string Format(SyntaxNode node)
    {
        var rewriter = new FormattingRewriter(_options);
        var formatted = rewriter.Visit(node);
        return formatted?.ToFullString() ?? "";
    }
}
