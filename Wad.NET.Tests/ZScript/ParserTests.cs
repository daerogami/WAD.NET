using System.Linq;
using WAD.NET.ZScript;
using WAD.NET.ZScript.Syntax;
using Xunit;

namespace WAD.NET.Tests.ZScript;

public class ParserTests
{
    // ------------------------------------------------------------------
    // Empty compilation unit
    // ------------------------------------------------------------------

    [Fact]
    public void ShouldParseEmptyCompilationUnit()
    {
        var parser = new Parser("", ScriptLanguage.ZScript);
        var unit = parser.ParseCompilationUnit();
        Assert.Empty(parser.Diagnostics);
        Assert.Null(unit.VersionDirective);
        Assert.Empty(unit.Includes);
        Assert.Empty(unit.Members);
    }

    // ------------------------------------------------------------------
    // Version directive
    // ------------------------------------------------------------------

    [Fact]
    public void ShouldParseVersionDirective()
    {
        var parser = new Parser("version \"4.10.0\"");
        var unit = parser.ParseCompilationUnit();
        Assert.Empty(parser.Diagnostics);
        Assert.NotNull(unit.VersionDirective);
        Assert.Equal("version", unit.VersionDirective!.VersionKeyword.Text);
        Assert.Equal("\"4.10.0\"", unit.VersionDirective.VersionString.Text);
    }

    // ------------------------------------------------------------------
    // Include directive
    // ------------------------------------------------------------------

    [Fact]
    public void ShouldParseIncludeDirective()
    {
        var parser = new Parser("#include \"zscript/weapons.zs\"");
        var unit = parser.ParseCompilationUnit();
        Assert.Empty(parser.Diagnostics);
        Assert.Single(unit.Includes);
        Assert.Equal("#", unit.Includes[0].HashToken.Text);
        Assert.Equal("include", unit.Includes[0].IncludeKeyword.Text);
        Assert.Equal("\"zscript/weapons.zs\"", unit.Includes[0].PathString.Text);
    }

    // ------------------------------------------------------------------
    // Simple class
    // ------------------------------------------------------------------

    [Fact]
    public void ShouldParseSimpleClass()
    {
        var parser = new Parser("class Demon : Actor { }");
        var unit = parser.ParseCompilationUnit();
        Assert.Empty(parser.Diagnostics);
        Assert.Single(unit.Members);
        var cls = Assert.IsType<ClassDeclarationSyntax>(unit.Members[0]);
        Assert.Equal("Demon", cls.Name);
        Assert.NotNull(cls.BaseList);
        Assert.Equal("Actor", cls.BaseClassName);
    }

    [Fact]
    public void ShouldParseClassWithEmptyBody()
    {
        var parser = new Parser("class Foo : Bar { }");
        var unit = parser.ParseCompilationUnit();
        Assert.Empty(parser.Diagnostics);
        var cls = Assert.IsType<ClassDeclarationSyntax>(unit.Members[0]);
        Assert.Equal("Foo", cls.Name);
        Assert.Equal("Bar", cls.BaseClassName);
        Assert.Empty(cls.Members);
    }

    // ------------------------------------------------------------------
    // Class with Default block
    // ------------------------------------------------------------------

    [Fact]
    public void ShouldParseClassWithDefaultBlock()
    {
        var source = @"class MyMonster : Actor
{
    Default
    {
        Health 100;
        +SOLID;
        -NOGRAVITY;
    }
}";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        Assert.Empty(parser.Diagnostics);
        var cls = Assert.IsType<ClassDeclarationSyntax>(unit.Members[0]);
        Assert.NotNull(cls.DefaultBlock);
        var defaults = cls.DefaultBlock!;
        Assert.Equal(3, defaults.Items.Length);

        var prop = Assert.IsType<PropertyAssignmentSyntax>(defaults.Items[0]);
        Assert.Equal("Health", prop.PropertyName.Text);
        Assert.Single(prop.Values);

        var flag1 = Assert.IsType<FlagDefinitionSyntax>(defaults.Items[1]);
        Assert.True(flag1.IsSet);
        Assert.Equal("SOLID", flag1.FlagName.Text);

        var flag2 = Assert.IsType<FlagDefinitionSyntax>(defaults.Items[2]);
        Assert.False(flag2.IsSet);
        Assert.Equal("NOGRAVITY", flag2.FlagName.Text);
    }

