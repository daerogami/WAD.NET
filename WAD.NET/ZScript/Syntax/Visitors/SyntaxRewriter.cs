using System.Collections.Immutable;

namespace WAD.NET.ZScript.Syntax;

/// <summary>
/// A <see cref="SyntaxVisitor{TResult}"/> that produces a new syntax tree by visiting all nodes
/// and reconstructing any node whose children changed. If no children change, the original
/// node reference is returned (structural sharing).
/// </summary>
public abstract class SyntaxRewriter : SyntaxVisitor<SyntaxNode>
{
    /// <summary>
    /// Visits a list of syntax nodes, returning the same array if nothing changed
    /// or a new immutable array with the visited results.
    /// </summary>
    protected ImmutableArray<T> VisitList<T>(ImmutableArray<T> list) where T : SyntaxNode
    {
        ImmutableArray<T>.Builder? builder = null;
        for (int i = 0; i < list.Length; i++)
        {
            var item = list[i];
            var visited = (T?)Visit(item);
            if (visited != item && builder == null)
            {
                builder = ImmutableArray.CreateBuilder<T>(list.Length);
                for (int j = 0; j < i; j++)
                    builder.Add(list[j]);
            }
            if (builder != null && visited != null)
                builder.Add(visited);
        }
        return builder?.ToImmutable() ?? list;
    }

    /// <summary>
    /// Visits a <see cref="SeparatedSyntaxList{T}"/>, returning the same list if nothing changed
    /// or a new list with the visited results. The <paramref name="changed"/> parameter indicates
    /// whether any item was modified.
    /// </summary>
    protected SeparatedSyntaxList<T> VisitSeparatedList<T>(SeparatedSyntaxList<T> list, out bool changed) where T : SyntaxNode
    {
        changed = false;
        ImmutableArray<T>.Builder? builder = null;
        for (int i = 0; i < list.Count; i++)
        {
            var item = list[i];
            var visited = (T?)Visit(item);
            if (visited != item && builder == null)
            {
                changed = true;
                builder = ImmutableArray.CreateBuilder<T>(list.Count);
                for (int j = 0; j < i; j++)
                    builder.Add(list[j]);
            }
            if (builder != null && visited != null)
                builder.Add(visited);
        }
        if (builder != null)
            return new SeparatedSyntaxList<T>(builder.ToImmutable());
        return list;
    }

    // ---------------------------------------------------------------
    // Declaration visitors
    // ---------------------------------------------------------------

