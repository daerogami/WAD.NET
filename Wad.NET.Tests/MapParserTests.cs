using System;
using System.Text;
using Xunit;
using WAD.NET.Definitions.Classic.Map;
using WAD.NET.Definitions.Hexen;
using WAD.NET.Enums;
using WAD.NET.Maps;

namespace WAD.NET.Tests
{
    public class MapParserTests
    {
        #region MapDetector Tests

        [Theory]
        [InlineData("E1M1", true)]
        [InlineData("E2M5", true)]
        [InlineData("E4M9", true)]
        [InlineData("MAP01", true)]
        [InlineData("MAP32", true)]
        [InlineData("THINGS", false)]
        [InlineData("PLAYPAL", false)]
        [InlineData("E0M1", false)]
        [InlineData("E1M0", false)]
        [InlineData("MAP100", false)]
        public void MapDetector_IsMapMarker_ShouldDetectMapNames(string name, bool expected)
        {
            Assert.Equal(expected, MapDetector.IsMapMarker(name));
        }

        [Theory]
        [InlineData("THINGS", true)]
        [InlineData("LINEDEFS", true)]
        [InlineData("SIDEDEFS", true)]
        [InlineData("VERTEXES", true)]
        [InlineData("SEGS", true)]
        [InlineData("SSECTORS", true)]
        [InlineData("NODES", true)]
        [InlineData("SECTORS", true)]
        [InlineData("REJECT", true)]
        [InlineData("BLOCKMAP", true)]
        [InlineData("BEHAVIOR", true)]
        [InlineData("TEXTMAP", true)]
        [InlineData("ENDMAP", true)]
        [InlineData("PLAYPAL", false)]
        [InlineData("E1M1", false)]
        public void MapDetector_IsMapLump_ShouldDetectMapLumps(string name, bool expected)
        {
            Assert.Equal(expected, MapDetector.IsMapLump(name));
        }

        [Fact]
        public void MapDetector_DetectFormat_ShouldReturnUdmfForTextmap()
        {
            var lumps = new[] { "TEXTMAP", "ENDMAP" };
            Assert.Equal(MapFormat.UDMF, MapDetector.DetectFormat(lumps));
        }

        [Fact]
        public void MapDetector_DetectFormat_ShouldReturnHexenForBehavior()
        {
            var lumps = new[] { "THINGS", "LINEDEFS", "SIDEDEFS", "VERTEXES", "BEHAVIOR" };
            Assert.Equal(MapFormat.Hexen, MapDetector.DetectFormat(lumps));
        }

        [Fact]
        public void MapDetector_DetectFormat_ShouldReturnDoomForStandardLumps()
        {
            var lumps = new[] { "THINGS", "LINEDEFS", "SIDEDEFS", "VERTEXES" };
            Assert.Equal(MapFormat.Doom, MapDetector.DetectFormat(lumps));
        }

        [Fact]
        public void MapDetector_DetectFormat_ShouldReturnUnknownForEmptyList()
        {
            var lumps = Array.Empty<string>();
            Assert.Equal(MapFormat.Unknown, MapDetector.DetectFormat(lumps));
        }

        [Theory]
        [InlineData("E1M1", 1)]
        [InlineData("E2M5", 2)]
        [InlineData("E4M9", 4)]
        [InlineData("MAP01", -1)]
        public void MapDetector_GetEpisode_ShouldReturnCorrectEpisode(string mapName, int expected)
        {
            Assert.Equal(expected, MapDetector.GetEpisode(mapName));
        }

        [Theory]
        [InlineData("E1M1", 1)]
        [InlineData("E2M5", 5)]
        [InlineData("E4M9", 9)]
        [InlineData("MAP01", 1)]
        [InlineData("MAP15", 15)]
        [InlineData("MAP32", 32)]
        public void MapDetector_GetMapNumber_ShouldReturnCorrectNumber(string mapName, int expected)
        {
            Assert.Equal(expected, MapDetector.GetMapNumber(mapName));
        }

        #endregion

        #region DoomThing Tests