    // ------------------------------------------------------------------
    // Class with States block
    // ------------------------------------------------------------------

    [Fact]
    public void ShouldParseClassWithStatesBlock()
    {
        var source = @"class MyMonster : Actor
{
    States
    {
    Spawn:
        POSS AB 4
        Loop
    See:
        POSS AABBCCDD 4
        Loop
    }
}";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        Assert.Empty(parser.Diagnostics);
        var cls = Assert.IsType<ClassDeclarationSyntax>(unit.Members[0]);
        Assert.NotNull(cls.StatesBlock);
        var states = cls.StatesBlock!;

        // Spawn:, frame, Loop, See:, frame, Loop
        Assert.Equal(6, states.States.Length);

        var label1 = Assert.IsType<StateLabelSyntax>(states.States[0]);
        Assert.Equal("Spawn", label1.LabelName);

        var frame1 = Assert.IsType<StateFrameSyntax>(states.States[1]);
        Assert.Equal("POSS", frame1.SpriteName);
        Assert.Equal("AB", frame1.Frames);

        Assert.IsType<StateLoopSyntax>(states.States[2]);

        var label2 = Assert.IsType<StateLabelSyntax>(states.States[3]);
        Assert.Equal("See", label2.LabelName);
    }

    // ------------------------------------------------------------------
    // Class with methods
    // ------------------------------------------------------------------

    [Fact]
    public void ShouldParseClassWithMethod()
    {
        var source = @"class MyActor : Actor
{
    void DoSomething()
    {
        return;
    }
}";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        Assert.Empty(parser.Diagnostics);
        var cls = Assert.IsType<ClassDeclarationSyntax>(unit.Members[0]);
        Assert.Single(cls.Methods);
        var method = cls.Methods.First();
        Assert.Equal("DoSomething", method.Name);
        Assert.NotNull(method.Body);
    }

    [Fact]
    public void ShouldParseNativeMethod()
    {
        var source = @"class Foo : Bar
{
    native void Tick();
}";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        Assert.Empty(parser.Diagnostics);
        var cls = Assert.IsType<ClassDeclarationSyntax>(unit.Members[0]);
        Assert.Single(cls.Methods);
        var method = cls.Methods.First();
        Assert.Equal("Tick", method.Name);
        Assert.True(method.IsNative);
        Assert.Null(method.Body);
    }

    // ------------------------------------------------------------------
    // Field declarations
    // ------------------------------------------------------------------

    [Fact]
    public void ShouldParseFieldDeclaration()
    {
        var source = @"class Foo : Bar
{
    int health;
}";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        Assert.Empty(parser.Diagnostics);
        var cls = Assert.IsType<ClassDeclarationSyntax>(unit.Members[0]);
        Assert.Single(cls.Fields);
        var field = cls.Fields.First();
        Assert.Equal("health", field.Variables[0].Identifier.Text);
    }

    // ------------------------------------------------------------------
    // DECORATE actor
    // ------------------------------------------------------------------

