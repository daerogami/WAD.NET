using System.Collections.Generic;
using System.Linq;
using WAD.NET.ZScript.Syntax;
using Xunit;

namespace WAD.NET.Tests.ZScript;

public class LexerTests
{
    /// <summary>
    /// Helper to lex all tokens from source text.
    /// </summary>
    private static List<SyntaxToken> LexAll(string text)
    {
        var lexer = new Lexer(text);
        var tokens = new List<SyntaxToken>();
        SyntaxToken token;
        do
        {
            token = lexer.Lex();
            tokens.Add(token);
        }
        while (token.Kind != SyntaxTokenKind.EndOfFile);
        return tokens;
    }

    /// <summary>
    /// Helper to lex a single non-EOF token from source text.
    /// </summary>
    private static SyntaxToken LexOne(string text)
    {
        var tokens = LexAll(text);
        Assert.True(tokens.Count >= 2, "Expected at least one token plus EndOfFile");
        Assert.Equal(SyntaxTokenKind.EndOfFile, tokens[tokens.Count - 1].Kind);
        return tokens[0];
    }

    // ------------------------------------------------------------------
    // Empty / EndOfFile
    // ------------------------------------------------------------------

    [Fact]
    public void Lex_EmptyString_ReturnsEndOfFile()
    {
        var tokens = LexAll("");
        Assert.Single(tokens);
        Assert.Equal(SyntaxTokenKind.EndOfFile, tokens[0].Kind);
    }

    [Fact]
    public void Lex_EmptyString_SingleToken()
    {
        var tokens = LexAll("");
        Assert.Single(tokens);
        Assert.Equal(SyntaxTokenKind.EndOfFile, tokens[0].Kind);
    }

    // ------------------------------------------------------------------
    // Integer Literals
    // ------------------------------------------------------------------

    [Theory]
    [InlineData("0", 0L)]
    [InlineData("42", 42L)]
    [InlineData("12345", 12345L)]
    public void Lex_IntegerLiteral(string text, long expected)
    {
        var token = LexOne(text);
        Assert.Equal(SyntaxTokenKind.IntegerLiteral, token.Kind);
        Assert.Equal(text, token.Text);
        Assert.Equal(expected, token.Value);
    }

    [Theory]
    [InlineData("0xFF", 255L)]
    [InlineData("0x00", 0L)]
    [InlineData("0x1A", 26L)]
    [InlineData("0Xff", 255L)]
    [InlineData("0XFF", 255L)]
    public void Lex_HexLiteral(string text, long expected)
    {
        var token = LexOne(text);
        Assert.Equal(SyntaxTokenKind.IntegerLiteral, token.Kind);
        Assert.Equal(text, token.Text);
        Assert.Equal(expected, token.Value);
    }

    // ------------------------------------------------------------------
    // Float Literals
    // ------------------------------------------------------------------

    [Theory]
    [InlineData("3.14")]
    [InlineData("0.5")]
    [InlineData("100.0")]
    public void Lex_FloatLiteral(string text)
    {
        var token = LexOne(text);
        Assert.Equal(SyntaxTokenKind.FloatLiteral, token.Kind);
        Assert.Equal(text, token.Text);
        Assert.IsType<double>(token.Value);
    }

    [Fact]
    public void Lex_FloatLiteral_CorrectValue()
    {
        var token = LexOne("3.14");
        Assert.Equal(SyntaxTokenKind.FloatLiteral, token.Kind);
        Assert.Equal(3.14, (double)token.Value!, 10);
    }

    [Theory]
    [InlineData("1e10")]
    [InlineData("2.5E-3")]
    [InlineData("1E+5")]
    [InlineData("3e2")]
    public void Lex_ScientificNotation(string text)
    {
        var token = LexOne(text);
        Assert.Equal(SyntaxTokenKind.FloatLiteral, token.Kind);
        Assert.Equal(text, token.Text);
        Assert.IsType<double>(token.Value);
    }

    [Fact]
    public void Lex_ScientificNotation_CorrectValue()
    {
        var token = LexOne("2.5E-3");
        Assert.Equal(SyntaxTokenKind.FloatLiteral, token.Kind);
        Assert.Equal(0.0025, (double)token.Value!, 10);
    }