        [Fact]
        public void DoomThing_Parse_ShouldParseCorrectly()
        {
            var data = new byte[10];
            BitConverter.GetBytes((short)100).CopyTo(data, 0);    // X
            BitConverter.GetBytes((short)-200).CopyTo(data, 2);   // Y
            BitConverter.GetBytes((ushort)90).CopyTo(data, 4);    // Angle
            BitConverter.GetBytes((ushort)1).CopyTo(data, 6);     // Type (player 1 start)
            BitConverter.GetBytes((ushort)0x0007).CopyTo(data, 8); // Flags (all skills)

            var thing = DoomThing.Parse(data);

            Assert.Equal(100, thing.X);
            Assert.Equal(-200, thing.Y);
            Assert.Equal(90, thing.Angle);
            Assert.Equal(1, thing.Type);
            Assert.True(thing.AppearsOnSkill1);
            Assert.True(thing.AppearsOnSkill3);
            Assert.True(thing.AppearsOnSkill5);
            Assert.False(thing.IsDeaf);
            Assert.False(thing.IsMultiplayerOnly);
        }

        [Fact]
        public void DoomThing_Parse_ShouldParseAmbushFlag()
        {
            var data = new byte[10];
            BitConverter.GetBytes((ushort)0x0008).CopyTo(data, 8); // Ambush flag

            var thing = DoomThing.Parse(data);

            Assert.True(thing.IsDeaf);
        }

        [Fact]
        public void DoomThing_Parse_ShouldThrowOnInsufficientData()
        {
            var data = new byte[5];
            Assert.Throws<ArgumentException>(() => DoomThing.Parse(data));
        }

        #endregion

        #region MapVertex Tests

        [Fact]
        public void MapVertex_Parse_ShouldParseCorrectly()
        {
            var data = new byte[4];
            BitConverter.GetBytes((short)1024).CopyTo(data, 0);
            BitConverter.GetBytes((short)-512).CopyTo(data, 2);

            var vertex = MapVertex.Parse(data);

            Assert.Equal(1024, vertex.X);
            Assert.Equal(-512, vertex.Y);
        }

        [Fact]
        public void MapVertex_Equality_ShouldWork()
        {
            var v1 = new MapVertex(100, 200);
            var v2 = new MapVertex(100, 200);
            var v3 = new MapVertex(100, 201);

            Assert.True(v1 == v2);
            Assert.False(v1 == v3);
            Assert.True(v1.Equals(v2));
        }

        [Fact]
        public void MapVertex_DistanceTo_ShouldCalculateCorrectly()
        {
            var v1 = new MapVertex(0, 0);
            var v2 = new MapVertex(3, 4);

            Assert.Equal(5.0, v1.DistanceTo(v2), 5);
        }

        #endregion

        #region DoomLinedef Tests

        [Fact]
        public void DoomLinedef_Parse_ShouldParseCorrectly()
        {
            var data = new byte[14];
            BitConverter.GetBytes((ushort)0).CopyTo(data, 0);      // Start vertex
            BitConverter.GetBytes((ushort)1).CopyTo(data, 2);      // End vertex
            BitConverter.GetBytes((ushort)0x0005).CopyTo(data, 4); // Flags (impassable + two-sided)
            BitConverter.GetBytes((ushort)11).CopyTo(data, 6);     // Special (S1 Door Open)
            BitConverter.GetBytes((ushort)5).CopyTo(data, 8);      // Tag
            BitConverter.GetBytes((ushort)0).CopyTo(data, 10);     // Front sidedef
            BitConverter.GetBytes((ushort)1).CopyTo(data, 12);     // Back sidedef

            var linedef = DoomLinedef.Parse(data);

            Assert.Equal(0, linedef.StartVertex);
            Assert.Equal(1, linedef.EndVertex);
            Assert.True(linedef.IsImpassable);
            Assert.True(linedef.IsTwoSided);
            Assert.Equal(11, linedef.Special);
            Assert.Equal(5, linedef.Tag);
            Assert.Equal(0, linedef.FrontSidedef);
            Assert.Equal(1, linedef.BackSidedef);
            Assert.True(linedef.HasBackSide);
        }

        [Fact]
        public void DoomLinedef_HasBackSide_ShouldBeFalseFor0xFFFF()
        {
            var data = new byte[14];
            BitConverter.GetBytes((ushort)0xFFFF).CopyTo(data, 12);

            var linedef = DoomLinedef.Parse(data);

            Assert.False(linedef.HasBackSide);
        }

        #endregion

        #region DoomSidedef Tests

