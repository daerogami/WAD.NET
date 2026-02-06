namespace WAD.NET.ZScript.Syntax;

/// <summary>
/// Base class for syntax visitors that do not return a value.
/// </summary>
public abstract class SyntaxVisitor
{
    /// <summary>
    /// Visits the specified node by dispatching to its Accept method.
    /// </summary>
    /// <param name="node">The node to visit, or <see langword="null"/>.</param>
    public virtual void Visit(SyntaxNode? node)
    {
        node?.Accept(this);
    }

    /// <summary>
    /// Called when no specific Visit method is overridden for a node kind.
    /// Visits all child nodes by default.
    /// </summary>
    /// <param name="node">The node being visited.</param>
    public virtual void DefaultVisit(SyntaxNode node)
    {
        foreach (var child in node.ChildNodes())
            Visit(child);
    }

    // Declaration visitors
    public virtual void VisitCompilationUnit(CompilationUnitSyntax node) => DefaultVisit(node);
    public virtual void VisitVersionDirective(VersionDirectiveSyntax node) => DefaultVisit(node);
    public virtual void VisitIncludeDirective(IncludeDirectiveSyntax node) => DefaultVisit(node);
    public virtual void VisitClassDeclaration(ClassDeclarationSyntax node) => DefaultVisit(node);
    public virtual void VisitActorDeclaration(ActorDeclarationSyntax node) => DefaultVisit(node);
    public virtual void VisitBaseList(BaseListSyntax node) => DefaultVisit(node);
    public virtual void VisitDefaultBlock(DefaultBlockSyntax node) => DefaultVisit(node);
    public virtual void VisitPropertyAssignment(PropertyAssignmentSyntax node) => DefaultVisit(node);
    public virtual void VisitFlagDefinition(FlagDefinitionSyntax node) => DefaultVisit(node);
    public virtual void VisitStatesBlock(StatesBlockSyntax node) => DefaultVisit(node);
    public virtual void VisitMethodDeclaration(MethodDeclarationSyntax node) => DefaultVisit(node);
    public virtual void VisitFieldDeclaration(FieldDeclarationSyntax node) => DefaultVisit(node);
    public virtual void VisitParameter(ParameterSyntax node) => DefaultVisit(node);
    public virtual void VisitConstDeclaration(ConstDeclarationSyntax node) => DefaultVisit(node);
    public virtual void VisitStructDeclaration(StructDeclarationSyntax node) => DefaultVisit(node);
    public virtual void VisitEnumDeclaration(EnumDeclarationSyntax node) => DefaultVisit(node);

    // Expression visitors
    public virtual void VisitLiteralExpression(LiteralExpressionSyntax node) => DefaultVisit(node);
    public virtual void VisitIdentifierExpression(IdentifierExpressionSyntax node) => DefaultVisit(node);
    public virtual void VisitMemberAccessExpression(MemberAccessExpressionSyntax node) => DefaultVisit(node);
    public virtual void VisitInvocationExpression(InvocationExpressionSyntax node) => DefaultVisit(node);
    public virtual void VisitBinaryExpression(BinaryExpressionSyntax node) => DefaultVisit(node);
    public virtual void VisitUnaryExpression(UnaryExpressionSyntax node) => DefaultVisit(node);
    public virtual void VisitConditionalExpression(ConditionalExpressionSyntax node) => DefaultVisit(node);
    public virtual void VisitCastExpression(CastExpressionSyntax node) => DefaultVisit(node);
    public virtual void VisitArrayAccessExpression(ArrayAccessExpressionSyntax node) => DefaultVisit(node);
    public virtual void VisitParenthesizedExpression(ParenthesizedExpressionSyntax node) => DefaultVisit(node);
    public virtual void VisitArgument(ArgumentSyntax node) => DefaultVisit(node);

    // Type visitors
    public virtual void VisitPredefinedType(PredefinedTypeSyntax node) => DefaultVisit(node);
    public virtual void VisitNamedType(NamedTypeSyntax node) => DefaultVisit(node);
    public virtual void VisitArrayType(ArrayTypeSyntax node) => DefaultVisit(node);
    public virtual void VisitMapType(MapTypeSyntax node) => DefaultVisit(node);
    public virtual void VisitClassType(ClassTypeSyntax node) => DefaultVisit(node);
    public virtual void VisitTypeArgumentList(TypeArgumentListSyntax node) => DefaultVisit(node);

    // Statement visitors
    public virtual void VisitBlockStatement(BlockStatementSyntax node) => DefaultVisit(node);
    public virtual void VisitExpressionStatement(ExpressionStatementSyntax node) => DefaultVisit(node);
    public virtual void VisitIfStatement(IfStatementSyntax node) => DefaultVisit(node);
    public virtual void VisitWhileStatement(WhileStatementSyntax node) => DefaultVisit(node);
    public virtual void VisitDoWhileStatement(DoWhileStatementSyntax node) => DefaultVisit(node);
    public virtual void VisitForStatement(ForStatementSyntax node) => DefaultVisit(node);
    public virtual void VisitForEachStatement(ForEachStatementSyntax node) => DefaultVisit(node);
    public virtual void VisitSwitchStatement(SwitchStatementSyntax node) => DefaultVisit(node);
    public virtual void VisitCaseLabel(CaseLabelSyntax node) => DefaultVisit(node);
    public virtual void VisitDefaultLabel(DefaultLabelSyntax node) => DefaultVisit(node);
    public virtual void VisitReturnStatement(ReturnStatementSyntax node) => DefaultVisit(node);
    public virtual void VisitBreakStatement(BreakStatementSyntax node) => DefaultVisit(node);
    public virtual void VisitContinueStatement(ContinueStatementSyntax node) => DefaultVisit(node);
    public virtual void VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node) => DefaultVisit(node);
    public virtual void VisitAssignmentStatement(AssignmentStatementSyntax node) => DefaultVisit(node);
    public virtual void VisitVariableDeclarator(VariableDeclaratorSyntax node) => DefaultVisit(node);

    // State visitors
    public virtual void VisitStateLabel(StateLabelSyntax node) => DefaultVisit(node);
    public virtual void VisitStateFrame(StateFrameSyntax node) => DefaultVisit(node);
    public virtual void VisitStateAction(StateActionSyntax node) => DefaultVisit(node);
    public virtual void VisitStateGoto(StateGotoSyntax node) => DefaultVisit(node);
    public virtual void VisitStateStop(StateStopSyntax node) => DefaultVisit(node);
    public virtual void VisitStateWait(StateWaitSyntax node) => DefaultVisit(node);
    public virtual void VisitStateLoop(StateLoopSyntax node) => DefaultVisit(node);
    public virtual void VisitStateFail(StateFailSyntax node) => DefaultVisit(node);
}
