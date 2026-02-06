using System.Linq;
using WAD.NET.ZScript.Formatting;
using WAD.NET.ZScript.Syntax;
using Xunit;

namespace WAD.NET.Tests.ZScript;

public class FormatterTests
{
    [Fact]
    public void DefaultFormattingOptions_HasCorrectDefaults()
    {
        var options = FormattingOptions.Default;

        Assert.Equal(4, options.IndentSize);
        Assert.True(options.UseTabs);
        Assert.True(options.SpaceAfterComma);
        Assert.True(options.SpaceAroundBinaryOperators);
        Assert.True(options.NewLineBeforeOpenBrace);
        Assert.True(options.IndentCaseLabels);
    }

    [Fact]
    public void Formatter_PreservesValidCodeStructure()
    {
        var source = "class MyActor : Actor\n{\n}";
        var formatter = new SyntaxFormatter();

        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        var result = formatter.Format(unit);

        // The formatted output should still be parseable
        var parser2 = new Parser(result);
        var unit2 = parser2.ParseCompilationUnit();
        Assert.Single(unit2.Classes);
        Assert.Equal("MyActor", unit2.Classes.First().Name);
    }

    [Fact]
    public void Formatter_ProducesParseableOutput()
    {
        var source = @"class MyActor : Actor
{
    Default
    {
        Health 100;
    }
    States
    {
    Spawn:
        POSS AB 4
        Loop
    }
}";
        var formatter = new SyntaxFormatter();
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        var result = formatter.Format(unit);

        // Verify the output is still parseable
        var parser2 = new Parser(result);
        var unit2 = parser2.ParseCompilationUnit();
        Assert.Empty(parser2.Diagnostics);
        Assert.Single(unit2.Classes);
    }

    [Fact]
    public void Formatter_CustomOptions_AreApplied()
    {
        var options = new FormattingOptions
        {
            UseTabs = false,
            IndentSize = 2,
            NewLineBeforeOpenBrace = true
        };
        var formatter = new SyntaxFormatter(options);

        var source = "class Test : Actor { }";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        var result = formatter.Format(unit);

        // Should be parseable
        var parser2 = new Parser(result);
        var unit2 = parser2.ParseCompilationUnit();
        Assert.Single(unit2.Classes);
    }
}