    [Fact]
    public void Lex_LeadingDotFloat()
    {
        var token = LexOne(".5");
        Assert.Equal(SyntaxTokenKind.FloatLiteral, token.Kind);
        Assert.Equal(".5", token.Text);
        Assert.Equal(0.5, (double)token.Value!, 10);
    }

    // ------------------------------------------------------------------
    // String Literals
    // ------------------------------------------------------------------

    [Fact]
    public void Lex_StringLiteral()
    {
        var token = LexOne("\"hello\"");
        Assert.Equal(SyntaxTokenKind.StringLiteral, token.Kind);
        Assert.Equal("\"hello\"", token.Text);
        Assert.Equal("hello", token.Value);
    }

    [Fact]
    public void Lex_StringLiteral_Empty()
    {
        var token = LexOne("\"\"");
        Assert.Equal(SyntaxTokenKind.StringLiteral, token.Kind);
        Assert.Equal("", token.Value);
    }

    [Theory]
    [InlineData("\"line\\nbreak\"", "line\nbreak")]
    [InlineData("\"tab\\there\"", "tab\there")]
    [InlineData("\"back\\\\slash\"", "back\\slash")]
    [InlineData("\"escaped\\\"quote\"", "escaped\"quote")]
    [InlineData("\"cr\\rhere\"", "cr\rhere")]
    public void Lex_StringLiteral_WithEscapes(string text, string expectedValue)
    {
        var token = LexOne(text);
        Assert.Equal(SyntaxTokenKind.StringLiteral, token.Kind);
        Assert.Equal(expectedValue, token.Value);
    }

    // ------------------------------------------------------------------
    // Name Literals
    // ------------------------------------------------------------------

    [Fact]
    public void Lex_NameLiteral()
    {
        var token = LexOne("'myname'");
        Assert.Equal(SyntaxTokenKind.NameLiteral, token.Kind);
        Assert.Equal("'myname'", token.Text);
        Assert.Equal("myname", token.Value);
    }

    [Fact]
    public void Lex_NameLiteral_Empty()
    {
        var token = LexOne("''");
        Assert.Equal(SyntaxTokenKind.NameLiteral, token.Kind);
        Assert.Equal("", token.Value);
    }

    // ------------------------------------------------------------------
    // Keywords
    // ------------------------------------------------------------------

