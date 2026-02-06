using System.Collections.Immutable;
using System.Linq;
using System.Text;
using WAD.NET.ZScript.Semantics;
using WAD.NET.ZScript.Syntax;

namespace WAD.NET.ZScript.Emit;

/// <summary>
/// Converts ZScript AST to C# code for native .NET mods.
/// </summary>
public sealed class CSharpEmitter : SyntaxVisitor<string>
{
    private readonly SemanticModel _model;
    private readonly StringBuilder _output = new StringBuilder();
    private int _indent;

    /// <summary>
    /// Creates a new <see cref="CSharpEmitter"/> with the specified semantic model.
    /// </summary>
    /// <param name="model">The semantic model for type resolution.</param>
    public CSharpEmitter(SemanticModel model)
    {
        _model = model;
    }

    /// <summary>
    /// Emits C# source code for the given compilation unit.
    /// </summary>
    /// <param name="root">The compilation unit to transpile.</param>
    /// <returns>The generated C# source code.</returns>
    public string Emit(CompilationUnitSyntax root)
    {
        _output.Clear();
        _indent = 0;

        _output.AppendLine("// Auto-generated from ZScript");
        _output.AppendLine("using System;");
        _output.AppendLine("using ZDoom.Runtime;");
        _output.AppendLine();

        foreach (var member in root.Members)
        {
            Visit(member);
            _output.AppendLine();
        }

        return _output.ToString();
    }

    /// <inheritdoc/>
    public override string? VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        WriteIndent();
        _output.Append("public class ");
        _output.Append(node.Name);

        if (node.BaseClassName != null)
        {
            _output.Append(" : ");
            _output.Append(MapType(node.BaseClassName));
        }

        _output.AppendLine();
        WriteIndent();
        _output.AppendLine("{");
        _indent++;

        // Emit fields from Default block as properties
        if (node.DefaultBlock != null)
        {
            foreach (var prop in node.DefaultBlock.Properties)
            {
                EmitProperty(prop);
            }
        }

        // Emit methods
        foreach (var method in node.Methods)
        {
            Visit(method);
        }

        // Emit states as a state machine
        if (node.StatesBlock != null)
        {
            EmitStates(node.StatesBlock);
        }

        _indent--;
        WriteIndent();
        _output.AppendLine("}");

