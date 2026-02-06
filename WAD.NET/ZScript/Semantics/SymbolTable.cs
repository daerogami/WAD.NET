using System;
using System.Collections.Generic;
using System.Linq;

namespace WAD.NET.ZScript.Semantics;

/// <summary>
/// Stores all resolved class symbols and provides lookup and base type resolution.
/// </summary>
public sealed class SymbolTable
{
    private readonly Dictionary<string, IClassSymbol> _classes =
        new Dictionary<string, IClassSymbol>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// All class symbols in this table.
    /// </summary>
    public IEnumerable<IClassSymbol> AllClasses => _classes.Values;

    /// <summary>
    /// Registers a class symbol. If a class with the same name already exists, it is overwritten.
    /// </summary>
    public void AddClass(IClassSymbol symbol) => _classes[symbol.Name] = symbol;

    /// <summary>
    /// Looks up a class by name (case-insensitive).
    /// </summary>
    /// <returns>The class symbol, or <see langword="null"/> if not found.</returns>
    public IClassSymbol? LookupClass(string name)
    {
        _classes.TryGetValue(name, out var symbol);
        return symbol;
    }

    /// <summary>
    /// Resolves the <see cref="ClassSymbol.BaseType"/> property for all classes whose
    /// base type name matches a registered class.
    /// </summary>
    public void ResolveBaseTypes()
    {
        foreach (var cls in _classes.Values.OfType<ClassSymbol>())
        {
            if (cls.BaseTypeName != null)
            {
                cls.SetBaseType(LookupClass(cls.BaseTypeName));
            }
        }
    }
}