    [Fact]
    public void ShouldParseSimpleActor()
    {
        var source = @"actor MyZombie : ZombieMan replaces ZombieMan 1001
{
    Health 50;
    +ISMONSTER;
    States
    {
    Spawn:
        POSS AB 4
        Loop
    }
}";
        var parser = new Parser(source, ScriptLanguage.Decorate);
        var unit = parser.ParseCompilationUnit();
        Assert.Empty(parser.Diagnostics);
        Assert.Single(unit.Members);
        var actor = Assert.IsType<ActorDeclarationSyntax>(unit.Members[0]);
        Assert.Equal("MyZombie", actor.Name);
        Assert.Equal("ZombieMan", actor.BaseName);
        Assert.Equal("ZombieMan", actor.ReplacesName);
        Assert.Equal(1001, actor.DoomEdNumber);
    }

    [Fact]
    public void ShouldParseActorWithFullBody()
    {
        var source = @"actor TestActor : Actor 5001
{
    Health 200;
    Radius 20;
    Height 56;
    +SOLID;
    +SHOOTABLE;
    -NOGRAVITY;
    States
    {
    Spawn:
        PLAY A 10
        Loop
    Death:
        PLAY H 5
        PLAY I 5
        PLAY J 5
        PLAY K -1
        Stop
    }
}";
        var parser = new Parser(source, ScriptLanguage.Decorate);
        var unit = parser.ParseCompilationUnit();
        Assert.Empty(parser.Diagnostics);
        var actor = Assert.IsType<ActorDeclarationSyntax>(unit.Members[0]);
        Assert.Equal("TestActor", actor.Name);
        Assert.Equal(5001, actor.DoomEdNumber);
    }

    // ------------------------------------------------------------------
    // Expressions
    // ------------------------------------------------------------------

    [Fact]
    public void ShouldParseBinaryExpression()
    {
        var source = @"class Foo : Bar
{
    void Test()
    {
        x + y;
    }
}";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        Assert.Empty(parser.Diagnostics);
        var cls = Assert.IsType<ClassDeclarationSyntax>(unit.Members[0]);
        var method = cls.Methods.First();
        Assert.NotNull(method.Body);
        var exprStmt = Assert.IsType<ExpressionStatementSyntax>(method.Body!.Statements[0]);
        var binary = Assert.IsType<BinaryExpressionSyntax>(exprStmt.Expression);
        Assert.Equal(SyntaxTokenKind.Plus, binary.OperatorToken.Kind);
    }

    [Fact]
    public void ShouldParseUnaryExpression()
    {
        var source = @"class Foo : Bar
{
    void Test()
    {
        -x;
    }
}";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        Assert.Empty(parser.Diagnostics);
        var cls = Assert.IsType<ClassDeclarationSyntax>(unit.Members[0]);
        var method = cls.Methods.First();
        var exprStmt = Assert.IsType<ExpressionStatementSyntax>(method.Body!.Statements[0]);
        var unary = Assert.IsType<UnaryExpressionSyntax>(exprStmt.Expression);
        Assert.Equal(SyntaxTokenKind.Minus, unary.OperatorToken.Kind);
    }

    [Fact]
    public void ShouldParseMemberAccessExpression()
    {
        var source = @"class Foo : Bar
{
    void Test()
    {
        self.health;
    }
}";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        Assert.Empty(parser.Diagnostics);
        var cls = Assert.IsType<ClassDeclarationSyntax>(unit.Members[0]);
        var method = cls.Methods.First();
        var exprStmt = Assert.IsType<ExpressionStatementSyntax>(method.Body!.Statements[0]);
        var memberAccess = Assert.IsType<MemberAccessExpressionSyntax>(exprStmt.Expression);
        Assert.Equal("health", memberAccess.Name.Text);
    }

    [Fact]
    public void ShouldParseInvocationExpression()
    {
        var source = @"class Foo : Bar
{
    void Test()
    {
        A_FireBullets(0, 0, 1, 5);
    }
}";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        Assert.Empty(parser.Diagnostics);
        var cls = Assert.IsType<ClassDeclarationSyntax>(unit.Members[0]);
        var method = cls.Methods.First();
        var exprStmt = Assert.IsType<ExpressionStatementSyntax>(method.Body!.Statements[0]);
        var invocation = Assert.IsType<InvocationExpressionSyntax>(exprStmt.Expression);
        Assert.Equal(4, invocation.Arguments.Count);
    }

