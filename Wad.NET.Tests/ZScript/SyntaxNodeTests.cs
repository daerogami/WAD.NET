using System.Collections.Immutable;
using System.Linq;
using WAD.NET.ZScript.Syntax;
using Xunit;

namespace WAD.NET.Tests.ZScript;

public class SyntaxNodeTests
{
    private static SyntaxToken MakeToken(SyntaxTokenKind kind, string text, int start, object? value = null)
    {
        return new SyntaxToken(kind, text, new TextSpan(start, text.Length), value: value);
    }

    [Fact]
    public void LiteralExpression_HasCorrectKindAndToken()
    {
        var token = MakeToken(SyntaxTokenKind.IntegerLiteral, "42", 0, value: 42);
        var literal = new LiteralExpressionSyntax(token);

        Assert.Equal(SyntaxNodeKind.LiteralExpression, literal.Kind);
        Assert.Equal(42, literal.Value);
        Assert.Equal(new TextSpan(0, 2), literal.Span);
        Assert.Empty(literal.ChildNodes());

        var childTokens = literal.ChildTokens().ToList();
        Assert.Single(childTokens);
        Assert.Equal(SyntaxTokenKind.IntegerLiteral, childTokens[0].Kind);
        Assert.Equal("42", childTokens[0].Text);
    }

    [Fact]
    public void BinaryExpression_ReturnsLeftAndRightAsChildNodes()
    {
        var leftToken = MakeToken(SyntaxTokenKind.IntegerLiteral, "1", 0, value: 1);
        var opToken = MakeToken(SyntaxTokenKind.Plus, "+", 2);
        var rightToken = MakeToken(SyntaxTokenKind.IntegerLiteral, "2", 4, value: 2);

        var left = new LiteralExpressionSyntax(leftToken);
        var right = new LiteralExpressionSyntax(rightToken);
        var binary = new BinaryExpressionSyntax(left, opToken, right);

        Assert.Equal(SyntaxNodeKind.BinaryExpression, binary.Kind);
        Assert.Equal(new TextSpan(0, 5), binary.Span);

        var childNodes = binary.ChildNodes().ToList();
        Assert.Equal(2, childNodes.Count);
        Assert.Same(left, childNodes[0]);
        Assert.Same(right, childNodes[1]);

        var childTokens = binary.ChildTokens().ToList();
        Assert.Single(childTokens);
        Assert.Equal(SyntaxTokenKind.Plus, childTokens[0].Kind);
    }

    [Fact]
    public void CompilationUnit_ReturnsChildNodesInOrder()
    {
        var versionKw = MakeToken(SyntaxTokenKind.VersionKeyword, "version", 0);
        var versionStr = MakeToken(SyntaxTokenKind.StringLiteral, "\"4.10\"", 8, value: "4.10");
        var versionDirective = new VersionDirectiveSyntax(versionKw, versionStr);

        var eofToken = MakeToken(SyntaxTokenKind.EndOfFile, "", 14);
        var unit = new CompilationUnitSyntax(
            versionDirective,
            ImmutableArray<IncludeDirectiveSyntax>.Empty,
            ImmutableArray<MemberDeclarationSyntax>.Empty,
            eofToken);

        Assert.Equal(SyntaxNodeKind.CompilationUnit, unit.Kind);

        var childNodes = unit.ChildNodes().ToList();
        Assert.Single(childNodes);
        Assert.IsType<VersionDirectiveSyntax>(childNodes[0]);

        var childTokens = unit.ChildTokens().ToList();
        Assert.Single(childTokens);
        Assert.Equal(SyntaxTokenKind.EndOfFile, childTokens[0].Kind);
    }

    [Fact]
    public void VersionDirective_HasCorrectSpan()
    {
        var versionKw = MakeToken(SyntaxTokenKind.VersionKeyword, "version", 0);
        var versionStr = MakeToken(SyntaxTokenKind.StringLiteral, "\"4.10\"", 8, value: "4.10");
        var versionDirective = new VersionDirectiveSyntax(versionKw, versionStr);

        Assert.Equal(SyntaxNodeKind.VersionDirective, versionDirective.Kind);
        Assert.Equal(new TextSpan(0, 14), versionDirective.Span);

        var tokens = versionDirective.ChildTokens().ToList();
        Assert.Equal(2, tokens.Count);
        Assert.Equal("version", tokens[0].Text);
        Assert.Equal("\"4.10\"", tokens[1].Text);
    }

