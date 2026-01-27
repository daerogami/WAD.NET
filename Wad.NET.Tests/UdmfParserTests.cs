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

        #region Namespace Variation Tests

        [Theory]
        [InlineData("doom")]
        [InlineData("Doom")]
        [InlineData("DOOM")]
        public void UdmfParser_ShouldParseDoomNamespace(string ns)
        {
            var parser = new UdmfParser();
            var map = parser.Parse($"namespace = \"{ns}\";");

            Assert.Equal(ns, map.Namespace);
        }

        [Theory]
        [InlineData("zdoom")]
        [InlineData("ZDoom")]
        [InlineData("ZDOOM")]
        public void UdmfParser_ShouldParseZDoomNamespace(string ns)
        {
            var parser = new UdmfParser();
            var map = parser.Parse($"namespace = \"{ns}\";");

            Assert.Equal(ns, map.Namespace);
        }

        [Theory]
        [InlineData("heretic")]
        [InlineData("Heretic")]
        [InlineData("HERETIC")]
        public void UdmfParser_ShouldParseHereticNamespace(string ns)
        {
            var parser = new UdmfParser();
            var map = parser.Parse($"namespace = \"{ns}\";");

            Assert.Equal(ns, map.Namespace);
        }

        [Theory]
        [InlineData("hexen")]
        [InlineData("Hexen")]
        [InlineData("HEXEN")]
        public void UdmfParser_ShouldParseHexenNamespace(string ns)
        {
            var parser = new UdmfParser();
            var map = parser.Parse($"namespace = \"{ns}\";");

            Assert.Equal(ns, map.Namespace);
        }

        [Theory]
        [InlineData("strife")]
        [InlineData("Strife")]
        [InlineData("STRIFE")]
        public void UdmfParser_ShouldParseStrifeNamespace(string ns)
        {
            var parser = new UdmfParser();
            var map = parser.Parse($"namespace = \"{ns}\";");

            Assert.Equal(ns, map.Namespace);
        }

        [Theory]
        [InlineData("eternity")]
        [InlineData("Eternity")]
        [InlineData("ETERNITY")]
        public void UdmfParser_ShouldParseEternityNamespace(string ns)
        {
            var parser = new UdmfParser();
            var map = parser.Parse($"namespace = \"{ns}\";");

            Assert.Equal(ns, map.Namespace);
        }

        [Fact]
        public void UdmfParser_DefaultNamespace_ShouldBeDoom()
        {
            var parser = new UdmfParser();
            var map = parser.Parse("vertex { x = 0; y = 0; }");

            Assert.Equal("doom", map.Namespace);
        }

        [Fact]
        public void UdmfParser_ZDoomNamespace_ShouldParseExtendedVertexFields()
        {
            var parser = new UdmfParser();
            var map = parser.Parse(@"
                namespace = ""zdoom"";
                vertex { x = 64; y = 128; zfloor = -32.0; zceiling = 256.0; }
            ");

            Assert.Equal("zdoom", map.Namespace);
            Assert.Single(map.Vertices);
            Assert.Equal(-32.0, map.Vertices[0].ZFloor);
            Assert.Equal(256.0, map.Vertices[0].ZCeiling);
        }

        [Fact]
        public void UdmfParser_ZDoomNamespace_ShouldParseExtendedLinedefFields()
        {
            var parser = new UdmfParser();
            var map = parser.Parse(@"
                namespace = ""zdoom"";
                linedef {
                    v1 = 0;
                    v2 = 1;
                    sidefront = 0;
                    blocking = true;
                    repeatspecial = true;
                    activation = 1024;
                    blockplayers = true;
                    blockeverything = false;
                }
            ");

            Assert.Equal("zdoom", map.Namespace);
            Assert.Single(map.Linedefs);
            Assert.True(map.Linedefs[0].Repeatspecial);
            Assert.Equal(1024, map.Linedefs[0].Activation);
            Assert.True(map.Linedefs[0].Blockplayers);
            Assert.False(map.Linedefs[0].Blockeverything);
        }

        [Fact]
        public void UdmfParser_ZDoomNamespace_ShouldParseExtendedSidedefFields()
        {
            var parser = new UdmfParser();
            var map = parser.Parse(@"
                namespace = ""zdoom"";
                sidedef {
                    sector = 0;
                    texturemiddle = ""STARTAN1"";
                    light = 64;
                    lightabsolute = true;
                    scalex_top = 2.0;
                    scaley_top = 1.5;
                    scalex_mid = 1.0;
                    scaley_mid = 1.0;
                    scalex_bottom = 0.5;
                    scaley_bottom = 0.5;
                }
            ");

            Assert.Equal("zdoom", map.Namespace);
            Assert.Single(map.Sidedefs);
            Assert.Equal(64, map.Sidedefs[0].Light);
            Assert.True(map.Sidedefs[0].LightAbsolute);
            Assert.Equal(2.0, map.Sidedefs[0].ScaleXTop);
            Assert.Equal(1.5, map.Sidedefs[0].ScaleYTop);
        }

        [Fact]
        public void UdmfParser_ZDoomNamespace_ShouldParseExtendedSectorFields()
        {
            var parser = new UdmfParser();
            var map = parser.Parse(@"
                namespace = ""zdoom"";
                sector {
                    heightfloor = 0;
                    heightceiling = 128;
                    texturefloor = ""FLOOR4_8"";
                    textureceiling = ""CEIL3_5"";
                    lightlevel = 160;
                    xpanningfloor = 16.0;
                    ypanningfloor = 32.0;
                    xpanningceiling = 8.0;
                    ypanningceiling = 4.0;
                    xscalefloor = 1.5;
                    yscalefloor = 2.0;
                    rotationfloor = 45.0;
                    rotationceiling = 90.0;
                    lightfloor = 32;
                    lightceiling = -16;
                    lightfloorabsolute = false;
                    lightceilingabsolute = true;
                    gravity = 0.5;
                }
            ");

            Assert.Equal("zdoom", map.Namespace);
            Assert.Single(map.Sectors);
            Assert.Equal(16.0, map.Sectors[0].XPanningFloor);
            Assert.Equal(32.0, map.Sectors[0].YPanningFloor);
            Assert.Equal(1.5, map.Sectors[0].XScaleFloor);
            Assert.Equal(45.0, map.Sectors[0].RotationFloor);
            Assert.Equal(32, map.Sectors[0].LightFloor);
            Assert.Equal(0.5, map.Sectors[0].Gravity);
        }

        [Fact]
        public void UdmfParser_ZDoomNamespace_ShouldParseExtendedThingFields()
        {
            var parser = new UdmfParser();
            var map = parser.Parse(@"
                namespace = ""zdoom"";
                thing {
                    id = 100;
                    x = 256;
                    y = 128;
                    height = 32.0;
                    angle = 90;
                    type = 1;
                    skill1 = true;
                    skill2 = true;
                    skill3 = true;
                    skill4 = true;
                    skill5 = true;
                    ambush = true;
                    single = true;
                    dm = false;
                    coop = true;
                    friend = true;
                    dormant = false;
                    class1 = true;
                    class2 = false;
                    class3 = true;
                    gravity = 0.8;
                    health = 200.0;
                    scalex = 2.0;
                    scaley = 1.5;
                    renderstyle = ""translucent"";
                    alpha = 0.75;
                    fillcolor = 16711680;
                }
            ");

            Assert.Equal("zdoom", map.Namespace);
            Assert.Single(map.Things);
            var thing = map.Things[0];
            Assert.Equal(100, thing.Id);
            Assert.Equal(32.0, thing.Height);
            Assert.True(thing.Friend);
            Assert.Equal(0.8, thing.Gravity);
            Assert.Equal(200.0, thing.Health);
            Assert.Equal(2.0, thing.ScaleX);
            Assert.Equal("translucent", thing.RenderStyle);
            Assert.Equal(0.75, thing.Alpha);
            Assert.Equal(16711680, thing.FillColor);
        }

        [Fact]
        public void UdmfParser_HexenNamespace_ShouldParseClassFlags()
        {
            var parser = new UdmfParser();
            var map = parser.Parse(@"
                namespace = ""hexen"";
                thing {
                    x = 64;
                    y = 64;
                    type = 1;
                    class1 = true;
                    class2 = true;
                    class3 = true;
                    dormant = true;
                }
            ");

            Assert.Equal("hexen", map.Namespace);
            Assert.Single(map.Things);
            Assert.True(map.Things[0].Class1);
            Assert.True(map.Things[0].Class2);
            Assert.True(map.Things[0].Class3);
            Assert.True(map.Things[0].Dormant);
        }

        [Fact]
        public void UdmfParser_HexenNamespace_ShouldParseThingSpecials()
        {
            var parser = new UdmfParser();
            var map = parser.Parse(@"
                namespace = ""hexen"";
                thing {
                    x = 64;
                    y = 64;
                    type = 1;
                    special = 80;
                    arg0 = 10;
                    arg1 = 20;
                    arg2 = 30;
                    arg3 = 40;
                    arg4 = 50;
                }
            ");

            Assert.Single(map.Things);
            Assert.Equal(80, map.Things[0].Special);
            Assert.Equal(10, map.Things[0].Arg0);
            Assert.Equal(20, map.Things[0].Arg1);
            Assert.Equal(30, map.Things[0].Arg2);
            Assert.Equal(40, map.Things[0].Arg3);
            Assert.Equal(50, map.Things[0].Arg4);
        }

        [Fact]
        public void UdmfParser_HexenNamespace_ShouldParseLinedefSpecials()
        {
            var parser = new UdmfParser();
            var map = parser.Parse(@"
                namespace = ""hexen"";
                linedef {
                    v1 = 0;
                    v2 = 1;
                    sidefront = 0;
                    special = 70;
                    arg0 = 1;
                    arg1 = 2;
                    arg2 = 3;
                    arg3 = 4;
                    arg4 = 5;
                }
            ");

            Assert.Single(map.Linedefs);
            Assert.Equal(70, map.Linedefs[0].Special);
            Assert.Equal(1, map.Linedefs[0].Arg0);
            Assert.Equal(2, map.Linedefs[0].Arg1);
        }

        [Fact]
        public void UdmfParser_DoomNamespace_ShouldParseBasicMap()
        {
            var parser = new UdmfParser();
            var map = parser.Parse(@"
                namespace = ""doom"";

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

            Assert.Equal("doom", map.Namespace);
            Assert.Equal(4, map.VertexCount);
            Assert.Equal(4, map.LinedefCount);
            Assert.Equal(4, map.SidedefCount);
            Assert.Equal(1, map.SectorCount);
            Assert.Equal(1, map.ThingCount);
        }

        [Fact]
        public void UdmfParser_StrifeNamespace_ShouldParseBasicMap()
        {
            var parser = new UdmfParser();
            var map = parser.Parse(@"
                namespace = ""strife"";
                vertex { x = 0; y = 0; }
                thing { x = 64; y = 64; type = 1; }
            ");

            Assert.Equal("strife", map.Namespace);
            Assert.Single(map.Vertices);
            Assert.Single(map.Things);
        }

        [Fact]
        public void UdmfParser_EternityNamespace_ShouldParseBasicMap()
        {
            var parser = new UdmfParser();
            var map = parser.Parse(@"
                namespace = ""eternity"";
                vertex { x = 0; y = 0; }
                thing { x = 64; y = 64; type = 1; }
            ");

            Assert.Equal("eternity", map.Namespace);
            Assert.Single(map.Vertices);
            Assert.Single(map.Things);
        }

        [Fact]
        public void UdmfParser_CustomNamespace_ShouldAccept()
        {
            var parser = new UdmfParser();
            var map = parser.Parse(@"
                namespace = ""mycustomport"";
                vertex { x = 0; y = 0; }
            ");

            Assert.Equal("mycustomport", map.Namespace);
        }

        [Fact]
        public void UdmfParser_MultipleNamespaceAssignments_ShouldUseLastOne()
        {
            var parser = new UdmfParser();
            var map = parser.Parse(@"
                namespace = ""doom"";
                namespace = ""zdoom"";
                vertex { x = 0; y = 0; }
            ");

            Assert.Equal("zdoom", map.Namespace);
        }

        [Fact]
        public void UdmfParser_NamespaceWithComments_ShouldParse()
        {
            var parser = new UdmfParser();
            var map = parser.Parse(@"
                // Namespace declaration
                namespace = ""zdoom""; // ZDoom extended features
                /* multi-line
                   comment */
                vertex { x = 0; y = 0; }
            ");

            Assert.Equal("zdoom", map.Namespace);
        }

        #endregion
    }
}