    /// <inheritdoc />
    public override SyntaxNode? VisitCompilationUnit(CompilationUnitSyntax node)
    {
        var versionDirective = node.VersionDirective != null
            ? (VersionDirectiveSyntax?)Visit(node.VersionDirective)
            : null;
        var includes = VisitList(node.Includes);
        var members = VisitList(node.Members);

        if (versionDirective != node.VersionDirective ||
            includes != node.Includes ||
            members != node.Members)
        {
            return new CompilationUnitSyntax(versionDirective, includes, members, node.EndOfFileToken);
        }
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitVersionDirective(VersionDirectiveSyntax node)
    {
        // Leaf node (no child SyntaxNodes)
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitIncludeDirective(IncludeDirectiveSyntax node)
    {
        // Leaf node (no child SyntaxNodes)
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        var baseList = node.BaseList != null
            ? (BaseListSyntax?)Visit(node.BaseList)
            : null;
        var members = VisitList(node.Members);

        if (baseList != node.BaseList || members != node.Members)
        {
            return new ClassDeclarationSyntax(
                node.Modifiers, node.ClassKeyword, node.Identifier,
                baseList, node.ReplacesKeyword, node.ReplacesIdentifier,
                node.EditorNumber, node.OpenBraceToken, members, node.CloseBraceToken);
        }
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitActorDeclaration(ActorDeclarationSyntax node)
    {
        var body = VisitList(node.Body);

        if (body != node.Body)
        {
            return new ActorDeclarationSyntax(
                node.ActorKeyword, node.Identifier, node.ColonToken,
                node.BaseIdentifier, node.ReplacesKeyword, node.ReplacesIdentifier,
                node.EditorNumber, node.OpenBraceToken, body, node.CloseBraceToken);
        }
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitBaseList(BaseListSyntax node)
    {
        var baseType = (TypeSyntax?)Visit(node.BaseType);
        if (baseType != null && baseType != node.BaseType)
        {
            return new BaseListSyntax(node.ColonToken, baseType);
        }
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitDefaultBlock(DefaultBlockSyntax node)
    {
        var items = VisitList(node.Items);

        if (items != node.Items)
        {
            return new DefaultBlockSyntax(
                node.DefaultKeyword, node.OpenBraceToken, items, node.CloseBraceToken);
        }
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitPropertyAssignment(PropertyAssignmentSyntax node)
    {
        var values = VisitList(node.Values);

        if (values != node.Values)
        {
            return new PropertyAssignmentSyntax(
                node.PrefixIdentifier, node.DotToken, node.PropertyName,
                values, node.SemicolonToken);
        }
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitFlagDefinition(FlagDefinitionSyntax node)
    {
        // Leaf node (no child SyntaxNodes)
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitStatesBlock(StatesBlockSyntax node)
    {
        var states = VisitList(node.States);

        if (states != node.States)
        {
            return new StatesBlockSyntax(
                node.StatesKeyword, node.OpenParenToken, node.StateOptions,
                node.CloseParenToken, node.OpenBraceToken, states, node.CloseBraceToken);
        }
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        var returnType = (TypeSyntax?)Visit(node.ReturnType);
        var parameters = VisitSeparatedList(node.Parameters, out bool parametersChanged);
        var body = node.Body != null
            ? (BlockStatementSyntax?)Visit(node.Body)
            : null;

        if (returnType != node.ReturnType ||
            parametersChanged ||
            body != node.Body)
        {
            return new MethodDeclarationSyntax(
                node.Modifiers, returnType ?? node.ReturnType, node.Identifier,
                node.OpenParenToken, parameters, node.CloseParenToken,
                node.ConstKeyword, body, node.SemicolonToken);
        }
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitFieldDeclaration(FieldDeclarationSyntax node)
    {
        var type = (TypeSyntax?)Visit(node.Type);
        var variables = VisitSeparatedList(node.Variables, out bool variablesChanged);

        if (type != node.Type || variablesChanged)
        {
            return new FieldDeclarationSyntax(
                node.Modifiers, type ?? node.Type, variables, node.SemicolonToken);
        }
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitParameter(ParameterSyntax node)
    {
        var type = (TypeSyntax?)Visit(node.Type);
        var defaultValue = node.DefaultValue != null
            ? (ExpressionSyntax?)Visit(node.DefaultValue)
            : null;

        if (type != node.Type || defaultValue != node.DefaultValue)
        {
            return new ParameterSyntax(
                node.Modifiers, type ?? node.Type, node.Identifier,
                node.EqualsToken, defaultValue);
        }
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitConstDeclaration(ConstDeclarationSyntax node)
    {
        var type = (TypeSyntax?)Visit(node.Type);
        var value = (ExpressionSyntax?)Visit(node.Value);

        if (type != node.Type || value != node.Value)
        {
            return new ConstDeclarationSyntax(
                node.ConstKeyword, type ?? node.Type, node.Identifier,
                node.EqualsToken, value ?? node.Value, node.SemicolonToken);
        }
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitStructDeclaration(StructDeclarationSyntax node)
    {
        var members = VisitList(node.Members);

        if (members != node.Members)
        {
            return new StructDeclarationSyntax(
                node.Modifiers, node.StructKeyword, node.Identifier,
                node.OpenBraceToken, members, node.CloseBraceToken);
        }
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitEnumDeclaration(EnumDeclarationSyntax node)
    {
        // Leaf node (Members are tokens, not SyntaxNodes)
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitVariableDeclarator(VariableDeclaratorSyntax node)
    {
        var initializer = node.Initializer != null
            ? (ExpressionSyntax?)Visit(node.Initializer)
            : null;

        if (initializer != node.Initializer)
        {
            return new VariableDeclaratorSyntax(
                node.Identifier, node.EqualsToken, initializer);
        }
        return node;
    }

    // ---------------------------------------------------------------
    // Expression visitors
    // ---------------------------------------------------------------

    /// <inheritdoc />
    public override SyntaxNode? VisitLiteralExpression(LiteralExpressionSyntax node)
    {
        // Leaf node
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitIdentifierExpression(IdentifierExpressionSyntax node)
    {
        // Leaf node
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
    {
        var expression = (ExpressionSyntax?)Visit(node.Expression);

        if (expression != null && expression != node.Expression)
        {
            return new MemberAccessExpressionSyntax(expression, node.DotToken, node.Name);
        }
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        var expression = (ExpressionSyntax?)Visit(node.Expression);
        var arguments = VisitSeparatedList(node.Arguments, out bool argumentsChanged);

        if (expression != node.Expression || argumentsChanged)
        {
            return new InvocationExpressionSyntax(
                expression ?? node.Expression, node.OpenParenToken,
                arguments, node.CloseParenToken);
        }
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitBinaryExpression(BinaryExpressionSyntax node)
    {
        var left = (ExpressionSyntax?)Visit(node.Left);
        var right = (ExpressionSyntax?)Visit(node.Right);

        if (left != node.Left || right != node.Right)
        {
            return new BinaryExpressionSyntax(
                left ?? node.Left, node.OperatorToken, right ?? node.Right);
        }
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitUnaryExpression(UnaryExpressionSyntax node)
    {
        var operand = (ExpressionSyntax?)Visit(node.Operand);

        if (operand != null && operand != node.Operand)
        {
            return new UnaryExpressionSyntax(node.OperatorToken, operand);
        }
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitConditionalExpression(ConditionalExpressionSyntax node)
    {
        var condition = (ExpressionSyntax?)Visit(node.Condition);
        var whenTrue = (ExpressionSyntax?)Visit(node.WhenTrue);
        var whenFalse = (ExpressionSyntax?)Visit(node.WhenFalse);

        if (condition != node.Condition ||
            whenTrue != node.WhenTrue ||
            whenFalse != node.WhenFalse)
        {
            return new ConditionalExpressionSyntax(
                condition ?? node.Condition, node.QuestionToken,
                whenTrue ?? node.WhenTrue, node.ColonToken,
                whenFalse ?? node.WhenFalse);
        }
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitCastExpression(CastExpressionSyntax node)
    {
        var type = (TypeSyntax?)Visit(node.Type);
        var expression = (ExpressionSyntax?)Visit(node.Expression);

        if (type != node.Type || expression != node.Expression)
        {
            return new CastExpressionSyntax(
                node.OpenParenToken, type ?? node.Type,
                node.CloseParenToken, expression ?? node.Expression);
        }
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitArrayAccessExpression(ArrayAccessExpressionSyntax node)
    {
        var expression = (ExpressionSyntax?)Visit(node.Expression);
        var index = (ExpressionSyntax?)Visit(node.Index);

        if (expression != node.Expression || index != node.Index)
        {
            return new ArrayAccessExpressionSyntax(
                expression ?? node.Expression, node.OpenBracketToken,
                index ?? node.Index, node.CloseBracketToken);
        }
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitParenthesizedExpression(ParenthesizedExpressionSyntax node)
    {
        var expression = (ExpressionSyntax?)Visit(node.Expression);

        if (expression != null && expression != node.Expression)
        {
            return new ParenthesizedExpressionSyntax(
                node.OpenParenToken, expression, node.CloseParenToken);
        }
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitArgument(ArgumentSyntax node)
    {
        var expression = (ExpressionSyntax?)Visit(node.Expression);

        if (expression != null && expression != node.Expression)
        {
            return new ArgumentSyntax(node.NameColon, expression);
        }
        return node;
    }

    // ---------------------------------------------------------------
    // Type visitors
    // ---------------------------------------------------------------

    /// <inheritdoc />
    public override SyntaxNode? VisitPredefinedType(PredefinedTypeSyntax node)
    {
        // Leaf node
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitNamedType(NamedTypeSyntax node)
    {
        var typeArguments = node.TypeArguments != null
            ? (TypeArgumentListSyntax?)Visit(node.TypeArguments)
            : null;

        if (typeArguments != node.TypeArguments)
        {
            return new NamedTypeSyntax(node.Identifier, typeArguments);
        }
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitArrayType(ArrayTypeSyntax node)
    {
        var elementType = (TypeSyntax?)Visit(node.ElementType);

        if (elementType != null && elementType != node.ElementType)
        {
            return new ArrayTypeSyntax(
                node.ArrayKeyword, node.LessThanToken,
                elementType, node.GreaterThanToken);
        }
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitMapType(MapTypeSyntax node)
    {
        var keyType = (TypeSyntax?)Visit(node.KeyType);
        var valueType = (TypeSyntax?)Visit(node.ValueType);

        if (keyType != node.KeyType || valueType != node.ValueType)
        {
            return new MapTypeSyntax(
                node.MapKeyword, node.LessThanToken,
                keyType ?? node.KeyType, node.CommaToken,
                valueType ?? node.ValueType, node.GreaterThanToken);
        }
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitClassType(ClassTypeSyntax node)
    {
        var constraintType = (TypeSyntax?)Visit(node.ConstraintType);

        if (constraintType != null && constraintType != node.ConstraintType)
        {
            return new ClassTypeSyntax(
                node.ClassKeyword, node.LessThanToken,
                constraintType, node.GreaterThanToken);
        }
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitTypeArgumentList(TypeArgumentListSyntax node)
    {
        var arguments = VisitSeparatedList(node.Arguments, out bool argumentsChanged);

        if (argumentsChanged)
        {
            return new TypeArgumentListSyntax(
                node.LessThanToken, arguments, node.GreaterThanToken);
        }
        return node;
    }

    // ---------------------------------------------------------------
    // Statement visitors
    // ---------------------------------------------------------------

    /// <inheritdoc />
    public override SyntaxNode? VisitBlockStatement(BlockStatementSyntax node)
    {
        var statements = VisitList(node.Statements);

        if (statements != node.Statements)
        {
            return new BlockStatementSyntax(
                node.OpenBraceToken, statements, node.CloseBraceToken);
        }
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitExpressionStatement(ExpressionStatementSyntax node)
    {
        var expression = (ExpressionSyntax?)Visit(node.Expression);

        if (expression != null && expression != node.Expression)
        {
            return new ExpressionStatementSyntax(expression, node.SemicolonToken);
        }
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitIfStatement(IfStatementSyntax node)
    {
        var condition = (ExpressionSyntax?)Visit(node.Condition);
        var statement = (StatementSyntax?)Visit(node.Statement);
        var elseStatement = node.ElseStatement != null
            ? (StatementSyntax?)Visit(node.ElseStatement)
            : null;

        if (condition != node.Condition ||
            statement != node.Statement ||
            elseStatement != node.ElseStatement)
        {
            return new IfStatementSyntax(
                node.IfKeyword, node.OpenParenToken,
                condition ?? node.Condition, node.CloseParenToken,
                statement ?? node.Statement,
                node.ElseKeyword, elseStatement);
        }
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitWhileStatement(WhileStatementSyntax node)
    {
        var condition = (ExpressionSyntax?)Visit(node.Condition);
        var body = (StatementSyntax?)Visit(node.Body);

        if (condition != node.Condition || body != node.Body)
        {
            return new WhileStatementSyntax(
                node.WhileKeyword, node.OpenParenToken,
                condition ?? node.Condition, node.CloseParenToken,
                body ?? node.Body);
        }
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitDoWhileStatement(DoWhileStatementSyntax node)
    {
        var body = (StatementSyntax?)Visit(node.Body);
        var condition = (ExpressionSyntax?)Visit(node.Condition);

        if (body != node.Body || condition != node.Condition)
        {
            return new DoWhileStatementSyntax(
                node.DoKeyword, body ?? node.Body, node.WhileKeyword,
                node.OpenParenToken, condition ?? node.Condition,
                node.CloseParenToken, node.SemicolonToken);
        }
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitForStatement(ForStatementSyntax node)
    {
        var initializer = node.Initializer != null
            ? (StatementSyntax?)Visit(node.Initializer)
            : null;
        var condition = node.Condition != null
            ? (ExpressionSyntax?)Visit(node.Condition)
            : null;
        var incrementor = node.Incrementor != null
            ? (ExpressionSyntax?)Visit(node.Incrementor)
            : null;
        var body = (StatementSyntax?)Visit(node.Body);

        if (initializer != node.Initializer ||
            condition != node.Condition ||
            incrementor != node.Incrementor ||
            body != node.Body)
        {
            return new ForStatementSyntax(
                node.ForKeyword, node.OpenParenToken,
                initializer, condition, node.SemicolonToken,
                incrementor, node.CloseParenToken,
                body ?? node.Body);
        }
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitForEachStatement(ForEachStatementSyntax node)
    {
        var type = (TypeSyntax?)Visit(node.Type);
        var expression = (ExpressionSyntax?)Visit(node.Expression);
        var body = (StatementSyntax?)Visit(node.Body);

        if (type != node.Type ||
            expression != node.Expression ||
            body != node.Body)
        {
            return new ForEachStatementSyntax(
                node.ForEachKeyword, node.OpenParenToken,
                type ?? node.Type, node.Identifier, node.InKeyword,
                expression ?? node.Expression, node.CloseParenToken,
                body ?? node.Body);
        }
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitSwitchStatement(SwitchStatementSyntax node)
    {
        var expression = (ExpressionSyntax?)Visit(node.Expression);
        var sections = VisitList(node.Sections);

        if (expression != node.Expression || sections != node.Sections)
        {
            return new SwitchStatementSyntax(
                node.SwitchKeyword, node.OpenParenToken,
                expression ?? node.Expression, node.CloseParenToken,
                node.OpenBraceToken, sections, node.CloseBraceToken);
        }
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitCaseLabel(CaseLabelSyntax node)
    {
        var value = (ExpressionSyntax?)Visit(node.Value);

        if (value != null && value != node.Value)
        {
            return new CaseLabelSyntax(node.CaseKeyword, value, node.ColonToken);
        }
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitDefaultLabel(DefaultLabelSyntax node)
    {
        // Leaf node
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitReturnStatement(ReturnStatementSyntax node)
    {
        var expression = node.Expression != null
            ? (ExpressionSyntax?)Visit(node.Expression)
            : null;

        if (expression != node.Expression)
        {
            return new ReturnStatementSyntax(
                node.ReturnKeyword, expression, node.SemicolonToken);
        }
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitBreakStatement(BreakStatementSyntax node)
    {
        // Leaf node
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitContinueStatement(ContinueStatementSyntax node)
    {
        // Leaf node
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
    {
        var type = node.Type != null
            ? (TypeSyntax?)Visit(node.Type)
            : null;
        var variables = VisitSeparatedList(node.Variables, out bool variablesChanged);

        if (type != node.Type || variablesChanged)
        {
            return new LocalDeclarationStatementSyntax(
                node.LetKeyword, type, variables, node.SemicolonToken);
        }
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitAssignmentStatement(AssignmentStatementSyntax node)
    {
        var target = (ExpressionSyntax?)Visit(node.Target);
        var value = (ExpressionSyntax?)Visit(node.Value);

        if (target != node.Target || value != node.Value)
        {
            return new AssignmentStatementSyntax(
                target ?? node.Target, node.OperatorToken,
                value ?? node.Value, node.SemicolonToken);
        }
        return node;
    }

    // ---------------------------------------------------------------
    // State visitors
    // ---------------------------------------------------------------

    /// <inheritdoc />
    public override SyntaxNode? VisitStateLabel(StateLabelSyntax node)
    {
        // Leaf node
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitStateFrame(StateFrameSyntax node)
    {
        var action = node.Action != null
            ? (StateActionSyntax?)Visit(node.Action)
            : null;

        if (action != node.Action)
        {
            return new StateFrameSyntax(
                node.SpriteIdentifier, node.FrameLetters,
                node.Duration, node.Modifiers, action);
        }
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitStateAction(StateActionSyntax node)
    {
        var arguments = VisitList(node.Arguments);
        var actionBlock = node.ActionBlock != null
            ? (BlockStatementSyntax?)Visit(node.ActionBlock)
            : null;

        if (arguments != node.Arguments || actionBlock != node.ActionBlock)
        {
            return new StateActionSyntax(
                node.ActionIdentifier, node.OpenParenToken,
                arguments, node.CloseParenToken, actionBlock);
        }
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitStateGoto(StateGotoSyntax node)
    {
        // Leaf node (no child SyntaxNodes)
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitStateStop(StateStopSyntax node)
    {
        // Leaf node
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitStateWait(StateWaitSyntax node)
    {
        // Leaf node
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitStateLoop(StateLoopSyntax node)
    {
        // Leaf node
        return node;
    }

    /// <inheritdoc />
    public override SyntaxNode? VisitStateFail(StateFailSyntax node)
    {
        // Leaf node
        return node;
    }
}
