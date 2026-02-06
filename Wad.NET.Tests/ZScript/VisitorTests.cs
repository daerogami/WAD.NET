using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using WAD.NET.ZScript.Syntax;
using Xunit;

namespace WAD.NET.Tests.ZScript;

public class VisitorTests
{
    #region Helper Methods

    private static SyntaxToken MakeToken(SyntaxTokenKind kind, string text, int start, object? value = null)
    {
        return new SyntaxToken(kind, text, new TextSpan(start, text.Length), value: value);
    }

    /// <summary>
    /// Creates a minimal actor declaration with the given name and state entries.
    /// </summary>
    private static ActorDeclarationSyntax MakeActor(string name, int offset, ImmutableArray<MemberDeclarationSyntax> body)
    {
        var actorKw = MakeToken(SyntaxTokenKind.ActorKeyword, "actor", offset);
        var ident = MakeToken(SyntaxTokenKind.Identifier, name, offset + 6);
        var openBrace = MakeToken(SyntaxTokenKind.OpenBrace, "{", offset + 6 + name.Length + 1);
        var closeBrace = MakeToken(SyntaxTokenKind.CloseBrace, "}", offset + 6 + name.Length + 2);

        return new ActorDeclarationSyntax(
            actorKw, ident, null, null, null, null, null,
            openBrace, body, closeBrace);
    }

    private static StateLabelSyntax MakeStateLabel(string label, int offset)
    {
        var ident = MakeToken(SyntaxTokenKind.Identifier, label, offset);
        var colon = MakeToken(SyntaxTokenKind.Colon, ":", offset + label.Length);
        return new StateLabelSyntax(ident, null, null, colon);
    }

    private static StateGotoSyntax MakeStateGoto(string target, int offset)
    {
        var gotoKw = MakeToken(SyntaxTokenKind.GotoKeyword, "Goto", offset);
        var labelIdent = MakeToken(SyntaxTokenKind.Identifier, target, offset + 5);
        return new StateGotoSyntax(gotoKw, null, null, labelIdent, null, null);
    }

    private static StatesBlockSyntax MakeStatesBlock(int offset, params StateSyntax[] states)
    {
        var statesKw = MakeToken(SyntaxTokenKind.StatesKeyword, "States", offset);
        var openBrace = MakeToken(SyntaxTokenKind.OpenBrace, "{", offset + 7);
        var closeBrace = MakeToken(SyntaxTokenKind.CloseBrace, "}", offset + 100);

        return new StatesBlockSyntax(
            statesKw, null, ImmutableArray<SyntaxToken>.Empty, null,
            openBrace, ImmutableArray.CreateRange(states), closeBrace);
    }

    private static StateFrameSyntax MakeStateFrame(int offset, StateActionSyntax? action = null)
    {
        var sprite = MakeToken(SyntaxTokenKind.Identifier, "TNT1", offset);
        var frames = MakeToken(SyntaxTokenKind.Identifier, "A", offset + 5);
        var duration = MakeToken(SyntaxTokenKind.IntegerLiteral, "1", offset + 7, value: 1);
        return new StateFrameSyntax(sprite, frames, duration, ImmutableArray<SyntaxToken>.Empty, action);
    }

    private static CompilationUnitSyntax MakeCompilationUnit(params MemberDeclarationSyntax[] members)
    {
        var eof = MakeToken(SyntaxTokenKind.EndOfFile, "", 1000);
        return new CompilationUnitSyntax(
            null,
            ImmutableArray<IncludeDirectiveSyntax>.Empty,
            ImmutableArray.CreateRange(members),
            eof);
    }

    #endregion

    #region StateLabelCollector Tests

    [Fact]
    public void StateLabelCollector_FindsAllLabels()
    {
        // Arrange: Build a tree with Spawn and See labels
        var spawn = MakeStateLabel("Spawn", 10);
        var see = MakeStateLabel("See", 30);
        var statesBlock = MakeStatesBlock(0, spawn, see);
        var actor = MakeActor("ZombieMan", 0,
            ImmutableArray.Create<MemberDeclarationSyntax>(statesBlock));
        var unit = MakeCompilationUnit(actor);

        // Act
        var collector = new StateLabelCollector();
        collector.Visit(unit);

        // Assert
        Assert.Equal(2, collector.Labels.Count);
        Assert.Contains("Spawn", collector.Labels);
        Assert.Contains("See", collector.Labels);
    }

    [Fact]
    public void StateLabelCollector_EmptyTree_ReturnsNoLabels()
    {
        var unit = MakeCompilationUnit();

        var collector = new StateLabelCollector();
        collector.Visit(unit);

        Assert.Empty(collector.Labels);
    }