    [Fact]
    public void ShouldParseArrayAccessExpression()
    {
        var source = @"class Foo : Bar
{
    void Test()
    {
        items[0];
    }
}";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        Assert.Empty(parser.Diagnostics);
        var cls = Assert.IsType<ClassDeclarationSyntax>(unit.Members[0]);
        var method = cls.Methods.First();
        var exprStmt = Assert.IsType<ExpressionStatementSyntax>(method.Body!.Statements[0]);
        var arrayAccess = Assert.IsType<ArrayAccessExpressionSyntax>(exprStmt.Expression);
        Assert.IsType<IdentifierExpressionSyntax>(arrayAccess.Expression);
    }

    [Fact]
    public void ShouldParseNestedExpressionsWithPrecedence()
    {
        var source = @"class Foo : Bar
{
    void Test()
    {
        a + b * c;
    }
}";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        Assert.Empty(parser.Diagnostics);
        var cls = Assert.IsType<ClassDeclarationSyntax>(unit.Members[0]);
        var method = cls.Methods.First();
        var exprStmt = Assert.IsType<ExpressionStatementSyntax>(method.Body!.Statements[0]);
        // a + (b * c) - multiplication has higher precedence
        var plus = Assert.IsType<BinaryExpressionSyntax>(exprStmt.Expression);
        Assert.Equal(SyntaxTokenKind.Plus, plus.OperatorToken.Kind);
        Assert.IsType<IdentifierExpressionSyntax>(plus.Left);
        var mult = Assert.IsType<BinaryExpressionSyntax>(plus.Right);
        Assert.Equal(SyntaxTokenKind.Asterisk, mult.OperatorToken.Kind);
    }

    // ------------------------------------------------------------------
    // Statements
    // ------------------------------------------------------------------

    [Fact]
    public void ShouldParseIfElseStatement()
    {
        var source = @"class Foo : Bar
{
    void Test()
    {
        if (x)
        {
            y;
        }
        else
        {
            z;
        }
    }
}";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        Assert.Empty(parser.Diagnostics);
        var cls = Assert.IsType<ClassDeclarationSyntax>(unit.Members[0]);
        var method = cls.Methods.First();
        var ifStmt = Assert.IsType<IfStatementSyntax>(method.Body!.Statements[0]);
        Assert.NotNull(ifStmt.ElseStatement);
    }

    [Fact]
    public void ShouldParseWhileStatement()
    {
        var source = @"class Foo : Bar
{
    void Test()
    {
        while (x) { y; }
    }
}";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        Assert.Empty(parser.Diagnostics);
        var cls = Assert.IsType<ClassDeclarationSyntax>(unit.Members[0]);
        var method = cls.Methods.First();
        Assert.IsType<WhileStatementSyntax>(method.Body!.Statements[0]);
    }

    [Fact]
    public void ShouldParseForStatement()
    {
        var source = @"class Foo : Bar
{
    void Test()
    {
        for (i = 0; i < 10; i = i + 1) { x; }
    }
}";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        Assert.Empty(parser.Diagnostics);
        var cls = Assert.IsType<ClassDeclarationSyntax>(unit.Members[0]);
        var method = cls.Methods.First();
        var forStmt = Assert.IsType<ForStatementSyntax>(method.Body!.Statements[0]);
        Assert.NotNull(forStmt.Initializer);
        Assert.NotNull(forStmt.Condition);
        Assert.NotNull(forStmt.Incrementor);
    }

    [Fact]
    public void ShouldParseReturnStatement()
    {
        var source = @"class Foo : Bar
{
    int GetValue()
    {
        return 42;
    }
}";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        Assert.Empty(parser.Diagnostics);
        var cls = Assert.IsType<ClassDeclarationSyntax>(unit.Members[0]);
        var method = cls.Methods.First();
        var returnStmt = Assert.IsType<ReturnStatementSyntax>(method.Body!.Statements[0]);
        Assert.NotNull(returnStmt.Expression);
    }

