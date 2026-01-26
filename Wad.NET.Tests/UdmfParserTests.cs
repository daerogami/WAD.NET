using System;
using Xunit;
using WAD.NET.UDMF;

namespace WAD.NET.Tests
{
    public class UdmfParserTests
    {
        #region Lexer Tests

        [Fact]
        public void UdmfLexer_ShouldTokenizeIdentifier()
        {
            var lexer = new UdmfLexer("vertex");
            var tokens = new System.Collections.Generic.List<UdmfToken>(lexer.Tokenize());

            Assert.Equal(2, tokens.Count);
            Assert.Equal(UdmfTokenType.Identifier, tokens[0].Type);
            Assert.Equal("vertex", tokens[0].Value);
            Assert.Equal(UdmfTokenType.EndOfFile, tokens[1].Type);
        }

        [Fact]
        public void UdmfLexer_ShouldTokenizeInteger()
        {
            var lexer = new UdmfLexer("123");
            var tokens = new System.Collections.Generic.List<UdmfToken>(lexer.Tokenize());

            Assert.Equal(UdmfTokenType.Integer, tokens[0].Type);
            Assert.Equal("123", tokens[0].Value);
        }

        [Fact]
        public void UdmfLexer_ShouldTokenizeNegativeNumber()
        {
            var lexer = new UdmfLexer("-456");
            var tokens = new System.Collections.Generic.List<UdmfToken>(lexer.Tokenize());

            Assert.Equal(UdmfTokenType.Integer, tokens[0].Type);
            Assert.Equal("-456", tokens[0].Value);
        }

        [Fact]
        public void UdmfLexer_ShouldTokenizeFloat()
        {
            var lexer = new UdmfLexer("3.14159");
            var tokens = new System.Collections.Generic.List<UdmfToken>(lexer.Tokenize());

            Assert.Equal(UdmfTokenType.Float, tokens[0].Type);
            Assert.Equal("3.14159", tokens[0].Value);
        }

        [Fact]
        public void UdmfLexer_ShouldTokenizeScientificNotation()
        {
            var lexer = new UdmfLexer("1.5e10");
            var tokens = new System.Collections.Generic.List<UdmfToken>(lexer.Tokenize());

            Assert.Equal(UdmfTokenType.Float, tokens[0].Type);
            Assert.Equal("1.5e10", tokens[0].Value);
        }

        [Fact]
        public void UdmfLexer_ShouldTokenizeString()
        {
            var lexer = new UdmfLexer("\"hello world\"");
            var tokens = new System.Collections.Generic.List<UdmfToken>(lexer.Tokenize());

            Assert.Equal(UdmfTokenType.String, tokens[0].Type);
            Assert.Equal("hello world", tokens[0].Value);
        }

        [Fact]
        public void UdmfLexer_ShouldHandleEscapedCharacters()
        {
            var lexer = new UdmfLexer("\"line1\\nline2\"");
            var tokens = new System.Collections.Generic.List<UdmfToken>(lexer.Tokenize());

            Assert.Equal(UdmfTokenType.String, tokens[0].Type);
            Assert.Equal("line1\nline2", tokens[0].Value);
        }

        [Fact]
        public void UdmfLexer_ShouldTokenizeBoolean()
        {
            var lexer = new UdmfLexer("true false");
            var tokens = new System.Collections.Generic.List<UdmfToken>(lexer.Tokenize());

            Assert.Equal(UdmfTokenType.Boolean, tokens[0].Type);
            Assert.Equal("true", tokens[0].Value);
            Assert.Equal(UdmfTokenType.Boolean, tokens[1].Type);
            Assert.Equal("false", tokens[1].Value);
        }

        [Fact]
        public void UdmfLexer_ShouldTokenizeSymbols()
        {
            var lexer = new UdmfLexer("{ } = ;");
            var tokens = new System.Collections.Generic.List<UdmfToken>(lexer.Tokenize());

            Assert.Equal(UdmfTokenType.OpenBrace, tokens[0].Type);
            Assert.Equal(UdmfTokenType.CloseBrace, tokens[1].Type);
            Assert.Equal(UdmfTokenType.Equals, tokens[2].Type);
            Assert.Equal(UdmfTokenType.Semicolon, tokens[3].Type);
        }