    [Fact]
    public void StateLabelCollector_MultipleActors_CollectsFromAll()
    {
        var spawn1 = MakeStateLabel("Spawn", 10);
        var states1 = MakeStatesBlock(0, spawn1);
        var actor1 = MakeActor("ZombieMan", 0,
            ImmutableArray.Create<MemberDeclarationSyntax>(states1));

        var death = MakeStateLabel("Death", 200);
        var states2 = MakeStatesBlock(190, death);
        var actor2 = MakeActor("Imp", 180,
            ImmutableArray.Create<MemberDeclarationSyntax>(states2));

        var unit = MakeCompilationUnit(actor1, actor2);

        var collector = new StateLabelCollector();
        collector.Visit(unit);

        Assert.Equal(2, collector.Labels.Count);
        Assert.Contains("Spawn", collector.Labels);
        Assert.Contains("Death", collector.Labels);
    }

    #endregion

    #region UnusedStateFinder Tests

    [Fact]
    public void UnusedStateFinder_IdentifiesUnreferencedLabels()
    {
        // Arrange: Spawn is defined but never goto'd; See is defined and goto'd
        var spawn = MakeStateLabel("Spawn", 10);
        var see = MakeStateLabel("See", 30);
        var gotoSee = MakeStateGoto("See", 50);
        var statesBlock = MakeStatesBlock(0, spawn, see, gotoSee);
        var actor = MakeActor("ZombieMan", 0,
            ImmutableArray.Create<MemberDeclarationSyntax>(statesBlock));
        var unit = MakeCompilationUnit(actor);

        // Act
        var finder = new UnusedStateFinder();
        finder.Visit(unit);

        // Assert
        var unused = finder.UnusedLabels.ToList();
        Assert.Single(unused);
        Assert.Equal("Spawn", unused[0]);
    }

    [Fact]
    public void UnusedStateFinder_AllLabelsReferenced_ReturnsEmpty()
    {
        var spawn = MakeStateLabel("Spawn", 10);
        var see = MakeStateLabel("See", 30);
        var gotoSpawn = MakeStateGoto("Spawn", 50);
        var gotoSee = MakeStateGoto("See", 70);
        var statesBlock = MakeStatesBlock(0, spawn, see, gotoSpawn, gotoSee);
        var actor = MakeActor("ZombieMan", 0,
            ImmutableArray.Create<MemberDeclarationSyntax>(statesBlock));
        var unit = MakeCompilationUnit(actor);

        var finder = new UnusedStateFinder();
        finder.Visit(unit);

        Assert.Empty(finder.UnusedLabels);
    }

    [Fact]
    public void UnusedStateFinder_CaseInsensitiveMatching()
    {
        var spawn = MakeStateLabel("Spawn", 10);
        // Goto references with different casing
        var gotoSpawn = MakeStateGoto("spawn", 50);
        var statesBlock = MakeStatesBlock(0, spawn, gotoSpawn);
        var actor = MakeActor("ZombieMan", 0,
            ImmutableArray.Create<MemberDeclarationSyntax>(statesBlock));
        var unit = MakeCompilationUnit(actor);

        var finder = new UnusedStateFinder();
        finder.Visit(unit);

        Assert.Empty(finder.UnusedLabels);
    }

    #endregion

    #region SyntaxRewriter Identity Tests

    /// <summary>
    /// A rewriter that makes no changes, used to test identity.
    /// </summary>
    private class IdentityRewriter : SyntaxRewriter
    {
    }

    [Fact]
    public void IdentityRewriter_ReturnsSameReference_ForUnchangedTree()
    {
        var spawn = MakeStateLabel("Spawn", 10);
        var stop = new StateStopSyntax(MakeToken(SyntaxTokenKind.StopKeyword, "Stop", 20));
        var statesBlock = MakeStatesBlock(0, spawn, stop);
        var actor = MakeActor("ZombieMan", 0,
            ImmutableArray.Create<MemberDeclarationSyntax>(statesBlock));
        var unit = MakeCompilationUnit(actor);

        var rewriter = new IdentityRewriter();
        var result = rewriter.Visit(unit);

        Assert.Same(unit, result);
    }