    [Theory]
    [InlineData("class", SyntaxTokenKind.ClassKeyword)]
    [InlineData("struct", SyntaxTokenKind.StructKeyword)]
    [InlineData("enum", SyntaxTokenKind.EnumKeyword)]
    [InlineData("const", SyntaxTokenKind.ConstKeyword)]
    [InlineData("static", SyntaxTokenKind.StaticKeyword)]
    [InlineData("private", SyntaxTokenKind.PrivateKeyword)]
    [InlineData("protected", SyntaxTokenKind.ProtectedKeyword)]
    [InlineData("virtual", SyntaxTokenKind.VirtualKeyword)]
    [InlineData("override", SyntaxTokenKind.OverrideKeyword)]
    [InlineData("final", SyntaxTokenKind.FinalKeyword)]
    [InlineData("native", SyntaxTokenKind.NativeKeyword)]
    [InlineData("actor", SyntaxTokenKind.ActorKeyword)]
    [InlineData("replaces", SyntaxTokenKind.ReplacesKeyword)]
    [InlineData("version", SyntaxTokenKind.VersionKeyword)]
    [InlineData("extend", SyntaxTokenKind.ExtendKeyword)]
    [InlineData("mixin", SyntaxTokenKind.MixinKeyword)]
    [InlineData("abstract", SyntaxTokenKind.AbstractKeyword)]
    [InlineData("deprecated", SyntaxTokenKind.DeprecatedKeyword)]
    [InlineData("readonly", SyntaxTokenKind.ReadOnlyKeyword)]
    [InlineData("default", SyntaxTokenKind.DefaultKeyword)]
    [InlineData("states", SyntaxTokenKind.StatesKeyword)]
    [InlineData("if", SyntaxTokenKind.IfKeyword)]
    [InlineData("else", SyntaxTokenKind.ElseKeyword)]
    [InlineData("while", SyntaxTokenKind.WhileKeyword)]
    [InlineData("do", SyntaxTokenKind.DoKeyword)]
    [InlineData("for", SyntaxTokenKind.ForKeyword)]
    [InlineData("foreach", SyntaxTokenKind.ForEachKeyword)]
    [InlineData("switch", SyntaxTokenKind.SwitchKeyword)]
    [InlineData("case", SyntaxTokenKind.CaseKeyword)]
    [InlineData("break", SyntaxTokenKind.BreakKeyword)]
    [InlineData("continue", SyntaxTokenKind.ContinueKeyword)]
    [InlineData("return", SyntaxTokenKind.ReturnKeyword)]
    [InlineData("goto", SyntaxTokenKind.GotoKeyword)]
    [InlineData("void", SyntaxTokenKind.VoidKeyword)]
    [InlineData("int", SyntaxTokenKind.IntKeyword)]
    [InlineData("uint", SyntaxTokenKind.UIntKeyword)]
    [InlineData("float", SyntaxTokenKind.FloatKeyword)]
    [InlineData("double", SyntaxTokenKind.DoubleKeyword)]
    [InlineData("bool", SyntaxTokenKind.BoolKeyword)]
    [InlineData("string", SyntaxTokenKind.StringKeyword)]
    [InlineData("name", SyntaxTokenKind.NameKeyword)]
    [InlineData("state", SyntaxTokenKind.StateKeyword)]
    [InlineData("color", SyntaxTokenKind.ColorKeyword)]
    [InlineData("sound", SyntaxTokenKind.SoundKeyword)]
    [InlineData("array", SyntaxTokenKind.ArrayKeyword)]
    [InlineData("map", SyntaxTokenKind.MapKeyword)]
    [InlineData("true", SyntaxTokenKind.TrueKeyword)]
    [InlineData("false", SyntaxTokenKind.FalseKeyword)]
    [InlineData("null", SyntaxTokenKind.NullKeyword)]
    [InlineData("stop", SyntaxTokenKind.StopKeyword)]
    [InlineData("wait", SyntaxTokenKind.WaitKeyword)]
    [InlineData("fail", SyntaxTokenKind.FailKeyword)]
    [InlineData("loop", SyntaxTokenKind.LoopKeyword)]
    [InlineData("let", SyntaxTokenKind.LetKeyword)]
    [InlineData("out", SyntaxTokenKind.OutKeyword)]
    [InlineData("in", SyntaxTokenKind.InKeyword)]
    public void Lex_Keyword(string text, SyntaxTokenKind expectedKind)
    {
        var token = LexOne(text);
        Assert.Equal(expectedKind, token.Kind);
        Assert.Equal(text, token.Text);
    }

    [Theory]
    [InlineData("Class", SyntaxTokenKind.ClassKeyword)]
    [InlineData("CLASS", SyntaxTokenKind.ClassKeyword)]
    [InlineData("cLaSs", SyntaxTokenKind.ClassKeyword)]
    [InlineData("ACTOR", SyntaxTokenKind.ActorKeyword)]
    [InlineData("States", SyntaxTokenKind.StatesKeyword)]
    [InlineData("DEFAULT", SyntaxTokenKind.DefaultKeyword)]
    public void Lex_Keyword_CaseInsensitive(string text, SyntaxTokenKind expectedKind)
    {
        var token = LexOne(text);
        Assert.Equal(expectedKind, token.Kind);
        Assert.Equal(text, token.Text);
    }

    // ------------------------------------------------------------------
    // Identifiers
    // ------------------------------------------------------------------

    [Theory]
    [InlineData("myVar")]
    [InlineData("_private")]
    [InlineData("foo123")]
    [InlineData("CamelCase")]
    [InlineData("_")]
    [InlineData("A")]
    public void Lex_Identifier(string text)
    {
        var token = LexOne(text);
        Assert.Equal(SyntaxTokenKind.Identifier, token.Kind);
        Assert.Equal(text, token.Text);
    }

    // ------------------------------------------------------------------
    // Operators
    // ------------------------------------------------------------------

