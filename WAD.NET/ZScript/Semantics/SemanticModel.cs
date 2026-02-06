using System;
using System.Collections.Generic;
using System.Linq;
using WAD.NET.ZScript.Syntax;

namespace WAD.NET.ZScript.Semantics;

/// <summary>
/// Provides semantic analysis over a parsed ZScript/DECORATE compilation unit.
/// Resolves classes, inheritance, properties, flags, and states.
/// </summary>
public sealed class SemanticModel
{
    private readonly CompilationUnitSyntax _root;
    private readonly SymbolTable _symbols;

    /// <summary>
    /// Creates a semantic model for a single compilation unit.
    /// </summary>
    /// <param name="root">The parsed compilation unit.</param>
    public SemanticModel(CompilationUnitSyntax root)
    {
        _root = root;
        _symbols = BuildSymbolTable(root);
    }

    private SemanticModel(CompilationUnitSyntax root, SymbolTable symbols)
    {
        _root = root;
        _symbols = symbols;
    }

    /// <summary>
    /// Creates a semantic model spanning multiple compilation units.
    /// All classes from all units are combined into a single symbol table
    /// so that cross-file inheritance can be resolved.
    /// </summary>
    /// <param name="units">The compilation units to analyze.</param>
    /// <returns>A semantic model with the combined symbol table.</returns>
    public static SemanticModel Create(IEnumerable<CompilationUnitSyntax> units)
    {
        var table = new SymbolTable();
        CompilationUnitSyntax? firstUnit = null;

        foreach (var unit in units)
        {
            if (firstUnit == null)
                firstUnit = unit;

            var visitor = new SymbolBuildingVisitor();
            visitor.Visit(unit);

            foreach (var cls in visitor.SymbolTable.AllClasses)
                table.AddClass(cls);
        }

        table.ResolveBaseTypes();

        if (firstUnit == null)
            throw new ArgumentException("At least one compilation unit is required.", nameof(units));

        return new SemanticModel(firstUnit, table);
    }

    /// <summary>
    /// All class symbols discovered in the analyzed compilation units.
    /// </summary>
    public IEnumerable<IClassSymbol> AllClasses => _symbols.AllClasses;

    /// <summary>
    /// Looks up a class by name (case-insensitive).
    /// </summary>
    /// <returns>The class symbol, or <see langword="null"/> if not found.</returns>
    public IClassSymbol? LookupClass(string name) => _symbols.LookupClass(name);

    /// <summary>
    /// Returns the symbol declared by the given syntax node, if any.
    /// </summary>
    public ISymbol? GetDeclaredSymbol(SyntaxNode node)
    {
        if (node is ClassDeclarationSyntax classDecl)
            return _symbols.LookupClass(classDecl.Name);

        if (node is ActorDeclarationSyntax actorDecl)
            return _symbols.LookupClass(actorDecl.Name);

        // For method, field, etc., search through classes
        foreach (var cls in _symbols.AllClasses)
        {
            if (node is MethodDeclarationSyntax methodDecl)
            {
                foreach (var method in cls.Methods)
                {
                    if (method.DeclaringSyntax == node)
                        return method;
                }
            }
            else if (node is FieldDeclarationSyntax)
            {
                foreach (var field in cls.Fields)
                {
                    if (field.DeclaringSyntax == node)
                        return field;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Returns the symbol referenced by the given expression, if resolvable.
    /// </summary>
    public ISymbol? GetSymbolInfo(ExpressionSyntax expression)
    {
        if (expression is IdentifierExpressionSyntax identifier)
        {
            // Try to resolve as a class name
            return _symbols.LookupClass(identifier.Identifier.Text);
        }

        return null;
    }

    /// <summary>
    /// Walks the inheritance chain from the given symbol upward through base types.
    /// Includes the starting symbol as the first element.
    /// </summary>
    public IEnumerable<IClassSymbol> GetInheritanceChain(IClassSymbol symbol)
    {
        var current = symbol;
        while (current != null)
        {
            yield return current;
            current = current.BaseType;
        }
    }

    /// <summary>
    /// Finds all classes that declare <c>replaces</c> for the given actor name (case-insensitive).
    /// </summary>
    public IEnumerable<IClassSymbol> GetReplacers(string actorName)
    {
        return _symbols.AllClasses
            .Where(c => c.ReplacesName != null &&
                        c.ReplacesName.Equals(actorName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Determines whether the given class is a monster.
    /// A class is considered a monster if it has the <c>ISMONSTER</c> flag set.
    /// </summary>
    public bool IsMonster(IClassSymbol classSymbol)
    {
        if (classSymbol.HasFlag("ISMONSTER"))
            return true;

        // Check the inheritance chain for ISMONSTER
        foreach (var ancestor in GetInheritanceChain(classSymbol))
        {
            if (ancestor.HasFlag("ISMONSTER"))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether the given class derives from <c>Weapon</c>.
    /// </summary>
    public bool IsWeapon(IClassSymbol classSymbol)
    {
        return GetInheritanceChain(classSymbol)
            .Any(c => c.Name.Equals("Weapon", StringComparison.OrdinalIgnoreCase));
    }

    private static SymbolTable BuildSymbolTable(CompilationUnitSyntax root)
    {
        var visitor = new SymbolBuildingVisitor();
        visitor.Visit(root);
        visitor.SymbolTable.ResolveBaseTypes();
        return visitor.SymbolTable;
    }
}
