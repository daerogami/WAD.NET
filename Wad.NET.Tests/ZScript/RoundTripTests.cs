using WAD.NET.ZScript;
using WAD.NET.ZScript.Syntax;
using Xunit;

namespace WAD.NET.Tests.ZScript;

public class RoundTripTests
{
    [Theory]
    [InlineData("class Foo : Bar { }")]
    [InlineData("class Foo : Bar\n{\n}")]
    [InlineData("struct MyData { }")]
    public void ShouldPreserveSourceExactly(string source)
    {
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        Assert.Empty(parser.Diagnostics);
        Assert.Equal(source, unit.ToFullString());
    }

    [Fact]
    public void ShouldPreserveVersionDirective()
    {
        var source = "version \"4.10.0\"";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        Assert.Empty(parser.Diagnostics);
        Assert.Equal(source, unit.ToFullString());
    }

    [Fact]
    public void ShouldPreserveClassWithDefaultBlock()
    {
        var source = "class MyClass : Actor\n{\n    Default\n    {\n        Health 100;\n    }\n}";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        Assert.Empty(parser.Diagnostics);
        Assert.Equal(source, unit.ToFullString());
    }

    [Fact]
    public void ShouldPreserveActorDeclaration()
    {
        var source = "actor Test { }";
        var parser = new Parser(source, ScriptLanguage.Decorate);
        var unit = parser.ParseCompilationUnit();
        Assert.Empty(parser.Diagnostics);
        Assert.Equal(source, unit.ToFullString());
    }

    [Fact]
    public void ShouldPreserveClassWithMethod()
    {
        var source = "class Foo : Bar\n{\n    void Test()\n    {\n        return;\n    }\n}";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        Assert.Empty(parser.Diagnostics);
        Assert.Equal(source, unit.ToFullString());
    }

    [Fact]
    public void ShouldPreserveClassWithField()
    {
        var source = "class Foo : Bar\n{\n    int health;\n}";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        Assert.Empty(parser.Diagnostics);
        Assert.Equal(source, unit.ToFullString());
    }

    [Fact]
    public void ShouldPreserveClassWithStatesBlock()
    {
        var source = "class Foo : Bar\n{\n    States\n    {\n    Spawn:\n        TNT1 A 1\n        Loop\n    }\n}";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        Assert.Empty(parser.Diagnostics);
        Assert.Equal(source, unit.ToFullString());
    }

    [Fact]
    public void ShouldPreserveInclude()
    {
        // Note: #include is consumed as preprocessor trivia by the lexer,
        // so the round-trip includes it in the trivia of the EOF token.
        // We verify the include is detected and the trivia-based text is present.
        var source = "#include \"weapons.zs\"";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        Assert.Empty(parser.Diagnostics);
        Assert.Single(unit.Includes);
        // ToFullString reconstructs from tokens including trivia;
        // since the include is trivia, the original text is in the EOF leading trivia
        Assert.Contains(source, unit.ToFullString());
    }

    [Fact]
    public void ShouldPreserveWhitespace()
    {
        var source = "class   Foo   :   Bar   {   }";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        Assert.Empty(parser.Diagnostics);
        Assert.Equal(source, unit.ToFullString());
    }

    [Fact]
    public void ShouldPreserveConstDeclaration()
    {
        var source = "const int MAX = 100;";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        Assert.Empty(parser.Diagnostics);
        Assert.Equal(source, unit.ToFullString());
    }

    [Fact]
    public void ShouldPreserveEnumDeclaration()
    {
        var source = "enum Colors\n{\n    Red,\n    Green,\n    Blue\n}";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        Assert.Empty(parser.Diagnostics);
        Assert.Equal(source, unit.ToFullString());
    }

    [Fact]
    public void ShouldPreserveStateGoto()
    {
        var source = "class Foo : Bar\n{\n    States\n    {\n    Spawn:\n        TNT1 A 1\n        Goto See\n    }\n}";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        Assert.Empty(parser.Diagnostics);
        Assert.Equal(source, unit.ToFullString());
    }

    [Fact]
    public void ShouldPreserveStateStop()
    {
        var source = "class Foo : Bar\n{\n    States\n    {\n    Spawn:\n        TNT1 A 1\n        Stop\n    }\n}";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        Assert.Empty(parser.Diagnostics);
        Assert.Equal(source, unit.ToFullString());
    }

    [Fact]
    public void ShouldPreserveNativeMethod()
    {
        var source = "class Foo : Bar\n{\n    native void Tick();\n}";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        Assert.Empty(parser.Diagnostics);
        Assert.Equal(source, unit.ToFullString());
    }

    [Fact]
    public void ShouldPreserveFlags()
    {
        var source = "class Foo : Bar\n{\n    Default\n    {\n        +SOLID;\n        -NOGRAVITY;\n    }\n}";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        Assert.Empty(parser.Diagnostics);
        Assert.Equal(source, unit.ToFullString());
    }
}