    [Theory]
    [InlineData("+", SyntaxTokenKind.Plus)]
    [InlineData("++", SyntaxTokenKind.PlusPlus)]
    [InlineData("+=", SyntaxTokenKind.PlusEquals)]
    [InlineData("-", SyntaxTokenKind.Minus)]
    [InlineData("--", SyntaxTokenKind.MinusMinus)]
    [InlineData("-=", SyntaxTokenKind.MinusEquals)]
    [InlineData("->", SyntaxTokenKind.Arrow)]
    [InlineData("*", SyntaxTokenKind.Asterisk)]
    [InlineData("*=", SyntaxTokenKind.AsteriskEquals)]
    [InlineData("/", SyntaxTokenKind.Slash)]
    [InlineData("/=", SyntaxTokenKind.SlashEquals)]
    [InlineData("%", SyntaxTokenKind.Percent)]
    [InlineData("%=", SyntaxTokenKind.PercentEquals)]
    [InlineData("&", SyntaxTokenKind.Ampersand)]
    [InlineData("&&", SyntaxTokenKind.AmpersandAmpersand)]
    [InlineData("&=", SyntaxTokenKind.AmpersandEquals)]
    [InlineData("|", SyntaxTokenKind.Pipe)]
    [InlineData("||", SyntaxTokenKind.PipePipe)]
    [InlineData("|=", SyntaxTokenKind.PipeEquals)]
    [InlineData("^", SyntaxTokenKind.Caret)]
    [InlineData("^=", SyntaxTokenKind.CaretEquals)]
    [InlineData("~", SyntaxTokenKind.Tilde)]
    [InlineData("~==", SyntaxTokenKind.ApproxEquals)]
    [InlineData("!", SyntaxTokenKind.Exclamation)]
    [InlineData("!=", SyntaxTokenKind.ExclamationEquals)]
    [InlineData("?", SyntaxTokenKind.Question)]
    [InlineData("=", SyntaxTokenKind.Equals)]
    [InlineData("==", SyntaxTokenKind.EqualsEquals)]
    [InlineData("<", SyntaxTokenKind.LessThan)]
    [InlineData("<=", SyntaxTokenKind.LessThanEquals)]
    [InlineData("<<", SyntaxTokenKind.LessThanLessThan)]
    [InlineData(">", SyntaxTokenKind.GreaterThan)]
    [InlineData(">=", SyntaxTokenKind.GreaterThanEquals)]
    [InlineData(">>", SyntaxTokenKind.GreaterThanGreaterThan)]
    [InlineData(">>>", SyntaxTokenKind.GreaterThanGreaterThanGreaterThan)]
    public void Lex_Operator(string text, SyntaxTokenKind expectedKind)
    {
        var token = LexOne(text);
        Assert.Equal(expectedKind, token.Kind);
        Assert.Equal(text, token.Text);
    }

    // ------------------------------------------------------------------
    // Punctuation
    // ------------------------------------------------------------------

    [Theory]
    [InlineData("(", SyntaxTokenKind.OpenParen)]
    [InlineData(")", SyntaxTokenKind.CloseParen)]
    [InlineData("{", SyntaxTokenKind.OpenBrace)]
    [InlineData("}", SyntaxTokenKind.CloseBrace)]
    [InlineData("[", SyntaxTokenKind.OpenBracket)]
    [InlineData("]", SyntaxTokenKind.CloseBracket)]
    [InlineData(";", SyntaxTokenKind.Semicolon)]
    [InlineData(":", SyntaxTokenKind.Colon)]
    [InlineData("::", SyntaxTokenKind.ColonColon)]
    [InlineData(",", SyntaxTokenKind.Comma)]
    [InlineData(".", SyntaxTokenKind.Dot)]
    [InlineData("..", SyntaxTokenKind.DotDot)]
    [InlineData("#", SyntaxTokenKind.Hash)]
    public void Lex_Punctuation(string text, SyntaxTokenKind expectedKind)
    {
        var token = LexOne(text);
        Assert.Equal(expectedKind, token.Kind);
        Assert.Equal(text, token.Text);
    }

