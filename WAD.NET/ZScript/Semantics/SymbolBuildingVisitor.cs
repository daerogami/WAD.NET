using System;
using System.Collections.Generic;
using System.Linq;
using WAD.NET.ZScript.Syntax;

namespace WAD.NET.ZScript.Semantics;

/// <summary>
/// A <see cref="SyntaxVisitor"/> that walks the syntax tree and builds a <see cref="SymbolTable"/>
/// containing all class, method, field, and state symbols.
/// </summary>
public class SymbolBuildingVisitor : SyntaxVisitor
{
    /// <summary>
    /// The symbol table populated by this visitor.
    /// </summary>
    public SymbolTable SymbolTable { get; } = new SymbolTable();

    /// <inheritdoc/>
    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        var symbol = new ClassSymbol(
            node.Name,
            node.BaseClassName,
            node.ReplacesClassName,
            node.DoomEdNumber,
            node);

        // Process Default block
        var defaultBlock = node.DefaultBlock;
        if (defaultBlock != null)
        {
            ProcessDefaultBlock(symbol, defaultBlock);
        }

        // Process States block
        var statesBlock = node.StatesBlock;
        if (statesBlock != null)
        {
            ProcessStatesBlock(symbol, statesBlock);
        }

        // Process methods
        foreach (var method in node.Methods)
        {
            ProcessMethod(symbol, method);
        }

        // Process fields
        foreach (var field in node.Fields)
        {
            ProcessField(symbol, field);
        }

        SymbolTable.AddClass(symbol);
    }

    /// <inheritdoc/>
    public override void VisitActorDeclaration(ActorDeclarationSyntax node)
    {
        var symbol = new ClassSymbol(
            node.Name,
            node.BaseName,
            node.ReplacesName,
            node.DoomEdNumber,
            node);

        // Actor body items are wrapped in synthetic DefaultBlockSyntax and StatesBlockSyntax
        foreach (var item in node.Body)
        {
            if (item is DefaultBlockSyntax defaultBlock)
            {
                ProcessDefaultBlock(symbol, defaultBlock);
            }
            else if (item is StatesBlockSyntax statesBlock)
            {
                ProcessStatesBlock(symbol, statesBlock);
            }
        }

        SymbolTable.AddClass(symbol);
    }

    private void ProcessDefaultBlock(ClassSymbol symbol, DefaultBlockSyntax defaultBlock)
    {
        foreach (var item in defaultBlock.Items)
        {
            if (item is PropertyAssignmentSyntax prop)
            {
                symbol.AddProperty(prop.FullPropertyName, ExtractPropertyValue(prop));
            }
            else if (item is FlagDefinitionSyntax flag)
            {
                if (flag.IsSet)
                {
                    symbol.AddFlag(flag.FullFlagName);
                }
                else
                {
                    symbol.RemoveFlag(flag.FullFlagName);
                }
            }
        }
    }

    private void ProcessStatesBlock(ClassSymbol symbol, StatesBlockSyntax statesBlock)
    {
        string? currentLabel = null;
        SyntaxNode? currentLabelNode = null;
        var currentFrames = new List<StateFrameInfo>();

        foreach (var state in statesBlock.States)
        {
            if (state is StateLabelSyntax label)
            {
                // Flush previous label group
                if (currentLabel != null)
                {
                    symbol.AddState(new StateSymbol(
                        currentLabel,
                        currentFrames.ToArray(),
                        currentLabelNode));
                }

                currentLabel = label.LabelName;
                currentLabelNode = label;
                currentFrames = new List<StateFrameInfo>();
            }
            else if (state is StateFrameSyntax frame)
            {
                var info = new StateFrameInfo
                {
                    SpriteName = frame.SpriteName,
                    FrameLetters = frame.Frames,
                    Duration = frame.DurationTics ?? 0,
                    IsBright = frame.IsBright,
                    ActionName = frame.Action?.ActionIdentifier?.Text
                };
                currentFrames.Add(info);
            }
            // StateGoto, StateLoop, StateStop, StateWait, StateFail are flow control
            // and do not contribute frame data.
        }

        // Flush the last label group
        if (currentLabel != null)
        {
            symbol.AddState(new StateSymbol(
                currentLabel,
                currentFrames.ToArray(),
                currentLabelNode));
        }
    }

    private void ProcessMethod(ClassSymbol symbol, MethodDeclarationSyntax method)
    {
        var returnType = ResolveTypeSyntax(method.ReturnType);

        var parameters = new List<IParameterSymbol>();
        foreach (var param in method.Parameters)
        {
            var paramType = ResolveTypeSyntax(param.Type);
            parameters.Add(new ParameterSymbol(param.Identifier.Text, paramType, param));
        }

        var methodSymbol = new MethodSymbol(
            method.Name,
            returnType,
            parameters,
            method.IsVirtual,
            method.IsOverride,
            method.IsNative,
            method.IsAction,
            method);

        symbol.AddMethod(methodSymbol);
    }

    private void ProcessField(ClassSymbol symbol, FieldDeclarationSyntax field)
    {
        var fieldType = ResolveTypeSyntax(field.Type);

        bool isStatic = false;
        bool isReadOnly = false;
        foreach (var mod in field.Modifiers)
        {
            if (mod.Kind == SyntaxTokenKind.StaticKeyword)
                isStatic = true;
            if (string.Equals(mod.Text, "readonly", StringComparison.OrdinalIgnoreCase))
                isReadOnly = true;
        }

        foreach (var variable in field.Variables)
        {
            var fieldSymbol = new FieldSymbol(
                variable.Identifier.Text,
                fieldType,
                isStatic,
                isReadOnly,
                field);
            symbol.AddField(fieldSymbol);
        }
    }

    private static ITypeSymbol? ResolveTypeSyntax(TypeSyntax? typeSyntax)
    {
        if (typeSyntax == null)
            return null;

        var typeName = typeSyntax.ToString();
        return new TypeSymbol(typeName, typeSyntax);
    }

    /// <summary>
    /// Extracts a simple value from a property assignment expression.
    /// Returns the literal value for numeric/string/bool literals,
    /// the identifier text for identifier expressions, or <see langword="null"/> for complex expressions.
    /// </summary>
    private static object? ExtractPropertyValue(PropertyAssignmentSyntax prop)
    {
        if (prop.Values.Length == 0)
            return null;

        // Take the first value expression
        var expr = prop.Values[0];
        return ExtractExpressionValue(expr);
    }

    private static object? ExtractExpressionValue(ExpressionSyntax expr)
    {
        if (expr is LiteralExpressionSyntax literal)
        {
            // Convert long to int when it fits, since ZScript commonly uses int-sized values
            if (literal.Value is long longVal && longVal >= int.MinValue && longVal <= int.MaxValue)
                return (int)longVal;
            return literal.Value;
        }

        if (expr is IdentifierExpressionSyntax identifier)
        {
            return identifier.Identifier.Text;
        }

        if (expr is UnaryExpressionSyntax unary)
        {
            var operandValue = ExtractExpressionValue(unary.Operand);
            if (unary.OperatorToken.Kind == SyntaxTokenKind.Minus)
            {
                if (operandValue is int intVal)
                    return -intVal;
                if (operandValue is long longVal)
                    return -longVal;
                if (operandValue is double doubleVal)
                    return -doubleVal;
            }
            return null;
        }

        // For complex expressions, return null
        return null;
    }
}