    [Fact]
    public void IdentifierExpression_HasCorrectKindAndIdentifier()
    {
        var token = MakeToken(SyntaxTokenKind.Identifier, "health", 0);
        var ident = new IdentifierExpressionSyntax(token);

        Assert.Equal(SyntaxNodeKind.IdentifierExpression, ident.Kind);
        Assert.Equal("health", ident.Identifier.Text);
        Assert.Empty(ident.ChildNodes());
        Assert.Single(ident.ChildTokens());
    }

    [Fact]
    public void UnaryExpression_HasOperandAsChild()
    {
        var opToken = MakeToken(SyntaxTokenKind.Minus, "-", 0);
        var numToken = MakeToken(SyntaxTokenKind.IntegerLiteral, "5", 1, value: 5);
        var operand = new LiteralExpressionSyntax(numToken);
        var unary = new UnaryExpressionSyntax(opToken, operand);

        Assert.Equal(SyntaxNodeKind.UnaryExpression, unary.Kind);
        Assert.Equal(new TextSpan(0, 2), unary.Span);

        var childNodes = unary.ChildNodes().ToList();
        Assert.Single(childNodes);
        Assert.Same(operand, childNodes[0]);
    }

    [Fact]
    public void BlockStatement_ContainsStatements()
    {
        var openBrace = MakeToken(SyntaxTokenKind.OpenBrace, "{", 0);
        var closeBrace = MakeToken(SyntaxTokenKind.CloseBrace, "}", 1);
        var block = new BlockStatementSyntax(
            openBrace,
            ImmutableArray<StatementSyntax>.Empty,
            closeBrace);

        Assert.Equal(SyntaxNodeKind.BlockStatement, block.Kind);
        Assert.Empty(block.ChildNodes());

        var tokens = block.ChildTokens().ToList();
        Assert.Equal(2, tokens.Count);
    }

    [Fact]
    public void PredefinedType_HasKeywordToken()
    {
        var keyword = MakeToken(SyntaxTokenKind.IntKeyword, "int", 0);
        var type = new PredefinedTypeSyntax(keyword);

        Assert.Equal(SyntaxNodeKind.PredefinedType, type.Kind);
        Assert.Empty(type.ChildNodes());
        Assert.Single(type.ChildTokens());
        Assert.Equal("int", type.ChildTokens().First().Text);
    }

    [Fact]
    public void StateStop_HasCorrectKindAndToken()
    {
        var stopKw = MakeToken(SyntaxTokenKind.StopKeyword, "Stop", 0);
        var stop = new StateStopSyntax(stopKw);

        Assert.Equal(SyntaxNodeKind.StateStop, stop.Kind);
        Assert.Empty(stop.ChildNodes());
        Assert.Single(stop.ChildTokens());
    }

    [Fact]
    public void FlagDefinition_IsSet_ReturnsCorrectly()
    {
        var plus = MakeToken(SyntaxTokenKind.Plus, "+", 0);
        var flagName = MakeToken(SyntaxTokenKind.Identifier, "SOLID", 1);
        var flag = new FlagDefinitionSyntax(plus, null, null, flagName, null);

        Assert.Equal(SyntaxNodeKind.FlagDefinition, flag.Kind);
        Assert.True(flag.IsSet);
        Assert.Equal("SOLID", flag.FullFlagName);
    }

    [Fact]
    public void FlagDefinition_WithPrefix_ReturnsFullName()
    {
        var minus = MakeToken(SyntaxTokenKind.Minus, "-", 0);
        var prefix = MakeToken(SyntaxTokenKind.Identifier, "Monster", 1);
        var dot = MakeToken(SyntaxTokenKind.Dot, ".", 8);
        var flagName = MakeToken(SyntaxTokenKind.Identifier, "NOGRAVITY", 9);
        var flag = new FlagDefinitionSyntax(minus, prefix, dot, flagName, null);

        Assert.False(flag.IsSet);
        Assert.Equal("Monster.NOGRAVITY", flag.FullFlagName);
    }