    // ------------------------------------------------------------------
    // Trivia - Single Line Comments
    // ------------------------------------------------------------------

    [Fact]
    public void Lex_SingleLineComment_AsLeadingTrivia()
    {
        var tokens = LexAll("// comment\nidentifier");
        // Should produce: identifier (with leading trivia), EndOfFile
        var identToken = tokens[0];
        Assert.Equal(SyntaxTokenKind.Identifier, identToken.Kind);
        Assert.Equal("identifier", identToken.Text);

        // Leading trivia should contain the comment and the newline
        Assert.True(identToken.LeadingTrivia.Count >= 2,
            $"Expected at least 2 leading trivia items, got {identToken.LeadingTrivia.Count}");

        var commentTrivia = identToken.LeadingTrivia[0];
        Assert.Equal(SyntaxTriviaKind.SingleLineComment, commentTrivia.Kind);
        Assert.Equal("// comment", commentTrivia.Text);

        var newlineTrivia = identToken.LeadingTrivia[1];
        Assert.Equal(SyntaxTriviaKind.EndOfLine, newlineTrivia.Kind);
    }

    // ------------------------------------------------------------------
    // Trivia - Multi-Line Comments
    // ------------------------------------------------------------------

    [Fact]
    public void Lex_MultiLineComment_AsLeadingTrivia()
    {
        var tokens = LexAll("/* comment */identifier");
        var identToken = tokens[0];
        Assert.Equal(SyntaxTokenKind.Identifier, identToken.Kind);
        Assert.Equal("identifier", identToken.Text);

        Assert.Equal(1, identToken.LeadingTrivia.Count);
        Assert.Equal(SyntaxTriviaKind.MultiLineComment, identToken.LeadingTrivia[0].Kind);
        Assert.Equal("/* comment */", identToken.LeadingTrivia[0].Text);
    }

    // ------------------------------------------------------------------
    // Trivia - Whitespace
    // ------------------------------------------------------------------

    [Fact]
    public void Lex_Whitespace_LeadingAndTrailingTrivia()
    {
        var tokens = LexAll("  identifier  ");
        var identToken = tokens[0];
        Assert.Equal(SyntaxTokenKind.Identifier, identToken.Kind);
        Assert.Equal("identifier", identToken.Text);

        // Leading whitespace
        Assert.True(identToken.LeadingTrivia.Count >= 1);
        Assert.Equal(SyntaxTriviaKind.Whitespace, identToken.LeadingTrivia[0].Kind);
        Assert.Equal("  ", identToken.LeadingTrivia[0].Text);

        // Trailing whitespace
        Assert.True(identToken.TrailingTrivia.Count >= 1);
        Assert.Equal(SyntaxTriviaKind.Whitespace, identToken.TrailingTrivia[0].Kind);
        Assert.Equal("  ", identToken.TrailingTrivia[0].Text);
    }

    // ------------------------------------------------------------------
    // Trivia - Newline ends trailing trivia
    // ------------------------------------------------------------------

    [Fact]
    public void Lex_NewlineEndsTrailingTrivia()
    {
        var tokens = LexAll("a \nb");

        // First token: "a" with trailing whitespace " " (newline is NOT trailing)
        var tokenA = tokens[0];
        Assert.Equal(SyntaxTokenKind.Identifier, tokenA.Kind);
        Assert.Equal("a", tokenA.Text);
        Assert.Equal(1, tokenA.TrailingTrivia.Count);
        Assert.Equal(SyntaxTriviaKind.Whitespace, tokenA.TrailingTrivia[0].Kind);

        // Second token: "b" with leading newline
        var tokenB = tokens[1];
        Assert.Equal(SyntaxTokenKind.Identifier, tokenB.Kind);
        Assert.Equal("b", tokenB.Text);
        Assert.True(tokenB.LeadingTrivia.Count >= 1);
        Assert.Equal(SyntaxTriviaKind.EndOfLine, tokenB.LeadingTrivia[0].Kind);
    }

    // ------------------------------------------------------------------
    // Bad Token
    // ------------------------------------------------------------------

    [Fact]
    public void Lex_UnrecognizedCharacter_BadToken()
    {
        var token = LexOne("@");
        Assert.Equal(SyntaxTokenKind.BadToken, token.Kind);
        Assert.Equal("@", token.Text);
    }