        [Fact]
        public void UdmfLexer_ShouldSkipSingleLineComments()
        {
            var lexer = new UdmfLexer("vertex // this is a comment\nthing");
            var tokens = new System.Collections.Generic.List<UdmfToken>(lexer.Tokenize());

            Assert.Equal(UdmfTokenType.Identifier, tokens[0].Type);
            Assert.Equal("vertex", tokens[0].Value);
            Assert.Equal(UdmfTokenType.Identifier, tokens[1].Type);
            Assert.Equal("thing", tokens[1].Value);
        }

        [Fact]
        public void UdmfLexer_ShouldSkipMultiLineComments()
        {
            var lexer = new UdmfLexer("vertex /* this is\na comment */ thing");
            var tokens = new System.Collections.Generic.List<UdmfToken>(lexer.Tokenize());

            Assert.Equal(UdmfTokenType.Identifier, tokens[0].Type);
            Assert.Equal("vertex", tokens[0].Value);
            Assert.Equal(UdmfTokenType.Identifier, tokens[1].Type);
            Assert.Equal("thing", tokens[1].Value);
        }

        [Fact]
        public void UdmfLexer_ShouldTrackLineNumbers()
        {
            var lexer = new UdmfLexer("a\nb\nc");
            var tokens = new System.Collections.Generic.List<UdmfToken>(lexer.Tokenize());

            Assert.Equal(1, tokens[0].Line);
            Assert.Equal(2, tokens[1].Line);
            Assert.Equal(3, tokens[2].Line);
        }

        #endregion

        #region Parser Tests

        [Fact]
        public void UdmfParser_ShouldParseNamespace()
        {
            var parser = new UdmfParser();
            var map = parser.Parse("namespace = \"doom\";");

            Assert.Equal("doom", map.Namespace);
        }

