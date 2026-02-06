using System;

namespace WAD.NET.ZScript.Syntax;

/// <summary>
/// Represents a span of text in source code, defined by a start position and length.
/// </summary>
public readonly struct TextSpan : IEquatable<TextSpan>
{
    /// <summary>
    /// The start position of the span (zero-based character offset).
    /// </summary>
    public int Start { get; }

    /// <summary>
    /// The length of the span in characters.
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// The exclusive end position of the span.
    /// </summary>
    public int End => Start + Length;

    /// <summary>
    /// Creates a new <see cref="TextSpan"/> with the specified start position and length.
    /// </summary>
    /// <param name="start">The start position (zero-based character offset).</param>
    /// <param name="length">The length in characters.</param>
    public TextSpan(int start, int length)
    {
        Start = start;
        Length = length;
    }

    public bool Equals(TextSpan other) =>
        Start == other.Start && Length == other.Length;

    public override bool Equals(object? obj) =>
        obj is TextSpan other && Equals(other);

    public override int GetHashCode() =>
        Start * 397 ^ Length;

    public static bool operator ==(TextSpan left, TextSpan right) =>
        left.Equals(right);

    public static bool operator !=(TextSpan left, TextSpan right) =>
        !left.Equals(right);

    public override string ToString() => $"[{Start}..{End})";
}