    [Fact]
    public void VariableDeclarator_WithInitializer_HasCorrectChildren()
    {
        var ident = MakeToken(SyntaxTokenKind.Identifier, "x", 0);
        var eq = MakeToken(SyntaxTokenKind.Equals, "=", 2);
        var valToken = MakeToken(SyntaxTokenKind.IntegerLiteral, "5", 4, value: 5);
        var value = new LiteralExpressionSyntax(valToken);

        var declarator = new VariableDeclaratorSyntax(ident, eq, value);

        Assert.Equal(SyntaxNodeKind.VariableDeclarator, declarator.Kind);

        var childNodes = declarator.ChildNodes().ToList();
        Assert.Single(childNodes);
        Assert.Same(value, childNodes[0]);

        var childTokens = declarator.ChildTokens().ToList();
        Assert.Equal(2, childTokens.Count);
        Assert.Equal("x", childTokens[0].Text);
        Assert.Equal("=", childTokens[1].Text);
    }

    [Fact]
    public void ReturnStatement_WithoutExpression_HasNoChildNodes()
    {
        var returnKw = MakeToken(SyntaxTokenKind.ReturnKeyword, "return", 0);
        var semi = MakeToken(SyntaxTokenKind.Semicolon, ";", 6);
        var stmt = new ReturnStatementSyntax(returnKw, null, semi);

        Assert.Equal(SyntaxNodeKind.ReturnStatement, stmt.Kind);
        Assert.Empty(stmt.ChildNodes());

        var tokens = stmt.ChildTokens().ToList();
        Assert.Equal(2, tokens.Count);
    }

    [Fact]
    public void ReturnStatement_WithExpression_HasChildNode()
    {
        var returnKw = MakeToken(SyntaxTokenKind.ReturnKeyword, "return", 0);
        var valToken = MakeToken(SyntaxTokenKind.IntegerLiteral, "0", 7, value: 0);
        var value = new LiteralExpressionSyntax(valToken);
        var semi = MakeToken(SyntaxTokenKind.Semicolon, ";", 8);
        var stmt = new ReturnStatementSyntax(returnKw, value, semi);

        var childNodes = stmt.ChildNodes().ToList();
        Assert.Single(childNodes);
        Assert.Same(value, childNodes[0]);
    }

    [Fact]
    public void StateLabelSyntax_LabelName_WithSubLabel()
    {
        var ident = MakeToken(SyntaxTokenKind.Identifier, "See", 0);
        var dot = MakeToken(SyntaxTokenKind.Dot, ".", 3);
        var sub = MakeToken(SyntaxTokenKind.Identifier, "Init", 4);
        var colon = MakeToken(SyntaxTokenKind.Colon, ":", 8);

        var label = new StateLabelSyntax(ident, dot, sub, colon);

        Assert.Equal(SyntaxNodeKind.StateLabel, label.Kind);
        Assert.Equal("See.Init", label.LabelName);
    }

    [Fact]
    public void CompilationUnit_WithClassDeclaration_ExposedViaConvenienceProperty()
    {
        // Build a minimal class: class Foo { }
        var classKw = MakeToken(SyntaxTokenKind.ClassKeyword, "class", 0);
        var classId = MakeToken(SyntaxTokenKind.Identifier, "Foo", 6);
        var openBrace = MakeToken(SyntaxTokenKind.OpenBrace, "{", 10);
        var closeBrace = MakeToken(SyntaxTokenKind.CloseBrace, "}", 11);

        var classDecl = new ClassDeclarationSyntax(
            ImmutableArray<SyntaxToken>.Empty,
            classKw,
            classId,
            null,
            null,
            null,
            null,
            openBrace,
            ImmutableArray<MemberDeclarationSyntax>.Empty,
            closeBrace);

        var eofToken = MakeToken(SyntaxTokenKind.EndOfFile, "", 12);
        var unit = new CompilationUnitSyntax(
            null,
            ImmutableArray<IncludeDirectiveSyntax>.Empty,
            ImmutableArray.Create<MemberDeclarationSyntax>(classDecl),
            eofToken);

        Assert.Single(unit.Classes);
        Assert.Equal("Foo", unit.Classes.First().Name);
        Assert.Empty(unit.Actors);
    }
}