    [Fact]
    public void Lex_Backtick_BadToken()
    {
        var token = LexOne("`");
        Assert.Equal(SyntaxTokenKind.BadToken, token.Kind);
    }

    // ------------------------------------------------------------------
    // Multiple tokens in sequence
    // ------------------------------------------------------------------

    [Fact]
    public void Lex_MultipleTokens()
    {
        var tokens = LexAll("int x = 42;");
        // Expect: int, x, =, 42, ;, EndOfFile
        Assert.Equal(6, tokens.Count);
        Assert.Equal(SyntaxTokenKind.IntKeyword, tokens[0].Kind);
        Assert.Equal(SyntaxTokenKind.Identifier, tokens[1].Kind);
        Assert.Equal("x", tokens[1].Text);
        Assert.Equal(SyntaxTokenKind.Equals, tokens[2].Kind);
        Assert.Equal(SyntaxTokenKind.IntegerLiteral, tokens[3].Kind);
        Assert.Equal(42L, tokens[3].Value);
        Assert.Equal(SyntaxTokenKind.Semicolon, tokens[4].Kind);
        Assert.Equal(SyntaxTokenKind.EndOfFile, tokens[5].Kind);
    }

    // ------------------------------------------------------------------
    // Full ZScript snippet
    // ------------------------------------------------------------------

    [Fact]
    public void Lex_FullZScriptSnippet()
    {
        var source = @"class MyActor : Actor
{
    int health;

    default
    {
        health 100;
    }

    states
    {
        Spawn:
            TNT1 A 1;
            stop;
    }
}";
        var tokens = LexAll(source);

        // Verify it lexes without errors (no BadToken)
        foreach (var token in tokens)
        {
            Assert.NotEqual(SyntaxTokenKind.BadToken, token.Kind);
        }

        // Verify the first few tokens
        var nonEof = tokens.Where(t => t.Kind != SyntaxTokenKind.EndOfFile).ToList();
        Assert.True(nonEof.Count > 5);
        Assert.Equal(SyntaxTokenKind.ClassKeyword, nonEof[0].Kind);
        Assert.Equal(SyntaxTokenKind.Identifier, nonEof[1].Kind);
        Assert.Equal("MyActor", nonEof[1].Text);
        Assert.Equal(SyntaxTokenKind.Colon, nonEof[2].Kind);
        Assert.Equal(SyntaxTokenKind.ActorKeyword, nonEof[3].Kind);
        Assert.Equal("Actor", nonEof[3].Text);
    }

    // ------------------------------------------------------------------
    // Token spans
    // ------------------------------------------------------------------

    [Fact]
    public void Lex_TokenSpan_Correct()
    {
        var tokens = LexAll("abc def");
        // "abc" at position 0..3
        Assert.Equal(0, tokens[0].Span.Start);
        Assert.Equal(3, tokens[0].Span.Length);
        // "def" at position 4..7
        Assert.Equal(4, tokens[1].Span.Start);
        Assert.Equal(3, tokens[1].Span.Length);
    }

    // ------------------------------------------------------------------
    // Trailing single-line comment
    // ------------------------------------------------------------------

    [Fact]
    public void Lex_TrailingSingleLineComment()
    {
        var tokens = LexAll("x // comment\ny");

        var tokenX = tokens[0];
        Assert.Equal(SyntaxTokenKind.Identifier, tokenX.Kind);
        Assert.Equal("x", tokenX.Text);

        // Trailing trivia: whitespace + comment
        Assert.Equal(2, tokenX.TrailingTrivia.Count);
        Assert.Equal(SyntaxTriviaKind.Whitespace, tokenX.TrailingTrivia[0].Kind);
        Assert.Equal(SyntaxTriviaKind.SingleLineComment, tokenX.TrailingTrivia[1].Kind);

        // "y" has leading newline trivia
        var tokenY = tokens[1];
        Assert.Equal(SyntaxTokenKind.Identifier, tokenY.Kind);
        Assert.True(tokenY.LeadingTrivia.Count >= 1);
        Assert.Equal(SyntaxTriviaKind.EndOfLine, tokenY.LeadingTrivia[0].Kind);
    }