        [Fact]
        public void DoomSidedef_Parse_ShouldParseCorrectly()
        {
            var data = new byte[30];
            BitConverter.GetBytes((short)16).CopyTo(data, 0);  // X offset
            BitConverter.GetBytes((short)-8).CopyTo(data, 2);  // Y offset
            Encoding.ASCII.GetBytes("STARTAN1").CopyTo(data, 4);   // Upper texture
            Encoding.ASCII.GetBytes("FLOOR4_8").CopyTo(data, 12);  // Lower texture
            Encoding.ASCII.GetBytes("-\0\0\0\0\0\0\0").CopyTo(data, 20);  // Middle texture (none)
            BitConverter.GetBytes((ushort)5).CopyTo(data, 28); // Sector

            var sidedef = DoomSidedef.Parse(data);

            Assert.Equal(16, sidedef.XOffset);
            Assert.Equal(-8, sidedef.YOffset);
            Assert.Equal("STARTAN1", sidedef.UpperTexture);
            Assert.Equal("FLOOR4_8", sidedef.LowerTexture);
            Assert.Equal("-", sidedef.MiddleTexture);
            Assert.Equal(5, sidedef.Sector);
            Assert.True(sidedef.HasUpperTexture);
            Assert.True(sidedef.HasLowerTexture);
            Assert.False(sidedef.HasMiddleTexture);
        }

        #endregion

        #region DoomSector Tests

        [Fact]
        public void DoomSector_Parse_ShouldParseCorrectly()
        {
            var data = new byte[26];
            BitConverter.GetBytes((short)0).CopyTo(data, 0);     // Floor height
            BitConverter.GetBytes((short)128).CopyTo(data, 2);   // Ceiling height
            Encoding.ASCII.GetBytes("FLOOR4_8").CopyTo(data, 4);  // Floor texture
            Encoding.ASCII.GetBytes("CEIL3_5\0").CopyTo(data, 12); // Ceiling texture
            BitConverter.GetBytes((ushort)160).CopyTo(data, 20); // Light level
            BitConverter.GetBytes((ushort)0).CopyTo(data, 22);   // Special
            BitConverter.GetBytes((ushort)0).CopyTo(data, 24);   // Tag

            var sector = DoomSector.Parse(data);

            Assert.Equal(0, sector.FloorHeight);
            Assert.Equal(128, sector.CeilingHeight);
            Assert.Equal("FLOOR4_8", sector.FloorTexture);
            Assert.Equal("CEIL3_5", sector.CeilingTexture);
            Assert.Equal(160, sector.LightLevel);
            Assert.Equal(128, sector.Height);
            Assert.False(sector.IsSecret);
            Assert.False(sector.IsDamaging);
        }

        [Fact]
        public void DoomSector_IsSecret_ShouldBeTrueForSpecial9()
        {
            var data = new byte[26];
            BitConverter.GetBytes((ushort)9).CopyTo(data, 22); // Secret special

            var sector = DoomSector.Parse(data);

            Assert.True(sector.IsSecret);
        }

        #endregion

        #region BSP Structure Tests

        [Fact]
        public void Seg_Parse_ShouldParseCorrectly()
        {
            var data = new byte[12];
            BitConverter.GetBytes((ushort)0).CopyTo(data, 0);     // Start vertex
            BitConverter.GetBytes((ushort)1).CopyTo(data, 2);     // End vertex
            BitConverter.GetBytes((short)16384).CopyTo(data, 4);  // Angle (North)
            BitConverter.GetBytes((ushort)5).CopyTo(data, 6);     // Linedef
            BitConverter.GetBytes((ushort)0).CopyTo(data, 8);     // Direction (front)
            BitConverter.GetBytes((short)0).CopyTo(data, 10);     // Offset

            var seg = Seg.Parse(data);

            Assert.Equal(0, seg.StartVertex);
            Assert.Equal(1, seg.EndVertex);
            Assert.Equal(16384, seg.Angle);
            Assert.Equal(5, seg.LinedefIndex);
            Assert.False(seg.IsBackSide);
            Assert.Equal(90.0, seg.AngleDegrees, 5);
        }

        [Fact]
        public void Subsector_Parse_ShouldParseCorrectly()
        {
            var data = new byte[4];
            BitConverter.GetBytes((ushort)3).CopyTo(data, 0);  // Seg count
            BitConverter.GetBytes((ushort)10).CopyTo(data, 2); // First seg

            var subsector = Subsector.Parse(data);

            Assert.Equal(3, subsector.SegCount);
            Assert.Equal(10, subsector.FirstSeg);
        }

