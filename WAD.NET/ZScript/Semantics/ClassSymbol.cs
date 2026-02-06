using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using WAD.NET.ZScript.Syntax;

namespace WAD.NET.ZScript.Semantics;

/// <summary>
/// Concrete implementation of <see cref="IClassSymbol"/> representing a resolved
/// ZScript class or DECORATE actor declaration.
/// </summary>
public class ClassSymbol : IClassSymbol
{
    private readonly List<IMethodSymbol> _methods = new List<IMethodSymbol>();
    private readonly List<IFieldSymbol> _fields = new List<IFieldSymbol>();
    private readonly Dictionary<string, object?> _properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    private ImmutableHashSet<string> _flags = ImmutableHashSet<string>.Empty.WithComparer(StringComparer.OrdinalIgnoreCase);
    private readonly List<IStateSymbol> _states = new List<IStateSymbol>();

    /// <summary>
    /// Creates a new <see cref="ClassSymbol"/> with the specified metadata.
    /// </summary>
    /// <param name="name">The class/actor name.</param>
    /// <param name="baseTypeName">The base class name, or <see langword="null"/>.</param>
    /// <param name="replacesName">The name of the class being replaced, or <see langword="null"/>.</param>
    /// <param name="doomEdNumber">The DoomEdNum, or <see langword="null"/>.</param>
    /// <param name="declaringSyntax">The syntax node that declared this class.</param>
    public ClassSymbol(
        string name,
        string? baseTypeName,
        string? replacesName,
        int? doomEdNumber,
        SyntaxNode? declaringSyntax)
    {
        Name = name;
        BaseTypeName = baseTypeName;
        ReplacesName = replacesName;
        DoomEdNumber = doomEdNumber;
        DeclaringSyntax = declaringSyntax;
    }

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public SymbolKind Kind => SymbolKind.Class;

    /// <inheritdoc/>
    public SyntaxNode? DeclaringSyntax { get; }

    /// <inheritdoc/>
    public IClassSymbol? BaseType { get; private set; }

    /// <inheritdoc/>
    public string? BaseTypeName { get; }

    /// <inheritdoc/>
    public string? ReplacesName { get; }

    /// <inheritdoc/>
    public int? DoomEdNumber { get; }

    /// <inheritdoc/>
    public IReadOnlyList<IMethodSymbol> Methods => _methods;

    /// <inheritdoc/>
    public IReadOnlyList<IFieldSymbol> Fields => _fields;

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object?> Properties => _properties;

    /// <inheritdoc/>
    public IReadOnlyCollection<string> Flags => _flags;

    /// <inheritdoc/>
    public bool HasFlag(string flag) => _flags.Contains(flag);

    /// <inheritdoc/>
    public IReadOnlyList<IStateSymbol> States => _states;

    /// <summary>
    /// Adds a method symbol to this class.
    /// </summary>
    public void AddMethod(IMethodSymbol method) => _methods.Add(method);

    /// <summary>
    /// Adds a field symbol to this class.
    /// </summary>
    public void AddField(IFieldSymbol field) => _fields.Add(field);

    /// <summary>
    /// Adds or updates a property in this class.
    /// </summary>
    public void AddProperty(string name, object? value) => _properties[name] = value;

    /// <summary>
    /// Adds a flag to this class.
    /// </summary>
    public void AddFlag(string flag) => _flags = _flags.Add(flag);

    /// <summary>
    /// Removes a flag from this class.
    /// </summary>
    public void RemoveFlag(string flag) => _flags = _flags.Remove(flag);

    /// <summary>
    /// Adds a state symbol to this class.
    /// </summary>
    public void AddState(IStateSymbol state) => _states.Add(state);

    /// <summary>
    /// Sets the resolved base type. Called during base type resolution.
    /// </summary>
    public void SetBaseType(IClassSymbol? baseType) => BaseType = baseType;
}