        return null;
    }

    /// <inheritdoc/>
    public override string? VisitActorDeclaration(ActorDeclarationSyntax node)
    {
        WriteIndent();
        _output.Append("public class ");
        _output.Append(node.Name);

        if (node.BaseName != null)
        {
            _output.Append(" : ");
            _output.Append(MapType(node.BaseName));
        }

        _output.AppendLine();
        WriteIndent();
        _output.AppendLine("{");
        _indent++;

        // Emit fields from Default block as properties
        var defaultBlock = node.Body.OfType<DefaultBlockSyntax>().FirstOrDefault();
        if (defaultBlock != null)
        {
            foreach (var prop in defaultBlock.Properties)
            {
                EmitProperty(prop);
            }
        }

        // Emit methods
        foreach (var method in node.Body.OfType<MethodDeclarationSyntax>())
        {
            Visit(method);
        }

        // Emit states
        var statesBlock = node.Body.OfType<StatesBlockSyntax>().FirstOrDefault();
        if (statesBlock != null)
        {
            EmitStates(statesBlock);
        }

        _indent--;
        WriteIndent();
        _output.AppendLine("}");

        return null;
    }

    /// <inheritdoc/>
    public override string? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        WriteIndent();

        // Modifiers
        foreach (var mod in node.Modifiers)
        {
            var modText = mod.Text.ToLowerInvariant();
            if (modText == "virtual" || modText == "override" || modText == "static")
            {
                _output.Append(modText);
                _output.Append(' ');
            }
        }

        _output.Append("public ");
        _output.Append(MapType(node.ReturnType.ToString()));
        _output.Append(' ');
        _output.Append(node.Identifier.Text);
        _output.Append('(');

        for (int i = 0; i < node.Parameters.Count; i++)
        {
            if (i > 0)
                _output.Append(", ");

            var param = node.Parameters[i];
            _output.Append(MapType(param.Type.ToString()));
            _output.Append(' ');
            _output.Append(param.Identifier.Text);
        }

        _output.Append(')');

        if (node.Body != null)
        {
            _output.AppendLine();
            WriteIndent();
            _output.AppendLine("{");
            _indent++;

            // Emit a stub comment
            WriteIndent();
            _output.AppendLine("// Method body omitted");

            _indent--;
            WriteIndent();
            _output.AppendLine("}");
        }
        else
        {
            _output.AppendLine(";");
        }

        return null;
    }

    private void EmitProperty(PropertyAssignmentSyntax prop)
    {
        WriteIndent();

        var propType = InferPropertyType(prop.FullPropertyName);
        _output.Append("public ");
        _output.Append(propType);
        _output.Append(' ');
        _output.Append(prop.PropertyName.Text);
        _output.Append(" { get; set; }");

        if (prop.Values.Length > 0)
        {
            _output.Append(" = ");
            _output.Append(EmitExpression(prop.Values[0]));
        }

        _output.AppendLine(";");
    }

    private void EmitStates(StatesBlockSyntax states)
    {
        WriteIndent();
        _output.AppendLine("protected override void DefineStates()");
        WriteIndent();
        _output.AppendLine("{");
        _indent++;

        foreach (var state in states.States)
        {
            if (state is StateLabelSyntax label)
            {
                WriteIndent();
                _output.Append("DefineLabel(\"");
                _output.Append(label.LabelName);
                _output.AppendLine("\");");
            }
            else if (state is StateFrameSyntax frame)
            {
                WriteIndent();
                _output.Append("AddFrame(\"");
                _output.Append(frame.SpriteName);
                _output.Append("\", \"");
                _output.Append(frame.Frames);
                _output.Append("\", ");
                // DurationTics may be null if token value is stored as long
                var duration = frame.DurationTics
                    ?? (frame.Duration.Value is long lv ? (int)lv : 0);
                _output.Append(duration);

                if (frame.IsBright)
                    _output.Append(", bright: true");

                if (frame.Action != null && !frame.Action.IsAnonymousBlock)
                {
                    var actionName = frame.Action.ActionIdentifier?.Text;
                    if (actionName != null)
                    {
                        _output.Append(", () => ");
                        _output.Append(actionName);
                        _output.Append("()");
                    }
                }

                _output.AppendLine(");");
            }
            else if (state is StateGotoSyntax gotoState)
            {
                WriteIndent();
                _output.Append("Goto(\"");
                _output.Append(gotoState.TargetLabel);
                _output.Append("\", ");
                _output.Append(gotoState.FrameOffset);
                _output.AppendLine(");");
            }
            else if (state is StateLoopSyntax)
            {
                WriteIndent();
                _output.AppendLine("Loop();");
            }
            else if (state is StateStopSyntax)
            {
                WriteIndent();
                _output.AppendLine("Stop();");
            }
            else if (state is StateWaitSyntax)
            {
                WriteIndent();
                _output.AppendLine("Wait();");
            }
            else if (state is StateFailSyntax)
            {
                WriteIndent();
                _output.AppendLine("Fail();");
            }
        }

        _indent--;
        WriteIndent();
        _output.AppendLine("}");
    }

    private string MapType(string zscriptType)
    {
        return zscriptType.ToLowerInvariant() switch
        {
            "int" => "int",
            "uint" => "uint",
            "float" => "float",
            "double" => "double",
            "bool" => "bool",
            "string" => "string",
            "name" => "string",
            "void" => "void",
            "vector2" => "Vector2",
            "vector3" => "Vector3",
            "state" => "State",
            "actor" => "Actor",
            "weapon" => "Weapon",
            "sound" => "string",
            "color" => "int",
            _ => zscriptType
        };
    }

    private string InferPropertyType(string propertyName)
    {
        return propertyName.ToLowerInvariant() switch
        {
            "health" or "damage" or "speed" or "radius" or "height" or "mass"
                or "reactiontime" or "painchance" or "threshold" => "int",
            "scale" or "alpha" or "renderstyle" => "float",
            "obituary" or "tag" or "hitobituary" => "string",
            _ => "object"
        };
    }

    private void WriteIndent()
    {
        _output.Append(new string('\t', _indent));
    }

    private string EmitExpression(ExpressionSyntax expr)
    {
        return expr switch
        {
            LiteralExpressionSyntax lit => lit.Token.Text,
            IdentifierExpressionSyntax id => id.Identifier.Text,
            BinaryExpressionSyntax bin =>
                $"({EmitExpression(bin.Left)} {bin.OperatorToken.Text} {EmitExpression(bin.Right)})",
            MemberAccessExpressionSyntax access =>
                $"{EmitExpression(access.Expression)}.{access.Name.Text}",
            InvocationExpressionSyntax inv =>
                $"{EmitExpression(inv.Expression)}({string.Join(", ", inv.Arguments.Select(a => EmitExpression(a.Expression)))})",
            ParenthesizedExpressionSyntax paren =>
                $"({EmitExpression(paren.Expression)})",
            UnaryExpressionSyntax unary =>
                $"{unary.OperatorToken.Text}{EmitExpression(unary.Operand)}",
            _ => "/* unknown */"
        };
    }
}
