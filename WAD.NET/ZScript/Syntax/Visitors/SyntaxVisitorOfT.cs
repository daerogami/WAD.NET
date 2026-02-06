namespace WAD.NET.ZScript.Syntax;

/// <summary>
/// Base class for syntax visitors that return a value of type <typeparamref name="TResult"/>.
/// </summary>
/// <typeparam name="TResult">The return type of the visit operation.</typeparam>
public abstract class SyntaxVisitor<TResult>
{
    /// <summary>
    /// Visits the specified node by dispatching to its Accept method.
    /// </summary>
    /// <param name="node">The node to visit, or <see langword="null"/>.</param>
    /// <returns>The result of visiting the node, or <see langword="default"/> if the node is null.</returns>
    public virtual TResult? Visit(SyntaxNode? node)
    {
        return node != null ? node.Accept(this) : default;
    }

    /// <summary>
    /// Called when no specific Visit method is overridden for a node kind.
    /// Returns <see langword="default"/> by default.
    /// </summary>
    /// <param name="node">The node being visited.</param>
    /// <returns>The default value of <typeparamref name="TResult"/>.</returns>
    public virtual TResult? DefaultVisit(SyntaxNode node) => default;

    // Declaration visitors
    public virtual TResult? VisitCompilationUnit(CompilationUnitSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitVersionDirective(VersionDirectiveSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitIncludeDirective(IncludeDirectiveSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitClassDeclaration(ClassDeclarationSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitActorDeclaration(ActorDeclarationSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitBaseList(BaseListSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitDefaultBlock(DefaultBlockSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitPropertyAssignment(PropertyAssignmentSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitFlagDefinition(FlagDefinitionSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitStatesBlock(StatesBlockSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitMethodDeclaration(MethodDeclarationSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitFieldDeclaration(FieldDeclarationSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitParameter(ParameterSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitConstDeclaration(ConstDeclarationSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitStructDeclaration(StructDeclarationSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitEnumDeclaration(EnumDeclarationSyntax node) => DefaultVisit(node);

    // Expression visitors
    public virtual TResult? VisitLiteralExpression(LiteralExpressionSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitIdentifierExpression(IdentifierExpressionSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitMemberAccessExpression(MemberAccessExpressionSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitInvocationExpression(InvocationExpressionSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitBinaryExpression(BinaryExpressionSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitUnaryExpression(UnaryExpressionSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitConditionalExpression(ConditionalExpressionSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitCastExpression(CastExpressionSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitArrayAccessExpression(ArrayAccessExpressionSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitParenthesizedExpression(ParenthesizedExpressionSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitArgument(ArgumentSyntax node) => DefaultVisit(node);

    // Type visitors
    public virtual TResult? VisitPredefinedType(PredefinedTypeSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitNamedType(NamedTypeSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitArrayType(ArrayTypeSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitMapType(MapTypeSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitClassType(ClassTypeSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitTypeArgumentList(TypeArgumentListSyntax node) => DefaultVisit(node);

    // Statement visitors
    public virtual TResult? VisitBlockStatement(BlockStatementSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitExpressionStatement(ExpressionStatementSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitIfStatement(IfStatementSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitWhileStatement(WhileStatementSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitDoWhileStatement(DoWhileStatementSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitForStatement(ForStatementSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitForEachStatement(ForEachStatementSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitSwitchStatement(SwitchStatementSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitCaseLabel(CaseLabelSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitDefaultLabel(DefaultLabelSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitReturnStatement(ReturnStatementSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitBreakStatement(BreakStatementSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitContinueStatement(ContinueStatementSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitAssignmentStatement(AssignmentStatementSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitVariableDeclarator(VariableDeclaratorSyntax node) => DefaultVisit(node);

    // State visitors
    public virtual TResult? VisitStateLabel(StateLabelSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitStateFrame(StateFrameSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitStateAction(StateActionSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitStateGoto(StateGotoSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitStateStop(StateStopSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitStateWait(StateWaitSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitStateLoop(StateLoopSyntax node) => DefaultVisit(node);
    public virtual TResult? VisitStateFail(StateFailSyntax node) => DefaultVisit(node);
}