    [Fact]
    public void ShouldParseBlockStatement()
    {
        var source = @"class Foo : Bar
{
    void Test()
    {
        { x; y; }
    }
}";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        Assert.Empty(parser.Diagnostics);
        var cls = Assert.IsType<ClassDeclarationSyntax>(unit.Members[0]);
        var method = cls.Methods.First();
        var block = Assert.IsType<BlockStatementSyntax>(method.Body!.Statements[0]);
        Assert.Equal(2, block.Statements.Length);
    }

    // ------------------------------------------------------------------
    // State modifiers
    // ------------------------------------------------------------------

    [Fact]
    public void ShouldParseStateFrameWithBrightModifier()
    {
        var source = @"class Foo : Bar
{
    States
    {
    Spawn:
        POSS A 4 Bright
        Loop
    }
}";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        Assert.Empty(parser.Diagnostics);
        var cls = Assert.IsType<ClassDeclarationSyntax>(unit.Members[0]);
        var frame = cls.StatesBlock!.Frames.First();
        Assert.True(frame.IsBright);
    }

    [Fact]
    public void ShouldParseStateActionWithArguments()
    {
        var source = @"class Foo : Bar
{
    States
    {
    Fire:
        PISG A 1 A_FireBullets(0, 0, 1, 5)
        Stop
    }
}";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        Assert.Empty(parser.Diagnostics);
        var cls = Assert.IsType<ClassDeclarationSyntax>(unit.Members[0]);
        var frame = cls.StatesBlock!.Frames.First();
        Assert.NotNull(frame.Action);
        Assert.True(frame.Action!.ActionIdentifier.HasValue);
        Assert.Equal("A_FireBullets", frame.Action.ActionIdentifier!.Value.Text);
        Assert.Equal(4, frame.Action.Arguments.Length);
    }

    [Fact]
    public void ShouldParseStateGotoWithClassPrefix()
    {
        var source = @"class Foo : Bar
{
    States
    {
    Spawn:
        TNT1 A 1
        Goto Super::See
    }
}";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        Assert.Empty(parser.Diagnostics);
        var cls = Assert.IsType<ClassDeclarationSyntax>(unit.Members[0]);
        var gotoState = cls.StatesBlock!.States.OfType<StateGotoSyntax>().First();
        Assert.Equal("Super::See", gotoState.TargetLabel);
    }

    [Fact]
    public void ShouldParseStateGotoWithOffset()
    {
        var source = @"class Foo : Bar
{
    States
    {
    Spawn:
        TNT1 A 1
        Goto See+2
    }
}";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        Assert.Empty(parser.Diagnostics);
        var cls = Assert.IsType<ClassDeclarationSyntax>(unit.Members[0]);
        var gotoState = cls.StatesBlock!.States.OfType<StateGotoSyntax>().First();
        Assert.Equal("See", gotoState.TargetLabel);
        Assert.Equal(2, gotoState.FrameOffset);
    }

    // ------------------------------------------------------------------
    // Error recovery
    // ------------------------------------------------------------------

    [Fact]
    public void ShouldReportDiagnosticForMissingSemicolon()
    {
        var source = @"class Foo : Bar
{
    int health
}";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        Assert.NotEmpty(parser.Diagnostics);
    }

    [Fact]
    public void ShouldReportDiagnosticForMissingCloseBrace()
    {
        var source = "class Foo : Bar {";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        Assert.NotEmpty(parser.Diagnostics);
    }

    // ------------------------------------------------------------------
    // Multiple declarations
    // ------------------------------------------------------------------

