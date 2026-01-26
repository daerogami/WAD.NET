using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WAD.NET.UDMF
{
    /// <summary>
    /// Parser for UDMF text format.
    /// </summary>
    public class UdmfParser
    {
        private Queue<UdmfToken> _tokens;
        private UdmfToken _current;

        /// <summary>
        /// Parses UDMF text into a map structure.
        /// </summary>
        /// <param name="text">The UDMF text to parse.</param>
        /// <returns>The parsed map.</returns>
        public UdmfMap Parse(string text)
        {
            var lexer = new UdmfLexer(text);
            _tokens = new Queue<UdmfToken>(lexer.Tokenize());
            _current = _tokens.Dequeue();

            var map = new UdmfMap();

            while (_current.Type != UdmfTokenType.EndOfFile)
            {
                var identifier = Expect(UdmfTokenType.Identifier);

                if (_current.Type == UdmfTokenType.OpenBrace)
                {
                    ParseBlock(map, identifier.Value);
                }
                else
                {
                    // Global assignment (like namespace = "doom";)
                    ParseGlobalAssignment(map, identifier.Value);
                }
            }

            return map;
        }

        private void ParseGlobalAssignment(UdmfMap map, string key)
        {
            Expect(UdmfTokenType.Equals);
            var value = ReadValue();
            Expect(UdmfTokenType.Semicolon);

            if (key.Equals("namespace", StringComparison.OrdinalIgnoreCase))
            {
                map.Namespace = value?.ToString() ?? "doom";
            }
        }

        private void ParseBlock(UdmfMap map, string blockType)
        {
            Expect(UdmfTokenType.OpenBrace);

            var properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            while (_current.Type != UdmfTokenType.CloseBrace)
            {
                var key = Expect(UdmfTokenType.Identifier);
                Expect(UdmfTokenType.Equals);
                var value = ReadValue();
                Expect(UdmfTokenType.Semicolon);
                properties[key.Value] = value;
            }

            Expect(UdmfTokenType.CloseBrace);

            switch (blockType.ToLowerInvariant())
            {
                case "vertex":
                    map.Vertices.Add(CreateVertex(properties));
                    break;
                case "linedef":
                    map.Linedefs.Add(CreateLinedef(properties));
                    break;
                case "sidedef":
                    map.Sidedefs.Add(CreateSidedef(properties));
                    break;
                case "sector":
                    map.Sectors.Add(CreateSector(properties));
                    break;
                case "thing":
                    map.Things.Add(CreateThing(properties));
                    break;
            }
        }

        private object ReadValue()
        {
            var token = _current;
            Advance();

            return token.Type switch
            {
                UdmfTokenType.Integer => int.Parse(token.Value, CultureInfo.InvariantCulture),
                UdmfTokenType.Float => double.Parse(token.Value, CultureInfo.InvariantCulture),
                UdmfTokenType.String => token.Value,
                UdmfTokenType.Boolean => token.Value == "true",
                UdmfTokenType.Identifier => token.Value, // For unquoted string values
                _ => throw new FormatException($"Unexpected token type {token.Type} at {token.Line}:{token.Column}")
            };
        }

        private UdmfToken Expect(UdmfTokenType type)
        {
            if (_current.Type != type)
            {
                throw new FormatException(
                    $"Expected {type} but got {_current.Type} at line {_current.Line}, column {_current.Column}");
            }

            var token = _current;
            Advance();
            return token;
        }

        private void Advance()
        {
            if (_tokens.Count > 0)
            {
                _current = _tokens.Dequeue();
            }
        }

        private static UdmfVertex CreateVertex(Dictionary<string, object> props)
        {
            var vertex = new UdmfVertex();
            if (props.TryGetValue("x", out var x)) vertex.X = ToDouble(x);
            if (props.TryGetValue("y", out var y)) vertex.Y = ToDouble(y);
            if (props.TryGetValue("zfloor", out var zf)) vertex.ZFloor = ToDouble(zf);
            if (props.TryGetValue("zceiling", out var zc)) vertex.ZCeiling = ToDouble(zc);
            return vertex;
        }

        private static UdmfLinedef CreateLinedef(Dictionary<string, object> props)
        {
            var linedef = new UdmfLinedef();
            if (props.TryGetValue("id", out var id)) linedef.Id = ToInt(id);
            if (props.TryGetValue("v1", out var v1)) linedef.V1 = ToInt(v1);
            if (props.TryGetValue("v2", out var v2)) linedef.V2 = ToInt(v2);
            if (props.TryGetValue("blocking", out var bl)) linedef.Blocking = ToBool(bl);
            if (props.TryGetValue("blockmonsters", out var bm)) linedef.BlockMonsters = ToBool(bm);
            if (props.TryGetValue("twosided", out var ts)) linedef.TwoSided = ToBool(ts);
            if (props.TryGetValue("dontpegtop", out var dpt)) linedef.DontPegTop = ToBool(dpt);
            if (props.TryGetValue("dontpegbottom", out var dpb)) linedef.DontPegBottom = ToBool(dpb);
            if (props.TryGetValue("secret", out var sec)) linedef.Secret = ToBool(sec);
            if (props.TryGetValue("blocksound", out var bs)) linedef.BlockSound = ToBool(bs);
            if (props.TryGetValue("dontdraw", out var dd)) linedef.DontDraw = ToBool(dd);
            if (props.TryGetValue("mapped", out var mp)) linedef.Mapped = ToBool(mp);
            if (props.TryGetValue("passuse", out var pu)) linedef.PassUse = ToBool(pu);
            if (props.TryGetValue("special", out var sp)) linedef.Special = ToInt(sp);
            if (props.TryGetValue("arg0", out var a0)) linedef.Arg0 = ToInt(a0);
            if (props.TryGetValue("arg1", out var a1)) linedef.Arg1 = ToInt(a1);
            if (props.TryGetValue("arg2", out var a2)) linedef.Arg2 = ToInt(a2);
            if (props.TryGetValue("arg3", out var a3)) linedef.Arg3 = ToInt(a3);
            if (props.TryGetValue("arg4", out var a4)) linedef.Arg4 = ToInt(a4);
            if (props.TryGetValue("sidefront", out var sf)) linedef.SideFront = ToInt(sf);
            if (props.TryGetValue("sideback", out var sb)) linedef.SideBack = ToInt(sb);
            if (props.TryGetValue("comment", out var cm)) linedef.Comment = cm?.ToString();
            if (props.TryGetValue("repeatspecial", out var rs)) linedef.Repeatspecial = ToBool(rs);
            if (props.TryGetValue("activation", out var act)) linedef.Activation = ToInt(act);
            if (props.TryGetValue("blockplayers", out var bp)) linedef.Blockplayers = ToBool(bp);
            if (props.TryGetValue("blockeverything", out var be)) linedef.Blockeverything = ToBool(be);
            return linedef;
        }

        private static UdmfSidedef CreateSidedef(Dictionary<string, object> props)
        {
            var sidedef = new UdmfSidedef();
            if (props.TryGetValue("sector", out var sec)) sidedef.Sector = ToInt(sec);
            if (props.TryGetValue("offsetx", out var ox)) sidedef.OffsetX = ToDouble(ox);
            if (props.TryGetValue("offsety", out var oy)) sidedef.OffsetY = ToDouble(oy);
            if (props.TryGetValue("texturetop", out var tt)) sidedef.TextureTop = tt?.ToString() ?? "-";
            if (props.TryGetValue("texturebottom", out var tb)) sidedef.TextureBottom = tb?.ToString() ?? "-";
            if (props.TryGetValue("texturemiddle", out var tm)) sidedef.TextureMiddle = tm?.ToString() ?? "-";
            if (props.TryGetValue("light", out var lt)) sidedef.Light = ToInt(lt);
            if (props.TryGetValue("lightabsolute", out var la)) sidedef.LightAbsolute = ToBool(la);
            if (props.TryGetValue("scalex_top", out var sxt)) sidedef.ScaleXTop = ToDouble(sxt);
            if (props.TryGetValue("scaley_top", out var syt)) sidedef.ScaleYTop = ToDouble(syt);
            if (props.TryGetValue("scalex_mid", out var sxm)) sidedef.ScaleXMid = ToDouble(sxm);
            if (props.TryGetValue("scaley_mid", out var sym)) sidedef.ScaleYMid = ToDouble(sym);
            if (props.TryGetValue("scalex_bottom", out var sxb)) sidedef.ScaleXBottom = ToDouble(sxb);
            if (props.TryGetValue("scaley_bottom", out var syb)) sidedef.ScaleYBottom = ToDouble(syb);
            return sidedef;
        }

        private static UdmfSector CreateSector(Dictionary<string, object> props)
        {
            var sector = new UdmfSector();
            if (props.TryGetValue("id", out var id)) sector.Id = ToInt(id);
            if (props.TryGetValue("heightfloor", out var hf)) sector.HeightFloor = ToInt(hf);
            if (props.TryGetValue("heightceiling", out var hc)) sector.HeightCeiling = ToInt(hc);
            if (props.TryGetValue("texturefloor", out var tf)) sector.TextureFloor = tf?.ToString();
            if (props.TryGetValue("textureceiling", out var tc)) sector.TextureCeiling = tc?.ToString();
            if (props.TryGetValue("lightlevel", out var ll)) sector.LightLevel = ToInt(ll);
            if (props.TryGetValue("special", out var sp)) sector.Special = ToInt(sp);
            if (props.TryGetValue("comment", out var cm)) sector.Comment = cm?.ToString();
            if (props.TryGetValue("xpanningfloor", out var xpf)) sector.XPanningFloor = ToDouble(xpf);
            if (props.TryGetValue("ypanningfloor", out var ypf)) sector.YPanningFloor = ToDouble(ypf);
            if (props.TryGetValue("xpanningceiling", out var xpc)) sector.XPanningCeiling = ToDouble(xpc);
            if (props.TryGetValue("ypanningceiling", out var ypc)) sector.YPanningCeiling = ToDouble(ypc);
            if (props.TryGetValue("xscalefloor", out var xsf)) sector.XScaleFloor = ToDouble(xsf);
            if (props.TryGetValue("yscalefloor", out var ysf)) sector.YScaleFloor = ToDouble(ysf);
            if (props.TryGetValue("xscaleceiling", out var xsc)) sector.XScaleCeiling = ToDouble(xsc);
            if (props.TryGetValue("yscaleceiling", out var ysc)) sector.YScaleCeiling = ToDouble(ysc);
            if (props.TryGetValue("rotationfloor", out var rf)) sector.RotationFloor = ToDouble(rf);
            if (props.TryGetValue("rotationceiling", out var rc)) sector.RotationCeiling = ToDouble(rc);
            if (props.TryGetValue("lightfloor", out var lf)) sector.LightFloor = ToInt(lf);
            if (props.TryGetValue("lightceiling", out var lc)) sector.LightCeiling = ToInt(lc);
            if (props.TryGetValue("lightfloorabsolute", out var lfa)) sector.LightFloorAbsolute = ToBool(lfa);
            if (props.TryGetValue("lightceilingabsolute", out var lca)) sector.LightCeilingAbsolute = ToBool(lca);
            if (props.TryGetValue("gravity", out var gr)) sector.Gravity = ToDouble(gr);
            return sector;
        }

        private static UdmfThing CreateThing(Dictionary<string, object> props)
        {
            var thing = new UdmfThing();
            if (props.TryGetValue("id", out var id)) thing.Id = ToInt(id);
            if (props.TryGetValue("x", out var x)) thing.X = ToDouble(x);
            if (props.TryGetValue("y", out var y)) thing.Y = ToDouble(y);
            if (props.TryGetValue("height", out var h)) thing.Height = ToDouble(h);
            if (props.TryGetValue("angle", out var an)) thing.Angle = ToInt(an);
            if (props.TryGetValue("type", out var ty)) thing.Type = ToInt(ty);
            if (props.TryGetValue("skill1", out var s1)) thing.Skill1 = ToBool(s1);
            if (props.TryGetValue("skill2", out var s2)) thing.Skill2 = ToBool(s2);
            if (props.TryGetValue("skill3", out var s3)) thing.Skill3 = ToBool(s3);
            if (props.TryGetValue("skill4", out var s4)) thing.Skill4 = ToBool(s4);
            if (props.TryGetValue("skill5", out var s5)) thing.Skill5 = ToBool(s5);
            if (props.TryGetValue("ambush", out var am)) thing.Ambush = ToBool(am);
            if (props.TryGetValue("single", out var si)) thing.Single = ToBool(si);
            if (props.TryGetValue("dm", out var dm)) thing.Dm = ToBool(dm);
            if (props.TryGetValue("coop", out var co)) thing.Coop = ToBool(co);
            if (props.TryGetValue("friend", out var fr)) thing.Friend = ToBool(fr);
            if (props.TryGetValue("dormant", out var dr)) thing.Dormant = ToBool(dr);
            if (props.TryGetValue("class1", out var c1)) thing.Class1 = ToBool(c1);
            if (props.TryGetValue("class2", out var c2)) thing.Class2 = ToBool(c2);
            if (props.TryGetValue("class3", out var c3)) thing.Class3 = ToBool(c3);
            if (props.TryGetValue("special", out var sp)) thing.Special = ToInt(sp);
            if (props.TryGetValue("arg0", out var a0)) thing.Arg0 = ToInt(a0);
            if (props.TryGetValue("arg1", out var a1)) thing.Arg1 = ToInt(a1);
            if (props.TryGetValue("arg2", out var a2)) thing.Arg2 = ToInt(a2);
            if (props.TryGetValue("arg3", out var a3)) thing.Arg3 = ToInt(a3);
            if (props.TryGetValue("arg4", out var a4)) thing.Arg4 = ToInt(a4);
            if (props.TryGetValue("comment", out var cm)) thing.Comment = cm?.ToString();
            if (props.TryGetValue("gravity", out var gr)) thing.Gravity = ToDouble(gr);
            if (props.TryGetValue("health", out var hl)) thing.Health = ToDouble(hl);
            if (props.TryGetValue("scalex", out var sx)) thing.ScaleX = ToDouble(sx);
            if (props.TryGetValue("scaley", out var sy)) thing.ScaleY = ToDouble(sy);
            if (props.TryGetValue("renderstyle", out var rs)) thing.RenderStyle = rs?.ToString();
            if (props.TryGetValue("alpha", out var al)) thing.Alpha = ToDouble(al);
            if (props.TryGetValue("fillcolor", out var fc)) thing.FillColor = ToInt(fc);
            return thing;
        }

        private static int ToInt(object value)
        {
            return value switch
            {
                int i => i,
                double d => (int)d,
                string s => int.Parse(s, CultureInfo.InvariantCulture),
                _ => 0
            };
        }

        private static double ToDouble(object value)
        {
            return value switch
            {
                double d => d,
                int i => i,
                string s => double.Parse(s, CultureInfo.InvariantCulture),
                _ => 0.0
            };
        }

        private static bool ToBool(object value)
        {
            return value switch
            {
                bool b => b,
                int i => i != 0,
                string s => s.Equals("true", StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }
    }
}