    [Fact]
    public void IdentityRewriter_ReturnsSameReference_ForBinaryExpression()
    {
        var leftToken = MakeToken(SyntaxTokenKind.IntegerLiteral, "1", 0, value: 1);
        var opToken = MakeToken(SyntaxTokenKind.Plus, "+", 2);
        var rightToken = MakeToken(SyntaxTokenKind.IntegerLiteral, "2", 4, value: 2);

        var left = new LiteralExpressionSyntax(leftToken);
        var right = new LiteralExpressionSyntax(rightToken);
        var binary = new BinaryExpressionSyntax(left, opToken, right);

        var rewriter = new IdentityRewriter();
        var result = rewriter.Visit(binary);

        Assert.Same(binary, result);
    }

    [Fact]
    public void IdentityRewriter_ReturnsSameReference_ForIfStatement()
    {
        var ifKw = MakeToken(SyntaxTokenKind.IfKeyword, "if", 0);
        var openParen = MakeToken(SyntaxTokenKind.OpenParen, "(", 3);
        var condToken = MakeToken(SyntaxTokenKind.TrueKeyword, "true", 4, value: true);
        var closeParen = MakeToken(SyntaxTokenKind.CloseParen, ")", 8);
        var openBrace = MakeToken(SyntaxTokenKind.OpenBrace, "{", 10);
        var closeBrace = MakeToken(SyntaxTokenKind.CloseBrace, "}", 11);

        var condition = new LiteralExpressionSyntax(condToken);
        var body = new BlockStatementSyntax(openBrace, ImmutableArray<StatementSyntax>.Empty, closeBrace);
        var ifStmt = new IfStatementSyntax(ifKw, openParen, condition, closeParen, body);

        var rewriter = new IdentityRewriter();
        var result = rewriter.Visit(ifStmt);

        Assert.Same(ifStmt, result);
    }

    [Fact]
    public void IdentityRewriter_ReturnsSameReference_ForBlockStatement()
    {
        var openBrace = MakeToken(SyntaxTokenKind.OpenBrace, "{", 0);
        var closeBrace = MakeToken(SyntaxTokenKind.CloseBrace, "}", 1);
        var block = new BlockStatementSyntax(openBrace, ImmutableArray<StatementSyntax>.Empty, closeBrace);

        var rewriter = new IdentityRewriter();
        var result = rewriter.Visit(block);

        Assert.Same(block, result);
    }

    #endregion

    #region ActorRenamer Tests

    [Fact]
    public void ActorRenamer_ChangesActorName()
    {
        var actor = MakeActor("ZombieMan", 0, ImmutableArray<MemberDeclarationSyntax>.Empty);
        var unit = MakeCompilationUnit(actor);

        var renamer = new ActorRenamer(name => name == "ZombieMan" ? "FastZombie" : name);
        var result = (CompilationUnitSyntax?)renamer.Visit(unit);

        Assert.NotNull(result);
        Assert.NotSame(unit, result);

        var resultActor = result!.Actors.First();
        Assert.Equal("FastZombie", resultActor.Name);
    }

    [Fact]
    public void ActorRenamer_DoesNotChangeName_WhenNotMatched()
    {
        var actor = MakeActor("Imp", 0, ImmutableArray<MemberDeclarationSyntax>.Empty);
        var unit = MakeCompilationUnit(actor);

        var renamer = new ActorRenamer(name => name == "ZombieMan" ? "FastZombie" : name);
        var result = (CompilationUnitSyntax?)renamer.Visit(unit);

        Assert.NotNull(result);
        // The compilation unit is the same since nothing inside changed
        Assert.Same(unit, result);
    }

    [Fact]
    public void ActorRenamer_RenamesMultipleActors()
    {
        var actor1 = MakeActor("ZombieMan", 0, ImmutableArray<MemberDeclarationSyntax>.Empty);
        var actor2 = MakeActor("Imp", 100, ImmutableArray<MemberDeclarationSyntax>.Empty);
        var unit = MakeCompilationUnit(actor1, actor2);

        var renamer = new ActorRenamer(name => "My" + name);
        var result = (CompilationUnitSyntax?)renamer.Visit(unit);

        Assert.NotNull(result);
        Assert.NotSame(unit, result);

        var actors = result!.Actors.ToList();
        Assert.Equal(2, actors.Count);
        Assert.Equal("MyZombieMan", actors[0].Name);
        Assert.Equal("MyImp", actors[1].Name);
    }