    [Fact]
    public void ShouldParseMultipleClassesInOneUnit()
    {
        var source = @"class Foo : Bar { }
class Baz : Bar { }";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        Assert.Empty(parser.Diagnostics);
        Assert.Equal(2, unit.Members.Length);
        Assert.IsType<ClassDeclarationSyntax>(unit.Members[0]);
        Assert.IsType<ClassDeclarationSyntax>(unit.Members[1]);
    }

    [Fact]
    public void ShouldParseMultipleActorsInOneUnit()
    {
        var source = @"actor Foo { }
actor Bar { }";
        var parser = new Parser(source, ScriptLanguage.Decorate);
        var unit = parser.ParseCompilationUnit();
        Assert.Empty(parser.Diagnostics);
        Assert.Equal(2, unit.Members.Length);
    }

    // ------------------------------------------------------------------
    // State sub-labels
    // ------------------------------------------------------------------

    [Fact]
    public void ShouldParseStateLabelWithSubLabel()
    {
        var source = @"class Foo : Bar
{
    States
    {
    See.Fast:
        POSS A 2
        Loop
    }
}";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        Assert.Empty(parser.Diagnostics);
        var cls = Assert.IsType<ClassDeclarationSyntax>(unit.Members[0]);
        var label = cls.StatesBlock!.Labels.First();
        Assert.Equal("See.Fast", label.LabelName);
    }

    // ------------------------------------------------------------------
    // Struct declaration
    // ------------------------------------------------------------------

    [Fact]
    public void ShouldParseStructDeclaration()
    {
        var source = @"struct MyData
{
    int x;
    int y;
}";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        Assert.Empty(parser.Diagnostics);
        var structDecl = Assert.IsType<StructDeclarationSyntax>(unit.Members[0]);
        Assert.Equal("MyData", structDecl.Identifier.Text);
        Assert.Equal(2, structDecl.Members.Length);
    }

    // ------------------------------------------------------------------
    // Enum declaration
    // ------------------------------------------------------------------

    [Fact]
    public void ShouldParseEnumDeclaration()
    {
        var source = @"enum MyEnum
{
    A,
    B,
    C
}";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        Assert.Empty(parser.Diagnostics);
        var enumDecl = Assert.IsType<EnumDeclarationSyntax>(unit.Members[0]);
        Assert.Equal("MyEnum", enumDecl.Identifier.Text);
    }

    // ------------------------------------------------------------------
    // Const declaration
    // ------------------------------------------------------------------

    [Fact]
    public void ShouldParseConstDeclaration()
    {
        var source = @"const int MAX_HEALTH = 100;";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        Assert.Empty(parser.Diagnostics);
        var constDecl = Assert.IsType<ConstDeclarationSyntax>(unit.Members[0]);
        Assert.Equal("MAX_HEALTH", constDecl.Identifier.Text);
    }

    // ------------------------------------------------------------------
    // Property with prefix
    // ------------------------------------------------------------------

    [Fact]
    public void ShouldParsePrefixedProperty()
    {
        var source = @"class Foo : Bar
{
    Default
    {
        Monster.Health 100;
    }
}";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        Assert.Empty(parser.Diagnostics);
        var cls = Assert.IsType<ClassDeclarationSyntax>(unit.Members[0]);
        var prop = cls.DefaultBlock!.Properties.First();
        Assert.Equal("Monster.Health", prop.FullPropertyName);
    }

    // ------------------------------------------------------------------
    // Prefixed flag
    // ------------------------------------------------------------------

    [Fact]
    public void ShouldParsePrefixedFlag()
    {
        var source = @"class Foo : Bar
{
    Default
    {
        +Monster.SOLID;
    }
}";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        Assert.Empty(parser.Diagnostics);
        var cls = Assert.IsType<ClassDeclarationSyntax>(unit.Members[0]);
        var flag = cls.DefaultBlock!.Flags.First();
        Assert.Equal("Monster.SOLID", flag.FullFlagName);
    }
}
