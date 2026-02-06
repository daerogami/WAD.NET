using System.Collections.Generic;
using WAD.NET.ZScript.Syntax;

namespace WAD.NET.ZScript.Semantics;

/// <summary>
/// Classifies the kind of a semantic symbol.
/// </summary>
public enum SymbolKind
{
    Class,
    Struct,
    Enum,
    Method,
    Field,
    Property,
    Local,
    Parameter,
    State,
    StateLabel
}

/// <summary>
/// Base interface for all semantic symbols resolved from the syntax tree.
/// </summary>
public interface ISymbol
{
    /// <summary>
    /// The declared name of the symbol.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The kind of this symbol.
    /// </summary>
    SymbolKind Kind { get; }

    /// <summary>
    /// The syntax node that declared this symbol, or <see langword="null"/> for built-in symbols.
    /// </summary>
    SyntaxNode? DeclaringSyntax { get; }
}

/// <summary>
/// Represents a type reference in the semantic model.
/// </summary>
public interface ITypeSymbol : ISymbol
{
    /// <summary>
    /// The fully-qualified type name.
    /// </summary>
    string TypeName { get; }
}

/// <summary>
/// Represents a class or actor declaration in ZScript/DECORATE.
/// </summary>
public interface IClassSymbol : ISymbol
{
    /// <summary>
    /// The resolved base type, or <see langword="null"/> if no base class was found.
    /// </summary>
    IClassSymbol? BaseType { get; }

    /// <summary>
    /// The textual name of the base type as written in source, or <see langword="null"/> if none.
    /// </summary>
    string? BaseTypeName { get; }

    /// <summary>
    /// The name of the class this one replaces, or <see langword="null"/> if not replacing.
    /// </summary>
    string? ReplacesName { get; }

    /// <summary>
    /// The DoomEdNum (editor number), or <see langword="null"/> if not specified.
    /// </summary>
    int? DoomEdNumber { get; }

    /// <summary>
    /// All method declarations in this class.
    /// </summary>
    IReadOnlyList<IMethodSymbol> Methods { get; }

    /// <summary>
    /// All field declarations in this class.
    /// </summary>
    IReadOnlyList<IFieldSymbol> Fields { get; }

    /// <summary>
    /// Properties defined in the Default block, keyed by name (case-insensitive).
    /// </summary>
    IReadOnlyDictionary<string, object?> Properties { get; }

    /// <summary>
    /// Flags set in the Default block (case-insensitive).
    /// </summary>
    IReadOnlyCollection<string> Flags { get; }

    /// <summary>
    /// Returns <see langword="true"/> if the given flag name is set (case-insensitive comparison).
    /// </summary>
    bool HasFlag(string flag);

    /// <summary>
    /// All state definitions in this class, grouped by label.
    /// </summary>
    IReadOnlyList<IStateSymbol> States { get; }
}

/// <summary>
/// Represents a method declaration.
/// </summary>
public interface IMethodSymbol : ISymbol
{
    /// <summary>
    /// The return type of the method, or <see langword="null"/> if unresolved.
    /// </summary>
    ITypeSymbol? ReturnType { get; }

    /// <summary>
    /// The parameters of the method.
    /// </summary>
    IReadOnlyList<IParameterSymbol> Parameters { get; }

    /// <summary>
    /// Whether the method has the <c>virtual</c> modifier.
    /// </summary>
    bool IsVirtual { get; }

    /// <summary>
    /// Whether the method has the <c>override</c> modifier.
    /// </summary>
    bool IsOverride { get; }

    /// <summary>
    /// Whether the method has the <c>native</c> modifier.
    /// </summary>
    bool IsNative { get; }

    /// <summary>
    /// Whether the method has the <c>action</c> modifier.
    /// </summary>
    bool IsAction { get; }
}

/// <summary>
/// Represents a field declaration.
/// </summary>
public interface IFieldSymbol : ISymbol
{
    /// <summary>
    /// The declared type of the field, or <see langword="null"/> if unresolved.
    /// </summary>
    ITypeSymbol? Type { get; }

    /// <summary>
    /// Whether the field has a <c>static</c> modifier.
    /// </summary>
    bool IsStatic { get; }

    /// <summary>
    /// Whether the field has a <c>readonly</c> modifier.
    /// </summary>
    bool IsReadOnly { get; }
}

/// <summary>
/// Represents a property declaration.
/// </summary>
public interface IPropertySymbol : ISymbol
{
    /// <summary>
    /// The declared type of the property, or <see langword="null"/> if unresolved.
    /// </summary>
    ITypeSymbol? Type { get; }
}

/// <summary>
/// Represents a method parameter.
/// </summary>
public interface IParameterSymbol : ISymbol
{
    /// <summary>
    /// The declared type of the parameter, or <see langword="null"/> if unresolved.
    /// </summary>
    ITypeSymbol? Type { get; }
}

/// <summary>
/// Represents a local variable.
/// </summary>
public interface ILocalSymbol : ISymbol
{
    /// <summary>
    /// The declared type of the local, or <see langword="null"/> if unresolved.
    /// </summary>
    ITypeSymbol? Type { get; }
}

/// <summary>
/// Represents a state label and its associated frames.
/// </summary>
public interface IStateSymbol : ISymbol
{
    /// <summary>
    /// The label name (e.g., "Spawn", "See", "Death").
    /// </summary>
    string Label { get; }

    /// <summary>
    /// The individual frames belonging to this state label.
    /// </summary>
    IReadOnlyList<StateFrameInfo> Frames { get; }
}