        [Fact]
        public void UdmfParser_ShouldParseVertex()
        {
            var parser = new UdmfParser();
            var map = parser.Parse(@"
                vertex { x = 64.0; y = 128.0; }
            ");

            Assert.Single(map.Vertices);
            Assert.Equal(64.0, map.Vertices[0].X);
            Assert.Equal(128.0, map.Vertices[0].Y);
        }

        [Fact]
        public void UdmfParser_ShouldParseMultipleVertices()
        {
            var parser = new UdmfParser();
            var map = parser.Parse(@"
                vertex { x = 0; y = 0; }
                vertex { x = 64; y = 0; }
                vertex { x = 64; y = 64; }
                vertex { x = 0; y = 64; }
            ");

            Assert.Equal(4, map.Vertices.Count);
        }

        [Fact]
        public void UdmfParser_ShouldParseLinedef()
        {
            var parser = new UdmfParser();
            var map = parser.Parse(@"
                linedef {
                    v1 = 0;
                    v2 = 1;
                    sidefront = 0;
                    blocking = true;
                    twosided = false;
                }
            ");

            Assert.Single(map.Linedefs);
            Assert.Equal(0, map.Linedefs[0].V1);
            Assert.Equal(1, map.Linedefs[0].V2);
            Assert.Equal(0, map.Linedefs[0].SideFront);
            Assert.True(map.Linedefs[0].Blocking);
            Assert.False(map.Linedefs[0].TwoSided);
        }

        [Fact]
        public void UdmfParser_ShouldParseSidedef()
        {
            var parser = new UdmfParser();
            var map = parser.Parse(@"
                sidedef {
                    sector = 0;
                    offsetx = 16.0;
                    offsety = 8.0;
                    texturetop = ""STARTAN1"";
                    texturemiddle = ""DOORTRAK"";
                    texturebottom = ""-"";
                }
            ");

            Assert.Single(map.Sidedefs);
            Assert.Equal(0, map.Sidedefs[0].Sector);
            Assert.Equal(16.0, map.Sidedefs[0].OffsetX);
            Assert.Equal(8.0, map.Sidedefs[0].OffsetY);
            Assert.Equal("STARTAN1", map.Sidedefs[0].TextureTop);
            Assert.Equal("DOORTRAK", map.Sidedefs[0].TextureMiddle);
            Assert.Equal("-", map.Sidedefs[0].TextureBottom);
        }

        [Fact]
        public void UdmfParser_ShouldParseSector()
        {
            var parser = new UdmfParser();
            var map = parser.Parse(@"
                sector {
                    heightfloor = 0;
                    heightceiling = 128;
                    texturefloor = ""FLOOR4_8"";
                    textureceiling = ""CEIL3_5"";
                    lightlevel = 192;
                    special = 0;
                }
            ");

            Assert.Single(map.Sectors);
            Assert.Equal(0, map.Sectors[0].HeightFloor);
            Assert.Equal(128, map.Sectors[0].HeightCeiling);
            Assert.Equal("FLOOR4_8", map.Sectors[0].TextureFloor);
            Assert.Equal("CEIL3_5", map.Sectors[0].TextureCeiling);
            Assert.Equal(192, map.Sectors[0].LightLevel);
        }

        [Fact]
        public void UdmfParser_ShouldParseThing()
        {
            var parser = new UdmfParser();
            var map = parser.Parse(@"
                thing {
                    x = 256.0;
                    y = 128.0;
                    height = 0;
                    angle = 90;
                    type = 1;
                    skill1 = true;
                    skill2 = true;
                    skill3 = true;
                    skill4 = true;
                    skill5 = true;
                    single = true;
                    ambush = false;
                }
            ");

            Assert.Single(map.Things);
            Assert.Equal(256.0, map.Things[0].X);
            Assert.Equal(128.0, map.Things[0].Y);
            Assert.Equal(90, map.Things[0].Angle);
            Assert.Equal(1, map.Things[0].Type);
            Assert.True(map.Things[0].Skill1);
            Assert.True(map.Things[0].Single);
            Assert.False(map.Things[0].Ambush);
        }

        [Fact]
        public void UdmfParser_ShouldParseCompleteMap()
        {
            var parser = new UdmfParser();
            var map = parser.Parse(@"
                namespace = ""zdoom"";

                // A simple square room
                vertex { x = 0; y = 0; }
                vertex { x = 128; y = 0; }
                vertex { x = 128; y = 128; }
                vertex { x = 0; y = 128; }

                linedef { v1 = 0; v2 = 1; sidefront = 0; blocking = true; }
                linedef { v1 = 1; v2 = 2; sidefront = 1; blocking = true; }
                linedef { v1 = 2; v2 = 3; sidefront = 2; blocking = true; }
                linedef { v1 = 3; v2 = 0; sidefront = 3; blocking = true; }

                sidedef { sector = 0; texturemiddle = ""STARTAN1""; }
                sidedef { sector = 0; texturemiddle = ""STARTAN1""; }
                sidedef { sector = 0; texturemiddle = ""STARTAN1""; }
                sidedef { sector = 0; texturemiddle = ""STARTAN1""; }

                sector {
                    heightfloor = 0;
                    heightceiling = 128;
                    texturefloor = ""FLOOR4_8"";
                    textureceiling = ""CEIL3_5"";
                    lightlevel = 160;
                }

                thing { x = 64; y = 64; type = 1; angle = 90; }
            ");

            Assert.Equal("zdoom", map.Namespace);
            Assert.Equal(4, map.VertexCount);
            Assert.Equal(4, map.LinedefCount);
            Assert.Equal(4, map.SidedefCount);
            Assert.Equal(1, map.SectorCount);
            Assert.Equal(1, map.ThingCount);
        }

        [Fact]
        public void UdmfParser_ShouldHandleZDoomExtensions()
        {
            var parser = new UdmfParser();
            var map = parser.Parse(@"
                vertex { x = 0; y = 0; zfloor = -16.0; zceiling = 144.0; }
            ");

            Assert.Single(map.Vertices);
            Assert.Equal(-16.0, map.Vertices[0].ZFloor);
            Assert.Equal(144.0, map.Vertices[0].ZCeiling);
        }

        [Fact]
        public void UdmfParser_ShouldThrowOnInvalidSyntax()
        {
            var parser = new UdmfParser();

            Assert.Throws<FormatException>(() => parser.Parse("vertex { x = }"));
        }

        [Fact]
        public void UdmfParser_ShouldHandleIntegerCoordinates()
        {
            var parser = new UdmfParser();
            var map = parser.Parse("vertex { x = 100; y = 200; }");

            Assert.Equal(100.0, map.Vertices[0].X);
            Assert.Equal(200.0, map.Vertices[0].Y);
        }

        #endregion
    }
}
