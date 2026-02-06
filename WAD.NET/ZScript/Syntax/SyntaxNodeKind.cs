namespace WAD.NET.ZScript.Syntax;

/// <summary>
/// Defines all syntax node kinds in the ZScript/DECORATE syntax tree.
/// </summary>
public enum SyntaxNodeKind
{
    CompilationUnit,
    VersionDirective,
    IncludeDirective,

    // Declarations
    ClassDeclaration,
    ActorDeclaration,
    StructDeclaration,
    EnumDeclaration,
    ConstDeclaration,

    // Members
    FieldDeclaration,
    PropertyDeclaration,
    MethodDeclaration,
    DefaultBlock,
    StatesBlock,
    FlagDefinition,

    // Default block items
    PropertyAssignment,
    BaseList,

    // States
    StateLabel,
    StateFrame,
    StateAction,
    StateGoto,
    StateStop,
    StateWait,
    StateLoop,
    StateFail,

    // Types
    PredefinedType,
    NamedType,
    ArrayType,
    MapType,
    ClassType,
    TypeArgumentList,

    // Expressions
    LiteralExpression,
    IdentifierExpression,
    MemberAccessExpression,
    InvocationExpression,
    BinaryExpression,
    UnaryExpression,
    ConditionalExpression,
    CastExpression,
    ArrayAccessExpression,
    ParenthesizedExpression,

    // Statements
    BlockStatement,
    ExpressionStatement,
    IfStatement,
    WhileStatement,
    DoWhileStatement,
    ForStatement,
    ForEachStatement,
    SwitchStatement,
    CaseLabel,
    DefaultLabel,
    ReturnStatement,
    BreakStatement,
    ContinueStatement,
    LocalDeclarationStatement,
    AssignmentStatement,

    // Parameters
    Parameter,
    Argument,
    VariableDeclarator
}