    [Fact]
    public void ActorRenamer_PreservesBody()
    {
        var spawn = MakeStateLabel("Spawn", 50);
        var stop = new StateStopSyntax(MakeToken(SyntaxTokenKind.StopKeyword, "Stop", 60));
        var statesBlock = MakeStatesBlock(40, spawn, stop);
        var actor = MakeActor("ZombieMan", 0,
            ImmutableArray.Create<MemberDeclarationSyntax>(statesBlock));
        var unit = MakeCompilationUnit(actor);

        var renamer = new ActorRenamer(name => "Renamed" + name);
        var result = (CompilationUnitSyntax?)renamer.Visit(unit);

        Assert.NotNull(result);
        var resultActor = result!.Actors.First();
        Assert.Equal("RenamedZombieMan", resultActor.Name);

        // Body should still have the states block
        Assert.Single(resultActor.Body);
        Assert.IsType<StatesBlockSyntax>(resultActor.Body[0]);
    }

    #endregion

    #region DefaultVisit Traversal Tests

    /// <summary>
    /// A visitor that counts the number of nodes visited.
    /// </summary>
    private class NodeCounter : SyntaxVisitor
    {
        public int Count { get; private set; }

        public override void DefaultVisit(SyntaxNode node)
        {
            Count++;
            base.DefaultVisit(node);
        }
    }

    [Fact]
    public void DefaultVisit_TraversesAllChildNodes()
    {
        // Build: actor Foo { States { Spawn: Stop } }
        var spawn = MakeStateLabel("Spawn", 30);
        var stop = new StateStopSyntax(MakeToken(SyntaxTokenKind.StopKeyword, "Stop", 40));
        var statesBlock = MakeStatesBlock(20, spawn, stop);
        var actor = MakeActor("Foo", 0,
            ImmutableArray.Create<MemberDeclarationSyntax>(statesBlock));
        var unit = MakeCompilationUnit(actor);

        var counter = new NodeCounter();
        counter.Visit(unit);

        // CompilationUnit -> ActorDeclaration -> StatesBlock -> StateLabel, StateStop
        // = 5 nodes total
        Assert.Equal(5, counter.Count);
    }

    [Fact]
    public void DefaultVisit_TraversesBinaryExpressionChildren()
    {
        var leftToken = MakeToken(SyntaxTokenKind.IntegerLiteral, "1", 0, value: 1);
        var opToken = MakeToken(SyntaxTokenKind.Plus, "+", 2);
        var rightToken = MakeToken(SyntaxTokenKind.IntegerLiteral, "2", 4, value: 2);

        var left = new LiteralExpressionSyntax(leftToken);
        var right = new LiteralExpressionSyntax(rightToken);
        var binary = new BinaryExpressionSyntax(left, opToken, right);

        var counter = new NodeCounter();
        counter.Visit(binary);

        // BinaryExpression -> LiteralExpression (left), LiteralExpression (right)
        Assert.Equal(3, counter.Count);
    }

    #endregion

    #region Custom Visitor (SyntaxVisitor<TResult>) Tests

    /// <summary>
    /// A visitor that counts all expression nodes and returns the count.
    /// </summary>
    private class ExpressionCounter : SyntaxVisitor<int>
    {
        public override int DefaultVisit(SyntaxNode node)
        {
            int count = 0;
            foreach (var child in node.ChildNodes())
            {
                count += Visit(child);
            }
            return count;
        }

        public override int VisitLiteralExpression(LiteralExpressionSyntax node) => 1;
        public override int VisitIdentifierExpression(IdentifierExpressionSyntax node) => 1;

        public override int VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            return 1 + Visit(node.Left) + Visit(node.Right);
        }

