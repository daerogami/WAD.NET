using System.Collections.Generic;
using WAD.NET.ZScript.Syntax;

namespace WAD.NET.ZScript.Semantics;

/// <summary>
/// Concrete implementation of <see cref="ITypeSymbol"/> for type references.
/// </summary>
public class TypeSymbol : ITypeSymbol
{
    public TypeSymbol(string typeName, SyntaxNode? declaringSyntax = null)
    {
        TypeName = typeName;
        Name = typeName;
        DeclaringSyntax = declaringSyntax;
    }

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public SymbolKind Kind => SymbolKind.Class;

    /// <inheritdoc/>
    public SyntaxNode? DeclaringSyntax { get; }

    /// <inheritdoc/>
    public string TypeName { get; }
}

/// <summary>
/// Concrete implementation of <see cref="IMethodSymbol"/>.
/// </summary>
public class MethodSymbol : IMethodSymbol
{
    public MethodSymbol(
        string name,
        ITypeSymbol? returnType,
        IReadOnlyList<IParameterSymbol> parameters,
        bool isVirtual,
        bool isOverride,
        bool isNative,
        bool isAction,
        SyntaxNode? declaringSyntax)
    {
        Name = name;
        ReturnType = returnType;
        Parameters = parameters;
        IsVirtual = isVirtual;
        IsOverride = isOverride;
        IsNative = isNative;
        IsAction = isAction;
        DeclaringSyntax = declaringSyntax;
    }

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public SymbolKind Kind => SymbolKind.Method;

    /// <inheritdoc/>
    public SyntaxNode? DeclaringSyntax { get; }

    /// <inheritdoc/>
    public ITypeSymbol? ReturnType { get; }

    /// <inheritdoc/>
    public IReadOnlyList<IParameterSymbol> Parameters { get; }

    /// <inheritdoc/>
    public bool IsVirtual { get; }

    /// <inheritdoc/>
    public bool IsOverride { get; }

    /// <inheritdoc/>
    public bool IsNative { get; }

    /// <inheritdoc/>
    public bool IsAction { get; }
}

/// <summary>
/// Concrete implementation of <see cref="IFieldSymbol"/>.
/// </summary>
public class FieldSymbol : IFieldSymbol
{
    public FieldSymbol(
        string name,
        ITypeSymbol? type,
        bool isStatic,
        bool isReadOnly,
        SyntaxNode? declaringSyntax)
    {
        Name = name;
        Type = type;
        IsStatic = isStatic;
        IsReadOnly = isReadOnly;
        DeclaringSyntax = declaringSyntax;
    }

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public SymbolKind Kind => SymbolKind.Field;

    /// <inheritdoc/>
    public SyntaxNode? DeclaringSyntax { get; }

    /// <inheritdoc/>
    public ITypeSymbol? Type { get; }

    /// <inheritdoc/>
    public bool IsStatic { get; }

    /// <inheritdoc/>
    public bool IsReadOnly { get; }
}

/// <summary>
/// Concrete implementation of <see cref="IPropertySymbol"/>.
/// </summary>
public class PropertySymbol : IPropertySymbol
{
    public PropertySymbol(string name, ITypeSymbol? type, SyntaxNode? declaringSyntax)
    {
        Name = name;
        Type = type;
        DeclaringSyntax = declaringSyntax;
    }

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public SymbolKind Kind => SymbolKind.Property;

    /// <inheritdoc/>
    public SyntaxNode? DeclaringSyntax { get; }

    /// <inheritdoc/>
    public ITypeSymbol? Type { get; }
}

/// <summary>
/// Concrete implementation of <see cref="IParameterSymbol"/>.
/// </summary>
public class ParameterSymbol : IParameterSymbol
{
    public ParameterSymbol(string name, ITypeSymbol? type, SyntaxNode? declaringSyntax)
    {
        Name = name;
        Type = type;
        DeclaringSyntax = declaringSyntax;
    }

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public SymbolKind Kind => SymbolKind.Parameter;

    /// <inheritdoc/>
    public SyntaxNode? DeclaringSyntax { get; }

    /// <inheritdoc/>
    public ITypeSymbol? Type { get; }
}

/// <summary>
/// Concrete implementation of <see cref="ILocalSymbol"/>.
/// </summary>
public class LocalSymbol : ILocalSymbol
{
    public LocalSymbol(string name, ITypeSymbol? type, SyntaxNode? declaringSyntax)
    {
        Name = name;
        Type = type;
        DeclaringSyntax = declaringSyntax;
    }

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public SymbolKind Kind => SymbolKind.Local;

    /// <inheritdoc/>
    public SyntaxNode? DeclaringSyntax { get; }

    /// <inheritdoc/>
    public ITypeSymbol? Type { get; }
}

/// <summary>
/// Concrete implementation of <see cref="IStateSymbol"/> representing a state label and its frames.
/// </summary>
public class StateSymbol : IStateSymbol
{
    public StateSymbol(string label, IReadOnlyList<StateFrameInfo> frames, SyntaxNode? declaringSyntax)
    {
        Label = label;
        Name = label;
        Frames = frames;
        DeclaringSyntax = declaringSyntax;
    }

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public SymbolKind Kind => SymbolKind.State;

    /// <inheritdoc/>
    public SyntaxNode? DeclaringSyntax { get; }

    /// <inheritdoc/>
    public string Label { get; }

    /// <inheritdoc/>
    public IReadOnlyList<StateFrameInfo> Frames { get; }
}
