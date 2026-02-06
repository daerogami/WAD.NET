using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace WAD.NET.ZScript.Syntax;

/// <summary>
/// An immutable list of syntax nodes of type <typeparamref name="T"/>,
/// typically used for comma-separated constructs such as parameter lists.
/// </summary>
/// <typeparam name="T">The syntax node type.</typeparam>
public readonly struct SeparatedSyntaxList<T> : IReadOnlyList<T> where T : SyntaxNode
{
    private readonly ImmutableArray<T> _items;

    /// <summary>
    /// An empty separated syntax list.
    /// </summary>
    public static SeparatedSyntaxList<T> Empty { get; } = new SeparatedSyntaxList<T>(ImmutableArray<T>.Empty);

    /// <summary>
    /// The number of items in this list.
    /// </summary>
    public int Count => _items.IsDefault ? 0 : _items.Length;

    /// <summary>
    /// Gets the item at the specified index.
    /// </summary>
    public T this[int index] => _items[index];

    /// <summary>
    /// Creates a new <see cref="SeparatedSyntaxList{T}"/> from an immutable array.
    /// </summary>
    /// <param name="items">The items.</param>
    public SeparatedSyntaxList(ImmutableArray<T> items)
    {
        _items = items.IsDefault ? ImmutableArray<T>.Empty : items;
    }

    public ImmutableArray<T>.Enumerator GetEnumerator() =>
        (_items.IsDefault ? ImmutableArray<T>.Empty : _items).GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        var items = _items.IsDefault ? ImmutableArray<T>.Empty : _items;
        return ((IEnumerable<T>)items).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        var items = _items.IsDefault ? ImmutableArray<T>.Empty : _items;
        return ((IEnumerable)items).GetEnumerator();
    }
}