        public override int VisitUnaryExpression(UnaryExpressionSyntax node)
        {
            return 1 + Visit(node.Operand);
        }
    }

    [Fact]
    public void CustomVisitor_CountsExpressions()
    {
        // Build: 1 + 2
        var leftToken = MakeToken(SyntaxTokenKind.IntegerLiteral, "1", 0, value: 1);
        var opToken = MakeToken(SyntaxTokenKind.Plus, "+", 2);
        var rightToken = MakeToken(SyntaxTokenKind.IntegerLiteral, "2", 4, value: 2);

        var left = new LiteralExpressionSyntax(leftToken);
        var right = new LiteralExpressionSyntax(rightToken);
        var binary = new BinaryExpressionSyntax(left, opToken, right);

        var counter = new ExpressionCounter();
        var result = counter.Visit(binary);

        // 1 (binary) + 1 (left literal) + 1 (right literal)
        Assert.Equal(3, result);
    }

    [Fact]
    public void CustomVisitor_CountsNestedExpressions()
    {
        // Build: -(1 + 2)
        var leftToken = MakeToken(SyntaxTokenKind.IntegerLiteral, "1", 2, value: 1);
        var opToken = MakeToken(SyntaxTokenKind.Plus, "+", 4);
        var rightToken = MakeToken(SyntaxTokenKind.IntegerLiteral, "2", 6, value: 2);

        var left = new LiteralExpressionSyntax(leftToken);
        var right = new LiteralExpressionSyntax(rightToken);
        var binary = new BinaryExpressionSyntax(left, opToken, right);

        var negOp = MakeToken(SyntaxTokenKind.Minus, "-", 0);
        var unary = new UnaryExpressionSyntax(negOp, binary);

        var counter = new ExpressionCounter();
        var result = counter.Visit(unary);

        // 1 (unary) + 1 (binary) + 1 (left) + 1 (right) = 4
        Assert.Equal(4, result);
    }

    #endregion

    #region Rewriter With Changes Tests

    /// <summary>
    /// A rewriter that replaces all integer literal 42 with 100.
    /// </summary>
    private class LiteralRewriter : SyntaxRewriter
    {
        public override SyntaxNode? VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            if (node.Value is int i && i == 42)
            {
                var newToken = new SyntaxToken(
                    SyntaxTokenKind.IntegerLiteral, "100",
                    node.Token.Span, value: 100);
                return new LiteralExpressionSyntax(newToken);
            }
            return node;
        }
    }

    [Fact]
    public void Rewriter_ReplacesMatchingLiteral()
    {
        var leftToken = MakeToken(SyntaxTokenKind.IntegerLiteral, "42", 0, value: 42);
        var opToken = MakeToken(SyntaxTokenKind.Plus, "+", 3);
        var rightToken = MakeToken(SyntaxTokenKind.IntegerLiteral, "1", 5, value: 1);

        var left = new LiteralExpressionSyntax(leftToken);
        var right = new LiteralExpressionSyntax(rightToken);
        var binary = new BinaryExpressionSyntax(left, opToken, right);

        var rewriter = new LiteralRewriter();
        var result = (BinaryExpressionSyntax?)rewriter.Visit(binary);

        Assert.NotNull(result);
        Assert.NotSame(binary, result);
        Assert.IsType<LiteralExpressionSyntax>(result!.Left);
        Assert.Equal(100, ((LiteralExpressionSyntax)result.Left).Value);
        // Right side should be the same instance since it was not changed
        Assert.Same(right, result.Right);
    }

    [Fact]
    public void Rewriter_ReturnsOriginal_WhenNoLiteralMatches()
    {
        var leftToken = MakeToken(SyntaxTokenKind.IntegerLiteral, "1", 0, value: 1);
        var opToken = MakeToken(SyntaxTokenKind.Plus, "+", 2);
        var rightToken = MakeToken(SyntaxTokenKind.IntegerLiteral, "2", 4, value: 2);

        var left = new LiteralExpressionSyntax(leftToken);
        var right = new LiteralExpressionSyntax(rightToken);
        var binary = new BinaryExpressionSyntax(left, opToken, right);

        var rewriter = new LiteralRewriter();
        var result = rewriter.Visit(binary);

        Assert.Same(binary, result);
    }

    #endregion

    #region Leaf Node Rewriter Tests

    [Fact]
    public void Rewriter_LeafNodes_ReturnSameReference()
    {
        var rewriter = new IdentityRewriter();

        // StateStop
        var stop = new StateStopSyntax(MakeToken(SyntaxTokenKind.StopKeyword, "Stop", 0));
        Assert.Same(stop, rewriter.Visit(stop));

        // StateWait
        var wait = new StateWaitSyntax(MakeToken(SyntaxTokenKind.WaitKeyword, "Wait", 0));
        Assert.Same(wait, rewriter.Visit(wait));

        // StateLoop
        var loop = new StateLoopSyntax(MakeToken(SyntaxTokenKind.LoopKeyword, "Loop", 0));
        Assert.Same(loop, rewriter.Visit(loop));

        // StateFail
        var fail = new StateFailSyntax(MakeToken(SyntaxTokenKind.FailKeyword, "Fail", 0));
        Assert.Same(fail, rewriter.Visit(fail));

        // LiteralExpression
        var literal = new LiteralExpressionSyntax(
            MakeToken(SyntaxTokenKind.IntegerLiteral, "42", 0, value: 42));
        Assert.Same(literal, rewriter.Visit(literal));

        // IdentifierExpression
        var ident = new IdentifierExpressionSyntax(
            MakeToken(SyntaxTokenKind.Identifier, "foo", 0));
        Assert.Same(ident, rewriter.Visit(ident));

        // PredefinedType
        var predefined = new PredefinedTypeSyntax(
            MakeToken(SyntaxTokenKind.IntKeyword, "int", 0));
        Assert.Same(predefined, rewriter.Visit(predefined));
    }

    #endregion
}
