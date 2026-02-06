using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace WAD.NET.ZScript.Syntax;

/// <summary>
/// Defines the kinds of syntax trivia (non-significant tokens such as whitespace and comments).
/// </summary>
public enum SyntaxTriviaKind
{
    None,
    Whitespace,
    EndOfLine,
    SingleLineComment,
    MultiLineComment,
    PreprocessorDirective,
    SkippedTokens
}

/// <summary>
/// Represents a piece of syntax trivia (whitespace, comment, preprocessor directive, etc.).
/// </summary>
public readonly struct SyntaxTrivia : IEquatable<SyntaxTrivia>
{
    /// <summary>
    /// The kind of trivia.
    /// </summary>
    public SyntaxTriviaKind Kind { get; }

    /// <summary>
    /// The text content of the trivia.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// The span of the trivia in the source text.
    /// </summary>
    public TextSpan Span { get; }

    /// <summary>
    /// Returns <see langword="true"/> if this trivia is whitespace or end-of-line.
    /// </summary>
    public bool IsWhitespace => Kind == SyntaxTriviaKind.Whitespace || Kind == SyntaxTriviaKind.EndOfLine;

    /// <summary>
    /// Returns <see langword="true"/> if this trivia is a comment.
    /// </summary>
    public bool IsComment => Kind == SyntaxTriviaKind.SingleLineComment || Kind == SyntaxTriviaKind.MultiLineComment;

    /// <summary>
    /// Creates a new <see cref="SyntaxTrivia"/> instance.
    /// </summary>
    /// <param name="kind">The kind of trivia.</param>
    /// <param name="text">The text content.</param>
    /// <param name="span">The source span.</param>
    public SyntaxTrivia(SyntaxTriviaKind kind, string text, TextSpan span)
    {
        Kind = kind;
        Text = text;
        Span = span;
    }

    public bool Equals(SyntaxTrivia other) =>
        Kind == other.Kind && Text == other.Text && Span == other.Span;

    public override bool Equals(object? obj) =>
        obj is SyntaxTrivia other && Equals(other);

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

    public static bool operator ==(SyntaxTrivia left, SyntaxTrivia right) =>
        left.Equals(right);

    public static bool operator !=(SyntaxTrivia left, SyntaxTrivia right) =>
        !left.Equals(right);

    public override string ToString() => Text ?? string.Empty;
}

/// <summary>
/// An immutable list of <see cref="SyntaxTrivia"/> items.
/// </summary>
public readonly struct SyntaxTriviaList : IReadOnlyList<SyntaxTrivia>
{
    private readonly ImmutableArray<SyntaxTrivia> _items;

    /// <summary>
    /// An empty trivia list.
    /// </summary>
    public static SyntaxTriviaList Empty { get; } = new SyntaxTriviaList(ImmutableArray<SyntaxTrivia>.Empty);

    /// <summary>
    /// The number of trivia items in this list.
    /// </summary>
    public int Count => _items.IsDefault ? 0 : _items.Length;

    /// <summary>
    /// Gets the trivia at the specified index.
    /// </summary>
    public SyntaxTrivia this[int index] => _items[index];

    /// <summary>
    /// Creates a new <see cref="SyntaxTriviaList"/> from an immutable array.
    /// </summary>
    /// <param name="items">The trivia items.</param>
    public SyntaxTriviaList(ImmutableArray<SyntaxTrivia> items)
    {
        _items = items.IsDefault ? ImmutableArray<SyntaxTrivia>.Empty : items;
    }

    public ImmutableArray<SyntaxTrivia>.Enumerator GetEnumerator() =>
        (_items.IsDefault ? ImmutableArray<SyntaxTrivia>.Empty : _items).GetEnumerator();

    IEnumerator<SyntaxTrivia> IEnumerable<SyntaxTrivia>.GetEnumerator()
    {
        var items = _items.IsDefault ? ImmutableArray<SyntaxTrivia>.Empty : _items;
        return ((IEnumerable<SyntaxTrivia>)items).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        var items = _items.IsDefault ? ImmutableArray<SyntaxTrivia>.Empty : _items;
        return ((IEnumerable)items).GetEnumerator();
    }
}
