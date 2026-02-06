using System;
using System.Collections.Immutable;

namespace WAD.NET.ZScript.Syntax;

/// <summary>
/// Represents a single token in the ZScript/DECORATE source, including its trivia.
/// </summary>
public readonly struct SyntaxToken : IEquatable<SyntaxToken>
{
    /// <summary>
    /// The kind of this token.
    /// </summary>
    public SyntaxTokenKind Kind { get; }

    /// <summary>
    /// The text of this token as it appears in source.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// The span of the token text itself (excluding trivia).
    /// </summary>
    public TextSpan Span { get; }

    /// <summary>
    /// The full span including leading and trailing trivia.
    /// </summary>
    public TextSpan FullSpan { get; }

    /// <summary>
    /// Trivia appearing before this token.
    /// </summary>
    public SyntaxTriviaList LeadingTrivia { get; }

    /// <summary>
    /// Trivia appearing after this token.
    /// </summary>
    public SyntaxTriviaList TrailingTrivia { get; }

    /// <summary>
    /// The semantic value of this token (e.g., the parsed integer for an IntegerLiteral).
    /// </summary>
    public object? Value { get; }

    /// <summary>
    /// Indicates whether this token was inserted by the parser and is not present in source.
    /// </summary>
    public bool IsMissing { get; }

    /// <summary>
    /// Creates a new <see cref="SyntaxToken"/> with the specified properties.
    /// The <see cref="FullSpan"/> is computed from leading trivia, the token span, and trailing trivia.
    /// </summary>
    public SyntaxToken(
        SyntaxTokenKind kind,
        string text,
        TextSpan span,
        SyntaxTriviaList leadingTrivia = default,
        SyntaxTriviaList trailingTrivia = default,
        object? value = null,
        bool isMissing = false)
    {
        Kind = kind;
        Text = text;
        Span = span;
        LeadingTrivia = leadingTrivia.Count > 0 ? leadingTrivia : SyntaxTriviaList.Empty;
        TrailingTrivia = trailingTrivia.Count > 0 ? trailingTrivia : SyntaxTriviaList.Empty;
        Value = value;
        IsMissing = isMissing;

        // Compute FullSpan: from the start of leading trivia (or token) to the end of trailing trivia (or token)
        var fullStart = LeadingTrivia.Count > 0 ? LeadingTrivia[0].Span.Start : span.Start;
        var fullEnd = TrailingTrivia.Count > 0 ? TrailingTrivia[TrailingTrivia.Count - 1].Span.End : span.End;
        FullSpan = new TextSpan(fullStart, fullEnd - fullStart);
    }

    /// <summary>
    /// Creates a new token with the specified leading trivia, preserving all other properties.
    /// </summary>
    public SyntaxToken WithLeadingTrivia(SyntaxTriviaList trivia) =>
        new SyntaxToken(Kind, Text, Span, trivia, TrailingTrivia, Value, IsMissing);

    /// <summary>
    /// Creates a new token with the specified trailing trivia, preserving all other properties.
    /// </summary>
    public SyntaxToken WithTrailingTrivia(SyntaxTriviaList trivia) =>
        new SyntaxToken(Kind, Text, Span, LeadingTrivia, trivia, Value, IsMissing);

    public bool Equals(SyntaxToken other) =>
        Kind == other.Kind &&
        Text == other.Text &&
        Span == other.Span &&
        IsMissing == other.IsMissing;

    public override bool Equals(object? obj) =>
        obj is SyntaxToken other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = (int)Kind * 397;
            hash = hash ^ (Text?.GetHashCode() ?? 0);
            hash = hash ^ Span.GetHashCode();
            return hash;
        }
    }

    public static bool operator ==(SyntaxToken left, SyntaxToken right) =>
        left.Equals(right);

    public static bool operator !=(SyntaxToken left, SyntaxToken right) =>
        !left.Equals(right);

    public override string ToString() => Text ?? string.Empty;
}