        [Fact]
        public void Node_Parse_ShouldParseCorrectly()
        {
            var data = new byte[28];
            BitConverter.GetBytes((short)0).CopyTo(data, 0);      // Partition X
            BitConverter.GetBytes((short)0).CopyTo(data, 2);      // Partition Y
            BitConverter.GetBytes((short)64).CopyTo(data, 4);     // Partition dX
            BitConverter.GetBytes((short)0).CopyTo(data, 6);      // Partition dY
            // Right bbox (top, bottom, left, right)
            BitConverter.GetBytes((short)128).CopyTo(data, 8);
            BitConverter.GetBytes((short)0).CopyTo(data, 10);
            BitConverter.GetBytes((short)0).CopyTo(data, 12);
            BitConverter.GetBytes((short)64).CopyTo(data, 14);
            // Left bbox
            BitConverter.GetBytes((short)128).CopyTo(data, 16);
            BitConverter.GetBytes((short)0).CopyTo(data, 18);
            BitConverter.GetBytes((short)64).CopyTo(data, 20);
            BitConverter.GetBytes((short)128).CopyTo(data, 22);
            // Children
            BitConverter.GetBytes((ushort)0x8001).CopyTo(data, 24); // Right (subsector 1)
            BitConverter.GetBytes((ushort)5).CopyTo(data, 26);      // Left (node 5)

            var node = Node.Parse(data);

            Assert.Equal(64, node.PartitionDx);
            Assert.True(node.RightIsSubsector);
            Assert.False(node.LeftIsSubsector);
            Assert.Equal(1, node.RightSubsectorIndex);
            Assert.Equal(5, node.LeftNodeIndex);
        }

        [Fact]
        public void BoundingBox_Contains_ShouldWork()
        {
            var bbox = new BoundingBox(100, 0, 0, 100);

            Assert.True(bbox.Contains(50, 50));
            Assert.True(bbox.Contains(0, 0));
            Assert.True(bbox.Contains(100, 100));
            Assert.False(bbox.Contains(-1, 50));
            Assert.False(bbox.Contains(50, 101));
        }

        #endregion

        #region Hexen Format Tests

        [Fact]
        public void HexenThing_Parse_ShouldParseCorrectly()
        {
            var data = new byte[20];
            BitConverter.GetBytes((ushort)100).CopyTo(data, 0);   // TID
            BitConverter.GetBytes((short)512).CopyTo(data, 2);    // X
            BitConverter.GetBytes((short)256).CopyTo(data, 4);    // Y
            BitConverter.GetBytes((short)32).CopyTo(data, 6);     // Z
            BitConverter.GetBytes((ushort)90).CopyTo(data, 8);    // Angle
            BitConverter.GetBytes((ushort)14).CopyTo(data, 10);   // Type
            BitConverter.GetBytes((ushort)0x0017).CopyTo(data, 12); // Flags (all skills + dormant)
            data[14] = 80;  // Special
            data[15] = 1;   // Arg0
            data[16] = 2;   // Arg1
            data[17] = 3;   // Arg2
            data[18] = 4;   // Arg3
            data[19] = 5;   // Arg4

            var thing = HexenThing.Parse(data);

            Assert.Equal(100, thing.TID);
            Assert.Equal(512, thing.X);
            Assert.Equal(256, thing.Y);
            Assert.Equal(32, thing.Z);
            Assert.Equal(90, thing.Angle);
            Assert.Equal(14, thing.Type);
            Assert.True(thing.AppearsOnSkill1);
            Assert.True(thing.AppearsOnSkill3);
            Assert.True(thing.IsDormant);
            Assert.Equal(80, thing.Special);
            Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, thing.Args);
        }

        [Fact]
        public void HexenLinedef_Parse_ShouldParseCorrectly()
        {
            var data = new byte[16];
            BitConverter.GetBytes((ushort)0).CopyTo(data, 0);      // Start vertex
            BitConverter.GetBytes((ushort)1).CopyTo(data, 2);      // End vertex
            BitConverter.GetBytes((ushort)0x0205).CopyTo(data, 4); // Flags (impassable + two-sided + repeatable)
            data[6] = 70;   // Special
            data[7] = 10;   // Arg0
            data[8] = 20;   // Arg1
            data[9] = 30;   // Arg2
            data[10] = 40;  // Arg3
            data[11] = 50;  // Arg4
            BitConverter.GetBytes((ushort)0).CopyTo(data, 12);     // Front sidedef
            BitConverter.GetBytes((ushort)1).CopyTo(data, 14);     // Back sidedef

            var linedef = HexenLinedef.Parse(data);

            Assert.Equal(0, linedef.StartVertex);
            Assert.Equal(1, linedef.EndVertex);
            Assert.True(linedef.IsTwoSided);
            Assert.Equal(70, linedef.Special);
            Assert.Equal(new byte[] { 10, 20, 30, 40, 50 }, linedef.Args);
            Assert.True(linedef.HasBackSide);
        }

        #endregion
    }
}