    // ------------------------------------------------------------------
    // Vector keywords
    // ------------------------------------------------------------------

    [Theory]
    [InlineData("vector2", SyntaxTokenKind.VectorKeyword)]
    [InlineData("vector3", SyntaxTokenKind.VectorKeyword)]
    [InlineData("Vector2", SyntaxTokenKind.VectorKeyword)]
    [InlineData("Vector3", SyntaxTokenKind.VectorKeyword)]
    public void Lex_VectorKeywords(string text, SyntaxTokenKind expectedKind)
    {
        var token = LexOne(text);
        Assert.Equal(expectedKind, token.Kind);
    }

    // ------------------------------------------------------------------
    // DotDot vs Dot
    // ------------------------------------------------------------------

    [Fact]
    public void Lex_DotDot_NotTwoDots()
    {
        var tokens = LexAll("..");
        // Should be a single DotDot token, not two Dot tokens
        Assert.Equal(2, tokens.Count); // DotDot + EOF
        Assert.Equal(SyntaxTokenKind.DotDot, tokens[0].Kind);
        Assert.Equal("..", tokens[0].Text);
    }

    // ------------------------------------------------------------------
    // Preprocessor directive as trivia
    // ------------------------------------------------------------------

    [Fact]
    public void Lex_PreprocessorDirective_AsLeadingTrivia()
    {
        var tokens = LexAll("#include \"zcommon.acs\"\nclass Foo {}");
        // The #include line should be trivia on the class keyword
        var classToken = tokens[0];
        Assert.Equal(SyntaxTokenKind.ClassKeyword, classToken.Kind);

        bool hasPreprocessor = false;
        for (int i = 0; i < classToken.LeadingTrivia.Count; i++)
        {
            if (classToken.LeadingTrivia[i].Kind == SyntaxTriviaKind.PreprocessorDirective)
            {
                hasPreprocessor = true;
                break;
            }
        }
        Assert.True(hasPreprocessor, "Expected preprocessor directive in leading trivia");
    }

    // ------------------------------------------------------------------
    // Edge cases
    // ------------------------------------------------------------------

    [Fact]
    public void Lex_OnlyWhitespace_ReturnsEndOfFile()
    {
        var tokens = LexAll("   \t  ");
        Assert.Single(tokens);
        Assert.Equal(SyntaxTokenKind.EndOfFile, tokens[0].Kind);
        // The whitespace should be leading trivia on the EOF token
        Assert.True(tokens[0].LeadingTrivia.Count >= 1);
        Assert.Equal(SyntaxTriviaKind.Whitespace, tokens[0].LeadingTrivia[0].Kind);
    }

    [Fact]
    public void Lex_OnlyComment_ReturnsEndOfFile()
    {
        var tokens = LexAll("// just a comment");
        Assert.Single(tokens);
        Assert.Equal(SyntaxTokenKind.EndOfFile, tokens[0].Kind);
        Assert.True(tokens[0].LeadingTrivia.Count >= 1);
        Assert.Equal(SyntaxTriviaKind.SingleLineComment, tokens[0].LeadingTrivia[0].Kind);
    }

    [Fact]
    public void Lex_Dot_BeforeDigit_IsFloat()
    {
        var tokens = LexAll(".5");
        Assert.Equal(2, tokens.Count);
        Assert.Equal(SyntaxTokenKind.FloatLiteral, tokens[0].Kind);
        Assert.Equal(0.5, (double)tokens[0].Value!, 10);
    }

    [Fact]
    public void Lex_IntegerFollowedByDotDot_IntegerAndDotDot()
    {
        // e.g. "1..10" should be 1, .., 10
        var tokens = LexAll("1..10");
        Assert.Equal(4, tokens.Count); // 1, .., 10, EOF
        Assert.Equal(SyntaxTokenKind.IntegerLiteral, tokens[0].Kind);
        Assert.Equal(SyntaxTokenKind.DotDot, tokens[1].Kind);
        Assert.Equal(SyntaxTokenKind.IntegerLiteral, tokens[2].Kind);
    }
}
