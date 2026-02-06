namespace WAD.NET.ZScript.Syntax;

/// <summary>
/// A discriminated union that holds either a <see cref="SyntaxNode"/> or a <see cref="SyntaxToken"/>.
/// </summary>
public readonly struct SyntaxNodeOrToken
{
    private readonly SyntaxNode? _node;
    private readonly SyntaxToken _token;

    /// <summary>
    /// Returns <see langword="true"/> if this instance wraps a <see cref="SyntaxNode"/>.
    /// </summary>
    public bool IsNode { get; }

    /// <summary>
    /// Returns <see langword="true"/> if this instance wraps a <see cref="SyntaxToken"/>.
    /// </summary>
    public bool IsToken => !IsNode;

    /// <summary>
    /// The span of the contained node or token.
    /// </summary>
    public TextSpan Span => IsNode ? _node!.Span : _token.Span;

    /// <summary>
    /// The full span (including trivia) of the contained node or token.
    /// </summary>
    public TextSpan FullSpan => IsNode ? _node!.FullSpan : _token.FullSpan;

    private SyntaxNodeOrToken(SyntaxNode node)
    {
        _node = node;
        _token = default;
        IsNode = true;
    }

    private SyntaxNodeOrToken(SyntaxToken token)
    {
        _node = null;
        _token = token;
        IsNode = false;
    }

    /// <summary>
    /// Returns the contained <see cref="SyntaxNode"/>, or <see langword="null"/> if this is a token.
    /// </summary>
    public SyntaxNode? AsNode() => IsNode ? _node : null;

    /// <summary>
    /// Returns the contained <see cref="SyntaxToken"/>.
    /// Only meaningful when <see cref="IsToken"/> is <see langword="true"/>.
    /// </summary>
    public SyntaxToken AsToken() => _token;

    /// <summary>
    /// Creates a <see cref="SyntaxNodeOrToken"/> wrapping the specified node.
    /// </summary>
    public static SyntaxNodeOrToken FromNode(SyntaxNode node) => new SyntaxNodeOrToken(node);

    /// <summary>
    /// Creates a <see cref="SyntaxNodeOrToken"/> wrapping the specified token.
    /// </summary>
    public static SyntaxNodeOrToken FromToken(SyntaxToken token) => new SyntaxNodeOrToken(token);
}
